namespace ProjectZenith.Contracts.Infrastructure
{
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes an event to a specified topic asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the event object.</typeparam>
        /// <param name="topic">The name of the topic to publish to.</param>
        /// <param name="message">The event object to publish.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous publish operation.</returns>
        Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
    }
}