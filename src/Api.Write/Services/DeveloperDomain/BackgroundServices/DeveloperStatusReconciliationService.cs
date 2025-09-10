using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Commands.Developer;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Infrastructure.MessageQueue;

namespace ProjectZenith.Api.Write.Services.DeveloperDomain.BackgroundServices
{
    public class DeveloperStatusReconciliationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AzureStorageQueueOptions _queueOptions;
        private readonly ILogger<DeveloperStatusReconciliationService> _logger;

        public DeveloperStatusReconciliationService(
            IServiceProvider serviceProvider,
            IOptions<AzureStorageQueueOptions> queueOptions,
            ILogger<DeveloperStatusReconciliationService> logger)
        {
            _serviceProvider = serviceProvider;
            _queueOptions = queueOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting DeveloperStatusReconciliationService.");

            using var initialScope = _serviceProvider.CreateScope();
            var queueService = initialScope.ServiceProvider.GetRequiredService<IQueueService>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await queueService.ReceiveMessageAsync(_queueOptions.DeveloperStatusReconciliationQueue, stoppingToken);

                    if (message != null)
                    {
                        var payload = JsonSerializer.Deserialize<Dictionary<string, Guid>>(message.MessageText);

                        if (payload != null && payload.TryGetValue("DeveloperId", out var developerId))
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                            var command = new ReconcilePayoutStatusCommand
                            (
                                DeveloperId: developerId
                            );

                            await mediator.Send(command, stoppingToken);

                            await queueService.DeleteMessageAsync(_queueOptions.DeveloperStatusReconciliationQueue, message, stoppingToken);

                            _logger.LogInformation("Processed payout status reconciliation for Developer {DeveloperId}.", developerId);
                        }
                    }
                    else
                    {
                        // No message found, wait before polling again
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing developer status reconciliation message: {Message}", ex.Message);
                    // Optional: Add a delay to avoid tight error loops
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }
        }
    }
}

