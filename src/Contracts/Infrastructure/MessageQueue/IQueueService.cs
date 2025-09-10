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

        /// <summary>
        /// Receives a single message from the specified queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A wrapper containing the message, or null if the queue is empty.</returns>
        Task<QueueMessageWrapper?> ReceiveMessageAsync(string queueName, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a message from the queue after it has been successfully processed.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="message">The message wrapper to be deleted.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task DeleteMessageAsync(string queueName, QueueMessageWrapper message, CancellationToken cancellationToken);
    }
}
