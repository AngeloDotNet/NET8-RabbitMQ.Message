using Microsoft.Extensions.DependencyInjection;

namespace RabbitMQ.Messaging.Abstractions;

public interface IMessagingBuilder
{
    IServiceCollection Services { get; }
}
