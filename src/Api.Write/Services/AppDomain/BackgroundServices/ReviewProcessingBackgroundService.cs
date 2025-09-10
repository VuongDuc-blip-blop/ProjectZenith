using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;

namespace ProjectZenith.Api.Write.Services.AppDomain.BackgroundServices
{
    public class ReviewProcessingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly KafkaOptions _kafkaOptions;
        private readonly ILogger<ReviewProcessingBackgroundService> _logger;

        public ReviewProcessingBackgroundService(
            IServiceProvider serviceProvider,
            IOptions<KafkaOptions> kafkaOptions,
            ILogger<ReviewProcessingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _kafkaOptions = kafkaOptions.Value;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Review Processing Background Service.");

            return Task.Run(async () =>
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = _kafkaOptions.BootstrapServers,
                    GroupId = "review-processing-group",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false
                };

                using var consumer = new ConsumerBuilder<string, string>(config).Build();
                consumer.Subscribe(KafkaTopics.Reviews);

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(stoppingToken);
                            var message = consumeResult.Message.Value;

                            var appIdString = consumeResult.Message.Key;
                            if (!Guid.TryParse(appIdString, out var appId))
                            {
                                _logger.LogWarning("Received a message on topic '{Topic}' with an invalid AppId key: {Key}", consumeResult.Topic, appIdString);
                                consumer.Commit(consumeResult);
                                continue;
                            }
                            await SendRecalculateCommandAsync(appId, stoppingToken);

                            consumer.Commit(consumeResult);
                            _logger.LogInformation("Processed review message for App ID {AppId} at offset {Offset} from topic {Topic}.", appId, consumeResult.Offset, consumeResult.Topic);


                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError("Consume error: {Error}", ex.Error.Reason);
                        }
                        catch (OperationCanceledException)
                        {
                            break; // Turn down the service gracefully
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Unexpected error: {Message}", ex.Message);
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Backoff before retrying
                        }
                    }
                }
                finally
                {
                    consumer.Close();
                    _logger.LogInformation("Review Processing Background Service is stopping.");
                }
            }, stoppingToken);
        }

        private async Task SendRecalculateCommandAsync(Guid appId, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new RecalculateAppRatingCommand(appId);
            await mediator.Send(command, cancellationToken);
        }
    }
}
