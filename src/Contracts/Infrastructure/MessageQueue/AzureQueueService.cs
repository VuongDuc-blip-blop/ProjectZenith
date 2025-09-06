using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;

namespace ProjectZenith.Contracts.Infrastructure.MessageQueue
{
    public class AzureQueueService : IQueueService
    {
        private readonly QueueServiceClient _queueServiceClient;

        // Inject a QueueServiceClient instead of IConfiguration
        public AzureQueueService(QueueServiceClient queueServiceClient)
        {
            _queueServiceClient = queueServiceClient
                ?? throw new ArgumentNullException(nameof(queueServiceClient));
        }

        public async Task SendMessageAsync(string queueName, string messageContent, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException("Queue name cannot be null or empty.", nameof(queueName));

            if (string.IsNullOrWhiteSpace(messageContent))
                throw new ArgumentException("Message content cannot be null or empty.", nameof(messageContent));

            // Get Queue client
            var queueClient = _queueServiceClient.GetQueueClient(queueName);

            // Ensure the queue exists
            await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            // Send message (explicit Base64 encoding for safety)
            var messageBytes = Encoding.UTF8.GetBytes(messageContent);
            await queueClient.SendMessageAsync(Convert.ToBase64String(messageBytes), cancellationToken);
        }
    }
}
