namespace ProjectZenith.Contracts.Infrastructure.MessageQueue
{
    public interface IQueueService
    {
        /// <summary>
        /// Sends a message to a specified queue to trigger background processing.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="messageContent">The content of the message to send.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task SendMessageAsync(string queueName, string messageContent, CancellationToken cancellationToken);
    }
}
