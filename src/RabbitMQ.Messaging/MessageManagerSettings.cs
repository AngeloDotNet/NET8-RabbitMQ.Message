using System.Text.Json;
using RabbitMQ.Messaging.Abstractions;

namespace RabbitMQ.Messaging;

public class MessageManagerSettings
{
    public string ConnectionString { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
    public ushort QueuePrefetchCount { get; set; }
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = JsonOptions.Default;
}
