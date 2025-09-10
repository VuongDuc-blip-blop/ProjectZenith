using System.Diagnostics;
using System.Text.Json;
using Azure.Storage.Queues;
using MediatR;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Commands.Purchase;
using ProjectZenith.Contracts.Configuration;

namespace ProjectZenith.Api.Write.Services.PurchaseDomain.BackgroundServices
{
    public class PayoutProcessingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AzureStorageQueueOptions _azureQueueOptions;
        private readonly ILogger<PayoutProcessingBackgroundService> _logger;

        public PayoutProcessingBackgroundService(IServiceProvider serviceProvider, IOptions<AzureStorageQueueOptions> azureQueueOptions, ILogger<PayoutProcessingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _azureQueueOptions = azureQueueOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueClient = new QueueClient(
            new Uri($"https://{_azureQueueOptions.AccountName}.queue.core.windows.net/{_azureQueueOptions.PayoutsToProcessQueue}"),
            new Azure.Identity.EnvironmentCredential());

            await queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await queueClient.ReceiveMessageAsync(cancellationToken: stoppingToken);
                    if (message.Value != null)
                    {
                        var command = JsonSerializer.Deserialize<ProcessSinglePayoutCommand>(message.Value.MessageText)
                        ?? throw new InvalidOperationException("Failed to deserialize ProcessSinglePayoutCommand from queue message.");

                        using var scope = _serviceProvider.CreateScope();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                        await mediator.Send(command, stoppingToken);

                        await queueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt, stoppingToken);

                        _logger.LogInformation("Processed and deleted payout message {MessageId} from payout {PayoutId}.", message.Value.MessageId, command.PayoutId);

                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait before checking for new messages
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing payout message: {Message}", ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait before retrying on error
                }
            }
        }
    }
}
