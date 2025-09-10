using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Infrastructure.MessageQueue;

namespace FileScan.Functions;

public class ReconcileDeveloperPayoutStatusFunction
{
    private readonly WriteDbContext _dbContext;
    private readonly IQueueService _queueService;
    private readonly AzureStorageQueueOptions _queueOptions;
    private readonly ILogger<ReconcileDeveloperPayoutStatusFunction> _logger;

    public ReconcileDeveloperPayoutStatusFunction(WriteDbContext dbContext, IQueueService queueService, IOptions<AzureStorageQueueOptions> queueOptions, ILogger<ReconcileDeveloperPayoutStatusFunction> logger)
    {
        _dbContext = dbContext;
        _queueService = queueService;
        _queueOptions = queueOptions.Value;
        _logger = logger;
    }

    [Function("ReconcileDeveloperPayoutStatus")]
    public async Task Run([TimerTrigger(" 0 0 2 * * *")] TimerInfo myTimer, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting developer payout status reconciliation at: {time}", DateTimeOffset.Now);

        var developersToReconcile = await _dbContext.Developers
            .Where(d => d.StripeAccountId != null
            && (d.PayoutStatus == DeveloperPayoutStatus.OnBoardingInProgress || d.PayoutStatus == DeveloperPayoutStatus.OnboardedButRestricted)
            && d.UpdatedAt <= DateTime.UtcNow.AddHours(-12))
            .ToListAsync(cancellationToken);

        foreach (var developer in developersToReconcile)
        {
            try
            {
                var message = new { DeveloperId = developer.UserId };
                await _queueService.SendMessageAsync(_queueOptions.DeveloperStatusReconciliationQueue, JsonSerializer.Serialize(message), cancellationToken);

                _logger.LogInformation("Enqueued payout status reconciliation for Developer {DeveloperId}.", developer.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue payout status reconciliation for Developer {DeveloperId}, Message: {Message}.", developer.UserId, ex.Message);
            }
        }
    }
}