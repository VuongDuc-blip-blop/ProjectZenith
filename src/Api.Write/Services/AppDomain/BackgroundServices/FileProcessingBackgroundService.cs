using Confluent.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Commands.Purchase;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Events.App;
using ProjectZenith.Contracts.Events.Purchase;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using System.Text.Json;
namespace ProjectZenith.Api.Write.Services.AppDomain.BackgroundServices
{
    public class FileProcessingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly KafkaOptions _kafkaOptions;
        private readonly ILogger<FileProcessingBackgroundService> _logger;

        public FileProcessingBackgroundService(IServiceProvider serviceProvider, IOptions<KafkaOptions> kafkaOptions, ILogger<FileProcessingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _kafkaOptions = kafkaOptions.Value;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File Processing Background Service is starting.");

            return Task.Run(async () =>
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = _kafkaOptions.BootstrapServers,
                    GroupId = "file-processing-group",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false
                };

                using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
                consumer.Subscribe(new[] { KafkaTopics.AppFileProcessingResults, KafkaTopics.ScreenshotProcessingResults });

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(stoppingToken);

                            await ProcessMessageAsync(consumeResult, stoppingToken);


                            consumer.Commit(consumeResult);
                            _logger.LogInformation("Message from topic '{Topic}' processed and committed at offset {Offset}.", consumeResult.Topic, consumeResult.Offset);
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError("Consume error: {Error}", ex.Error.Reason);
                        }
                        catch (OperationCanceledException)
                        {
                            // Graceful shutdown
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Unexpected error: {Message}", ex.Message);
                        }
                    }
                }
                finally
                {
                    consumer.Close();
                    _logger.LogInformation("File Processing Background Service is stopping.");
                }
            }, stoppingToken);
        }

        private async Task ProcessMessageAsync(ConsumeResult<Ignore, string> consumeResult, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
            var topic = consumeResult.Topic;

            var message = consumeResult.Message.Value;

            if (topic == KafkaTopics.AppFileProcessingResults)
            {
                if (JsonSerializer.Deserialize<AppFileValidatedEvent>(message) is { } validatedEvent)
                {
                    var version = await dbContext.AppVersions
                                .FirstOrDefaultAsync(v => v.FileId == validatedEvent.AppFileId, cancellationToken)
                                ?? throw new InvalidOperationException($"Version not found for AppFileId {validatedEvent.AppFileId}");

                    var command = new MarkVersionAsPendingApprovalCommand(
                        validatedEvent.AppId,
                        version.Id,
                        validatedEvent.AppFileId,
                        validatedEvent.FinalPath);
                    await mediator.Send(command, cancellationToken);
                }
                else if (JsonSerializer.Deserialize<AppFileValidationFailedEvent>(message) is { } failedEvent)
                {
                    var version = await dbContext.AppVersions
                                .FirstOrDefaultAsync(v => v.FileId == failedEvent.AppFileId, cancellationToken)
                                ?? throw new InvalidOperationException($"Version not found for AppFileId {failedEvent.AppFileId}");

                    var command = new RejectVersionCommand(
                        failedEvent.AppId,
                        version.Id,
                        failedEvent.AppFileId,
                        failedEvent.Reason,
                        failedEvent.RejectedPath);
                    await mediator.Send(command, cancellationToken);
                }
            }
            else if (topic == KafkaTopics.ScreenshotProcessingResults)
            {
                if (JsonSerializer.Deserialize<ScreenshotProcessedEvent>(message) is { } processedEvent)
                {
                    var command = new MarkScreenshotProcessedCommand(
                        processedEvent.AppId,
                        processedEvent.ScreenshotId,
                        processedEvent.BlobName,
                        processedEvent.Checksum
                    );
                    await mediator.Send(command, cancellationToken);
                }
            }
            else if (topic == KafkaTopics.Purchases)
            {
                if (JsonSerializer.Deserialize<PurchaseCompletedEvent>(message) is { } purchasedEvent)
                {
                    var command = new SchedulePayoutCommand(
                        purchasedEvent.DeveloperId,
                        purchasedEvent.PurchaseId,
                        purchasedEvent.Price * 0.7m, // Assuming a 70% payout rate, 30% platform fee
                        "Stripe",
                        purchasedEvent.PaymentId
                    );

                    await mediator.Send(command, cancellationToken);
                }
            }
        }
    }
}
