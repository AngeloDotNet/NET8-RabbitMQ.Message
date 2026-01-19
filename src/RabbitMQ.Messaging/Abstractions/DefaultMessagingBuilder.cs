using Microsoft.Extensions.DependencyInjection;

namespace RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Default implementation of <see cref="IMessagingBuilder"/> used by the library to
/// expose the application's <see cref="IServiceCollection"/> for messaging-related service registrations.
/// </summary>
internal class DefaultMessagingBuilder : IMessagingBuilder
{
    /// <summary>
    /// Gets the application's <see cref="IServiceCollection"/> used to register messaging services.
    /// </summary>
    /// <value>
    /// The <see cref="IServiceCollection"/> instance provided by the application's dependency injection container.
    /// </value>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagingBuilder"/> class.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to use for registering messaging services. Must not be <c>null</c>.</param>
    public DefaultMessagingBuilder(IServiceCollection services) => Services = services;
}