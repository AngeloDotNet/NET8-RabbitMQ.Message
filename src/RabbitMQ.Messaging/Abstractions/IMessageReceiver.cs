namespace RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Defines a contract for receiving messages of a specific reference type.
/// Implementations handle processing of incoming messages delivered by the messaging infrastructure.
/// </summary>
/// <typeparam name="T">The message payload type. Must be a reference type.</typeparam>
public interface IMessageReceiver<T> where T : class
{
    /// <summary>
    /// Processes an incoming message asynchronously.
    /// </summary>
    /// <param name="message">The received message instance to process. Implementations can assume this is not null.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// The task completes when message processing (including any acknowledgements or persistence) is finished.
    /// </returns>
    /// <remarks>
    /// Implementations should observe the <paramref name="cancellationToken"/> and ensure
    /// any required message acknowledgement or error handling is performed according to the messaging topology.
    /// Exceptions thrown from this method will typically be surfaced to the message dispatching infrastructure.
    /// </remarks>
    Task ReceiveAsync(T message, CancellationToken cancellationToken);
}
