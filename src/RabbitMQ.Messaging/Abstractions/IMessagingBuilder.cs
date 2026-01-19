using Microsoft.Extensions.DependencyInjection;

namespace RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Provides a builder abstraction for configuring messaging-related services.
/// Implementations expose the underlying <see cref="IServiceCollection"/> so callers
/// can register and configure dependencies required by the messaging components.
/// </summary>
public interface IMessagingBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used to register messaging services.
    /// </summary>
    /// <remarks>
    /// Use this collection to register message handlers, serializers, connectivity components,
    /// and any other services required by the messaging infrastructure. Registrations will
    /// be applied to the host application's dependency injection container.
    /// </remarks>
    IServiceCollection Services { get; }
}
