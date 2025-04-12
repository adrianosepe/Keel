using RabbitMQ.Client.Events;

namespace Keel.Infra.RabbitMQ;

public class RmqSubscriber
{
    private AsyncEventingBasicConsumer? _consumer;
    private IRmqProxy? _proxy;

    public event CustomAsyncEventHandler<RmqReceivedEventArgs>? Received;

    public async Task<RmqSubscriber> InitializeAsync(IRmqProxy proxy)
    {
        _proxy = proxy;

        await SubscribeAsync();

        return this;
    }

    internal async Task SubscribeAsync()
    {
        Unsubscribe();

        _consumer = await ((RmqProxy)_proxy!).CreateConsumerAsync();
        _consumer.ReceivedAsync += OnConsumerOnReceivedAsync!;
    }

    internal void Unsubscribe()
    {
        if (_consumer != null)
        {
            _consumer.ReceivedAsync -= OnConsumerOnReceivedAsync!;
        }
    }

    private async Task OnConsumerOnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            await (Received?.Invoke(this, new RmqReceivedEventArgs(ea.DeliveryTag, ea.Body))
                ?? Task.CompletedTask)
                .ConfigureAwait(false);

            await ((AsyncEventingBasicConsumer)sender)
                .Channel
                .BasicAckAsync(ea.DeliveryTag, multiple: false)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            //
        }
    }
}

public delegate Task CustomAsyncEventHandler<in TEventArgs>(object? sender, TEventArgs e);