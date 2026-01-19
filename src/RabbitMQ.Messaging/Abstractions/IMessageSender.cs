namespace RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Defines a publisher capable of sending messages to the messaging infrastructure.
/// Implementations are responsible for serializing and delivering messages to the configured exchange/queue.
/// </summary>
public interface IMessageSender
{
    /// <summary>
    /// Publishes a message asynchronously with an optional priority.
    /// </summary>
    /// <typeparam name="T">The message payload type. Must be a reference type.</typeparam>
    /// <param name="message">The message instance to publish. Implementations can assume this is not null.</param>
    /// <param name="priority">
    /// An integer that indicates the message priority. The default value is <c>1</c>.
    /// The interpretation of priority depends on the messaging topology and transport support.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous publish operation. The task completes when the publish request
    /// has been handed off to the messaging infrastructure (or when the publish has definitively failed).
    /// </returns>
    /// <remarks>
    /// Implementations should honor the <paramref name="priority"/> where the transport supports it.
    /// Exceptions thrown from this method typically indicate unrecoverable publish failures; callers or hosting
    /// infrastructure may choose to retry on transient errors.
    /// </remarks>
    Task PublishAsync<T>(T message, int priority = 1) where T : class;
}
