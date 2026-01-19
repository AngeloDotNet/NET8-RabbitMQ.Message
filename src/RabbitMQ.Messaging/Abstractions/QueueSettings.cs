namespace RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Holds configuration for queues used by the messaging system.
/// </summary>
/// <remarks>
/// This class is used to record queue registrations (queue name and message payload type)
/// which are later consumed by the messaging builder or host configuration to create
/// the appropriate queues and consumer bindings.
/// </remarks>
public class QueueSettings
{
    /// <summary>
    /// Gets the list of configured queues.
    /// Each entry is a tuple containing the queue <see cref="Name"/> and the payload <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// This collection is internal to the library and intended to be consumed by infrastructure code
    /// that wires up queues and message handlers. The list is mutable so registrations can be added
    /// during application startup.
    /// </remarks>
    internal IList<(string Name, Type Type)> Queues { get; } = new List<(string, Type)>();

    /// <summary>
    /// Registers a queue mapping for the specified message payload type.
    /// </summary>
    /// <typeparam name="T">The message payload type. Must be a reference type.</typeparam>
    /// <param name="queueName">
    /// Optional explicit queue name. If <c>null</c> or empty, the fully qualified name of <typeparamref name="T"/>
    /// (<c>typeof(T).FullName</c>) will be used as the queue name.
    /// </param>
    /// <remarks>
    /// Calling this method adds a tuple (queue name, type) to <see cref="Queues"/> which will later be
    /// used to configure message consumers and routing. Implementations should ensure queue names are unique
    /// when necessary.
    /// </remarks>
    public void Add<T>(string queueName = null!) where T : class
    {
        var type = typeof(T);
        Queues.Add((queueName ?? type.FullName!, type));
    }
}
