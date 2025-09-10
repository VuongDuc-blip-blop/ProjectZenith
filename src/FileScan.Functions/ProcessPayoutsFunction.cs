using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.Purchase;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Models;

namespace FileScan.Functions;

public class ProcessPayoutsFunction
{
    private readonly WriteDbContext _dbContext;
    private readonly AzureStorageQueueOptions _queueOptions;
    private readonly ILogger<ProcessPayoutsFunction> _logger;

    public ProcessPayoutsFunction(WriteDbContext dbContext, IOptions<AzureStorageQueueOptions> queueOptions, ILogger<ProcessPayoutsFunction> logger)
    {
        _dbContext = dbContext;
        _queueOptions = queueOptions.Value;
        _logger = logger;
    }

    [Function("ProcessPayouts")]
    public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        var queueClient = new QueueClient(
            new Uri($"https://{_queueOptions.AccountName}.queue.core.windows.net/{_queueOptions.PayoutsToProcessQueue}"),
            new Azure.Identity.EnvironmentCredential());

        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var duePayoutIds = await _dbContext.Payouts
            .Where(p => p.Status == PayoutStatus.Scheduled && p.ProcessAt <= DateTime.UtcNow)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var payoutId in duePayoutIds)
        {
            try
            {
                var command = new ProcessSinglePayoutCommand(payoutId);
                var message = JsonSerializer.Serialize(command);

                await queueClient.SendMessageAsync(message, cancellationToken: cancellationToken);

                _logger.LogInformation("Enqueued payout {PayoutId} for processing with ProcessingSinglePayoutCommand.", payoutId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing payout {PayoutId}: {Message}", payoutId, ex.Message);
            }
        }

    }
}