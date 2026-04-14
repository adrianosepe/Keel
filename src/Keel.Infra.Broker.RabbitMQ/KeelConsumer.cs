using MassTransit;

namespace Keel.Infra.Broker.RabbitMQ;

public abstract class KeelConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
    public Task Consume(ConsumeContext<TMessage> context)
    {
        return InternalHandleAsync(context);
    }

    protected abstract Task InternalHandleAsync(ConsumeContext<TMessage> context);
}