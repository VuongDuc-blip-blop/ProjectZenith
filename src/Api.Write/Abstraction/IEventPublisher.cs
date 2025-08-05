namespace ProjectZenith.Api.Write.Abstraction
{
    public interface IEventPublisher
    {
        /// <summary>
        /// Publish an event to a specified topic.
        /// </summary>
        /// <typeparam name="T">The type of the event object</typeparam>
        /// <param name="topic">The name of the topic to publish to</param>
        /// <param name="message">The event object to publish</param>
        /// <returns></returns>
        Task PublishAsync<T>(string topic, T message);
    }
}
