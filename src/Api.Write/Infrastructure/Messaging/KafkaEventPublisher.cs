// src/Api.Write/Infrastructure/Messaging/KafkaEventPublisher.cs
using ProjectZenith.Api.Write.Abstraction;
using System.Text.Json;

namespace ProjectZenith.Api.Write.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher
{
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        _logger.LogInformation("Publishing to topic {Topic}: {Message}", topic, JsonSerializer.Serialize(message));
        await Task.CompletedTask;
    }
}