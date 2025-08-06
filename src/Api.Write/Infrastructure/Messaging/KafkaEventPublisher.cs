using ProjectZenith.Api.Write.Abstraction;
using System.Text.Json;

namespace ProjectZenith.Api.Write.Infrastructure.Messaging
{
    public class KafkaEventPublisher : IEventPublisher
    {
        private readonly ILogger<KafkaEventPublisher> _logger;

        public KafkaEventPublisher(ILogger<KafkaEventPublisher> logger)
        {
            _logger = logger;
        }

        // The method signature now matches the new interface
        public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        {
            // 1. Check if cancellation has been requested BEFORE doing any work.
            // This is a best practice. If the operation is already cancelled,
            // we stop immediately by throwing the exception.
            cancellationToken.ThrowIfCancellationRequested();

            var serializedMessage = JsonSerializer.Serialize(message);

            _logger.LogInformation(
                "SIMULATING KAFKA PUBLISH: Publishing event to topic {Topic}",
                topic);

            try
            {
                // 2. Simulate a network delay. This makes the cancellation meaningful.
                // Without a delay, the task might complete before a cancellation
                // request has time to propagate. A real network call takes time.
                // Task.Delay will honor the cancellation token.
                await Task.Delay(50, cancellationToken); // Simulate a 50ms network call

                _logger.LogInformation(
                    "SIMULATION COMPLETE: Successfully published event to topic {Topic}: {Message}",
                    topic,
                    serializedMessage);
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