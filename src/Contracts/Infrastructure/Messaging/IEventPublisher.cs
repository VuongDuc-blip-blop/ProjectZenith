namespace ProjectZenith.Contracts.Infrastructure
{
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes an event to a specified topic using a key for partitioning.
        /// </summary>
        /// <param name="topic">The name of the topic to publish to.</param>
        /// <param name="key">The key used for partitioning. All messages with the same key will go to the same partition, ensuring order.</param>
        /// <param name="eventData">The event object to be serialized and published.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishAsync<T>(string topic, string key, T eventData, CancellationToken cancellationToken = default) where T : class;
    }
}