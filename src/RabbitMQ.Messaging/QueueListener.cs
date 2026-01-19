using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Messaging.Abstractions;

namespace RabbitMQ.Messaging;

/// <summary>
/// Background service that listens on a RabbitMQ queue and dispatches incoming messages of type <typeparamref name="T"/>
/// to a scoped <see cref="IMessageReceiver{T}"/> for processing.
/// </summary>
/// <typeparam name="T">The message type that will be deserialized from the queue body and forwarded to the receiver.</typeparam>
internal class QueueListener<T> : BackgroundService where T : class
{
    private readonly MessageManager messageManager;
    private readonly MessageManagerSettings messageManagerSettings;
    private readonly ILogger logger;
    private readonly IServiceProvider serviceProvider;
    private readonly string queueName;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueListener{T}"/> class.
    /// </summary>
    /// <param name="messageManager">Provides the RabbitMQ channel and message acknowledgement helpers.</param>
    /// <param name="messageManagerSettings">Settings used for JSON serialization and related behavior.</param>
    /// <param name="settings">Queue settings used to resolve the actual queue name for the message type <typeparamref name="T"/>.</param>
    /// <param name="logger">Logger used to record lifecycle and error events.</param>
    /// <param name="serviceProvider">Service provider used to create a scope for resolving the <see cref="IMessageReceiver{T}"/>.</param>
    public QueueListener(MessageManager messageManager, MessageManagerSettings messageManagerSettings, QueueSettings settings,
        ILogger<QueueListener<T>> logger, IServiceProvider serviceProvider)
    {
        this.messageManager = messageManager;
        this.messageManagerSettings = messageManagerSettings;
        this.logger = logger;
        this.serviceProvider = serviceProvider;

        queueName = settings.Queues.First(q => q.Type == typeof(T)).Name;
    }

    /// <summary>
    /// Called when the host is starting. Logs the start of the listener and delegates to the base implementation.
    /// </summary>
    /// <param name="cancellationToken">Token to signal start cancellation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous start operation.</returns>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("RabbitMQ Listener for {QueueName} started", queueName);

        return base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Called when the host is stopping. Logs the stop of the listener and delegates to the base implementation.
    /// </summary>
    /// <param name="cancellationToken">Token to signal stop cancellation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous stop operation.</returns>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("RabbitMQ Listener for {QueueName} stopped", queueName);

        return base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Executes the background listener by registering an <see cref="AsyncEventingBasicConsumer"/> on the RabbitMQ channel.
    /// The registered callback:
    /// - Deserializes the incoming message body to <typeparamref name="T"/> using configured JSON options.
    /// - Creates a scoped <see cref="IServiceScope"/> to resolve an <see cref="IMessageReceiver{T}"/>.
    /// - Invokes <see cref="IMessageReceiver{T}.ReceiveAsync(T, CancellationToken)"/>.
    /// - Acknowledges the message via <see cref="MessageManager.MarkAsComplete"/> on success or rejects it via <see cref="MessageManager.MarkAsRejected"/> on failure.
    /// </summary>
    /// <param name="stoppingToken">Token that signals the listener to stop processing.</param>
    /// <returns>A completed <see cref="Task"/> once the consumer has been registered. Message processing continues via the consumer callback.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="stoppingToken"/> requests cancellation before or during execution.</exception>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new AsyncEventingBasicConsumer(messageManager.Channel);
        consumer.ReceivedAsync += async (_, message) =>
        {
            try
            {
                logger.LogDebug("Messaged received: {Request}", Encoding.UTF8.GetString(message.Body.Span));
                using var scope = serviceProvider.CreateScope();

                var receiver = scope.ServiceProvider.GetRequiredService<IMessageReceiver<T>>();
                var response = JsonSerializer.Deserialize<T>(message.Body.Span, messageManagerSettings.JsonSerializerOptions ?? JsonOptions.Default);

                await receiver.ReceiveAsync(response!, stoppingToken);

                messageManager.MarkAsComplete(message);
                logger.LogDebug("Message processed");
            }
            catch (Exception ex)
            {
                messageManager.MarkAsRejected(message);
                logger.LogError(ex, "Unexpected error while processing message");
            }

            stoppingToken.ThrowIfCancellationRequested();
        };

        messageManager.Channel.BasicConsumeAsync(queueName, false, null!, false, false, null, consumer, stoppingToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously disposes the listener's managed resources.
    /// This will dispose the underlying <see cref="MessageManager"/> and call <c>base.Dispose()</c>.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await messageManager.DisposeAsync();
        base.Dispose();
    }
}