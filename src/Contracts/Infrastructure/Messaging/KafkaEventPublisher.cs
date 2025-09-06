using Microsoft.Extensions.Logging;
using System.Text.Json;
using Confluent.Kafka;
using ProjectZenith.Contracts.Configuration;
using Microsoft.Extensions.Options;

namespace ProjectZenith.Contracts.Infrastructure
{
    public class KafkaEventPublisher : IEventPublisher
    {
        private readonly IProducer<Null, string> _producer;
        private readonly KafkaOptions _kafkaOptions;
        private readonly ILogger<KafkaEventPublisher> _logger;

        public KafkaEventPublisher(ILogger<KafkaEventPublisher> logger, IOptions<KafkaOptions> kafkaOptions)
        {
            _logger = logger;
            _kafkaOptions = kafkaOptions.Value;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaOptions.BootstrapServers,
                Acks = Acks.Leader
            };
            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }


        // The method signature now matches the new interface
        public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        {
            // 1. Check if cancellation has been requested BEFORE doing any work.
            // This is a best practice. If the operation is already cancelled,
            // we stop immediately by throwing the exception.
            cancellationToken.ThrowIfCancellationRequested();

            var serializedMessage = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<Null, string>
            {
                Value = serializedMessage
            };


            _logger.LogInformation(
                "SIMULATING KAFKA PUBLISH: Publishing event to topic {Topic}",
                topic);

            try
            {
                var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
                _logger.LogInformation(
                    "KAFKA PUBLISH COMPLETE: Successfully published event to topic {Topic}: {Message}",
                    topic,
                    serializedMessage);
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(
                    ex,
                    "KAFKA PUBLISH FAILED: Failed to publish event to topic {Topic}: {ErrorReason}",
                    topic,
                    ex.Error.Reason);
                throw; // Re-throw the exception after logging it.
            }
            catch (OperationCanceledException)
            {
                // 3. Catch the specific exception from Task.Delay.
                // This is how we know our simulation was cancelled.
                _logger.LogWarning(
                    "SIMULATION CANCELED: Publishing to topic {Topic} was canceled.",
                    topic);

                // Re-throw the exception so the calling code knows it was cancelled.
                throw;
            }
        }
    }
}