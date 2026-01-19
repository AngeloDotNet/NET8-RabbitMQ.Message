using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Messaging.Abstractions;

namespace RabbitMQ.Messaging;

/// <summary>
/// Extension methods to register RabbitMQ messaging components into an <see cref="IServiceCollection"/>.
/// </summary>
public static class RabbitMQExtensions
{
    /// <summary>
    /// Registers RabbitMQ messaging core services and applies provided configuration actions.
    /// </summary>
    /// <param name="services">The service collection to add RabbitMQ messaging services to.</param>
    /// <param name="messageManagerConfiguration">
    /// An action to configure <see cref="MessageManagerSettings"/> used by the messaging system.
    /// The provided <see cref="MessageManagerSettings"/> instance will be populated by this action and registered as a singleton.
    /// </param>
    /// <param name="queuesConfiguration">
    /// An action to configure <see cref="QueueSettings"/> used to configure queues and listeners.
    /// The provided <see cref="QueueSettings"/> instance will be populated by this action and registered as a singleton.
    /// </param>
    /// <returns>An <see cref="IMessagingBuilder"/> that can be used to further configure messaging receivers.</returns>
    /// <remarks>
    /// This method registers the following services:
    /// - <see cref="MessageManager"/> as a singleton.
    /// - <see cref="IMessageSender"/> resolved to the registered <see cref="MessageManager"/>.
    /// - The configured <see cref="MessageManagerSettings"/> and <see cref="QueueSettings"/> as singletons.
    /// It returns a <see cref="DefaultMessagingBuilder"/> wrapping the provided <paramref name="services"/>.
    /// </remarks>
    public static IMessagingBuilder AddRabbitMq(this IServiceCollection services, Action<MessageManagerSettings> messageManagerConfiguration,
        Action<QueueSettings> queuesConfiguration)
    {
        services.AddSingleton<MessageManager>();
        services.AddSingleton<IMessageSender>(provider => provider.GetService<MessageManager>()!);

        var messageManagerSettings = new MessageManagerSettings();
        messageManagerConfiguration.Invoke(messageManagerSettings);
        services.AddSingleton(messageManagerSettings);

        var queueSettings = new QueueSettings();
        queuesConfiguration.Invoke(queueSettings);
        services.AddSingleton(queueSettings);

        return new DefaultMessagingBuilder(services);
    }

    /// <summary>
    /// Registers a message receiver and a hosted queue listener for the specified message type.
    /// </summary>
    /// <typeparam name="TObject">The CLR type of messages this receiver will handle. Must be a reference type.</typeparam>
    /// <typeparam name="TReceiver">
    /// The concrete receiver type that implements <see cref="IMessageReceiver{TObject}"/>.
    /// Must be a reference type and registered as a transient service.
    /// </typeparam>
    /// <param name="builder">The messaging builder used to add receiver registrations.</param>
    /// <returns>The same <see cref="IMessagingBuilder"/> instance to allow fluent chaining.</returns>
    /// <remarks>
    /// This method registers:
    /// - A hosted service <see cref="QueueListener{TObject}"/> that will listen for messages of type <typeparamref name="TObject"/>.
    /// - A transient <see cref="IMessageReceiver{TObject}"/> implementation of type <typeparamref name="TReceiver"/>.
    /// Use this method for each message type you want the application to consume.
    /// </remarks>
    public static IMessagingBuilder AddReceiver<TObject, TReceiver>(this IMessagingBuilder builder)
        where TObject : class
        where TReceiver : class, IMessageReceiver<TObject>
    {
        builder.Services.AddHostedService<QueueListener<TObject>>();
        builder.Services.AddTransient<IMessageReceiver<TObject>, TReceiver>();

        return builder;
    }
}
