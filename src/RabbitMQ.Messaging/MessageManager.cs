using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Messaging.Abstractions;

namespace RabbitMQ.Messaging;

/// <summary>
/// Manages a RabbitMQ connection and channel, provides publish and acknowledgement helpers,
/// and implements asynchronous disposal for the managed resources.
/// </summary>
internal class MessageManager : IMessageSender, IAsyncDisposable
{
    /// <summary>
    /// Header name used to set the maximum priority for declared queues.
    /// </summary>
    private const string MaxPriorityHeader = "x-max-priority";

    /// <summary>
    /// The underlying RabbitMQ connection. Initialized in the constructor.
    /// </summary>
    internal IConnection Connection { get; private set; }

    /// <summary>
    /// The underlying RabbitMQ channel. Initialized in the constructor.
    /// </summary>
    internal IChannel Channel { get; private set; }

    /// <summary>
    /// Settings used to configure the message manager (connection string, exchange name, serializer options, etc).
    /// </summary>
    private readonly MessageManagerSettings messageManagerSettings;

    /// <summary>
    /// Queue settings (list of known queues and their mapped types).
    /// </summary>
    private readonly QueueSettings queueSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageManager"/> class.
    /// Establishes a connection and a channel to the RabbitMQ broker, declares the configured exchange,
    /// declares and binds the configured queues, and applies any prefetch settings.
    /// </summary>
    /// <param name="messageManagerSettings">Configuration for the message manager (connection string, exchange, serializer options).</param>
    /// <param name="queueSettings">Configuration for queues (names and message type mappings).</param>
    /// <remarks>
    /// This constructor performs synchronous waits on async methods to create the connection and channel.
    /// It may throw exceptions propagated from the RabbitMQ client if connection/channel creation or
    /// exchange/queue declaration fails (for example, network or broker errors).
    /// </remarks>
    public MessageManager(MessageManagerSettings messageManagerSettings, QueueSettings queueSettings)
    {
        var factory = new ConnectionFactory { Uri = new Uri(messageManagerSettings.ConnectionString) };

        Connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        Channel = Connection.CreateChannelAsync().GetAwaiter().GetResult();

        if (messageManagerSettings.QueuePrefetchCount > 0)
        {
            Channel.BasicQosAsync(0, messageManagerSettings.QueuePrefetchCount, false);
        }

        Channel.ExchangeDeclareAsync(messageManagerSettings.ExchangeName, ExchangeType.Direct, durable: true);

        foreach (var (queue, args)
            in from (string Name, Type Type) queue in queueSettings.Queues
               let args = new Dictionary<string, object>
               {
                   [MaxPriorityHeader] = 10
               }
               select (queue, args))
        {
            Channel.QueueDeclareAsync(queue.Name, durable: true, exclusive: false, autoDelete: false, args);
            Channel.QueueBindAsync(queue.Name, messageManagerSettings.ExchangeName, queue.Name, null);
        }

        this.messageManagerSettings = messageManagerSettings;
        this.queueSettings = queueSettings;
    }

    /// <summary>
    /// Publishes a message of type <typeparamref name="T"/> to the exchange using the routing key
    /// mapped to the message type in the queue settings. The message is serialized as JSON using
    /// the configured serializer options (or a default).
    /// </summary>
    /// <typeparam name="T">The CLR type of the message to publish.</typeparam>
    /// <param name="message">The message instance to serialize and publish. Must be a reference type.</param>
    /// <param name="priority">Message priority (converted to a byte). Defaults to 1. Valid range is 0-255, but effective range depends on queue configuration.</param>
    /// <returns>A completed <see cref="Task"/> once the publish call has been issued.</returns>
    /// <remarks>
    /// The method looks up the routing key by matching the message CLR type to the configured queues.
    /// If no matching queue is found, a <see cref="InvalidOperationException"/> may be thrown by the LINQ lookup.
    /// Publishing is performed via <see cref="IChannel.BasicPublishAsync"/>; errors from the client will propagate.
    /// </remarks>
    public Task PublishAsync<T>(T message, int priority = 1) where T : class
    {
        var sendBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize<object>(message, messageManagerSettings.JsonSerializerOptions ?? JsonOptions.Default));
        var routingKey = queueSettings.Queues.First(q => q.Type == typeof(T)).Name;

        return PublishAsync(sendBytes.AsMemory(), routingKey, priority);
    }

    /// <summary>
    /// Publishes a raw message body to the configured exchange using the provided routing key.
    /// </summary>
    /// <param name="body">The message body to publish.</param>
    /// <param name="routingKey">The routing key (usually the queue name) to which the message will be routed.</param>
    /// <param name="priority">Message priority (converted to a byte). Defaults to 1.</param>
    /// <returns>A completed <see cref="Task"/> once the publish call has been issued.</returns>
    /// <remarks>
    /// The message is published with persistent delivery mode and the provided priority.
    /// The <c>mandatory</c> flag is set to <c>true</c> when calling <see cref="IChannel.BasicPublishAsync"/>,
    /// which means the broker will return unroutable messages if they cannot be routed.
    /// Exceptions from the client library may be thrown for connection/channel issues.
    /// </remarks>
    private Task PublishAsync(ReadOnlyMemory<byte> body, string routingKey, int priority = 1)
    {
        var props = new BasicProperties
        {
            Persistent = true,
            Priority = Convert.ToByte(priority)
        };

        Channel.BasicPublishAsync(messageManagerSettings.ExchangeName, routingKey, true, props, body);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Acknowledges that the specified delivered message has been processed successfully.
    /// </summary>
    /// <param name="message">The delivered message whose delivery tag will be acknowledged.</param>
    /// <remarks>
    /// This forwards the acknowledgement to the channel via <see cref="IChannel.BasicAckAsync"/>.
    /// Any exceptions from the client will propagate to the caller.
    /// </remarks>
    public void MarkAsComplete(BasicDeliverEventArgs message) => Channel.BasicAckAsync(message.DeliveryTag, false);

    /// <summary>
    /// Rejects the specified delivered message without requeueing.
    /// </summary>
    /// <param name="message">The delivered message to reject.</param>
    /// <remarks>
    /// This forwards the rejection to the channel via <see cref="IChannel.BasicRejectAsync"/> with <c>requeue=false</c>.
    /// Any exceptions from the client will propagate to the caller.
    /// </remarks>
    public void MarkAsRejected(BasicDeliverEventArgs message) => Channel.BasicRejectAsync(message.DeliveryTag, false);

    /// <summary>
    /// Asynchronously disposes the channel and connection managed by this instance.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    /// <remarks>
    /// The implementation attempts to close the channel and connection if they are open.
    /// Any exceptions thrown while closing are caught and ignored to ensure best-effort cleanup.
    /// After disposal, <see cref="GC.SuppressFinalize(object)"/> is called to prevent finalization.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Channel.IsOpen)
            {
                await Channel.CloseAsync().ConfigureAwait(false);
            }

            if (Connection.IsOpen)
            {
                await Connection.CloseAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // Ignore exceptions on dispose
        }

        GC.SuppressFinalize(this);
    }
}