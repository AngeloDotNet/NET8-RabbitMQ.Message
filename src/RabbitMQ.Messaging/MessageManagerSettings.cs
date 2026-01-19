using System.Text.Json;
using RabbitMQ.Messaging.Abstractions;

namespace RabbitMQ.Messaging;

/// <summary>
/// Configuration settings used by the <see cref="MessageManager"/> to connect to RabbitMQ,
/// declare exchanges/queues and to serialize messages.
/// </summary>
/// <remarks>
/// Typical values:
/// - <see cref="ConnectionString"/>: AMQP URI used to create the connection (e.g. "amqp://guest:guest@localhost:5672").
/// - <see cref="ExchangeName"/>: name of the exchange to declare and publish to.
/// - <see cref="QueuePrefetchCount"/>: optional prefetch count applied via BasicQos; 0 means no prefetch set.
/// - <see cref="JsonSerializerOptions"/>: serializer options used when serializing messages to JSON.
/// </remarks>
public class MessageManagerSettings
{
    /// <summary>
    /// AMQP connection string used by the <see cref="MessageManager"/> to establish a connection to the broker.
    /// </summary>
    /// <example>
    /// "amqp://guest:guest@localhost:5672"
    /// </example>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// The name of the exchange to declare and publish messages to.
    /// </summary>
    /// <remarks>
    /// This exchange will be declared with <c>ExchangeType.Direct</c> and <c>durable=true</c> by the
    /// <see cref="MessageManager"/> constructor.
    /// </remarks>
    public string ExchangeName { get; set; } = null!;

    /// <summary>
    /// The per-channel prefetch count used when calling <c>BasicQos</c>.
    /// </summary>
    /// <remarks>
    /// A value of zero means no explicit prefetch is set. When greater than zero the <see cref="MessageManager"/>
    /// will apply this value to the channel to limit the number of unacknowledged messages delivered to consumers.
    /// </remarks>
    public ushort QueuePrefetchCount { get; set; }

    /// <summary>
    /// <see cref="JsonSerializerOptions"/> used when serializing messages to JSON for publishing.
    /// </summary>
    /// <remarks>
    /// By default this is initialized to <see cref="JsonOptions.Default"/>. Supply custom options to customize
    /// property naming, converters, or other serialization behavior.
    /// </remarks>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = JsonOptions.Default;
}
