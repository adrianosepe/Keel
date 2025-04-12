namespace Keel.Infra.RabbitMQ;

public interface IRmqProxy
{
    bool IsConnected { get; }
    Task<bool> AddAsync(byte[] data, CancellationToken cancellationToken, byte maxAttempts = 3);
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task AddSubscriberAsync(RmqSubscriber subscriber);
}