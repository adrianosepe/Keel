using Keel.Domain.CleanCode.Async;
using MassTransit;

namespace Keel.Infra.Broker.RabbitMQ;

public class KeelProducer<TMessage>(ISendEndpointProvider provider, string queue) : IKeelProducer<TMessage> where TMessage : class
{
    private readonly AsyncLazy<ISendEndpoint> _lazySendEndpointAsync  = new(() => provider.GetSendEndpoint(new Uri($"queue:{queue}")));
    
    public async Task ProduceAsync(TMessage message)
    {
        var sendEndpoint = await _lazySendEndpointAsync.Value;
        await sendEndpoint.Send(message);
    }
}