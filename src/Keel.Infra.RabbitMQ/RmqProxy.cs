using JetBrains.Annotations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Keel.Infra.RabbitMQ;

[UsedImplicitly]
public class RmqProxy : IRmqProxy
{
    private readonly RmqQueueEndpoint _endpoint;
    private readonly Lazy<ConnectionFactory> _lazyConnFactory;

    private readonly List<RmqSubscriber> _subscribers;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private IChannel? _channel;
    private IConnection? _connection;
    private BasicProperties? _defProperties;

    public RmqProxy(RmqQueueEndpoint endpointOptions)
    {
        _endpoint = endpointOptions;
        _subscribers = [];

        _lazyConnFactory = new Lazy<ConnectionFactory>(
            () => new ConnectionFactory
                {
                    HostName = _endpoint.Server,
                    Port = _endpoint.Port,
                    UserName = _endpoint.User,
                    Password = _endpoint.Pwd,
                });
    }

    private ConnectionFactory ConnFactory => _lazyConnFactory.Value;

    public bool IsConnected => _channel != null;

    public async Task<bool> AddAsync(byte[] data, CancellationToken cancellationToken, byte maxAttempts = 3)
    {
        var i = 0;

        while (true)
        {
            try
            {
                if (i == maxAttempts)
                {
                    return false;
                }

                await _channel!.BasicPublishAsync(
                        exchange: _endpoint.Exchange ?? string.Empty,
                        routingKey: _endpoint.Name,
                        mandatory: true,
                        basicProperties: _defProperties!,
                        body: data,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return true;
            }
            catch (Exception)
            {
                i++;

                await DisconnectAsync();
                await ConnectAsync();
            }
        }
    }

    public async Task<bool> ConnectAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (IsConnected)
            {
                return true;
            }

            try
            {
                _connection = await ConnFactory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                
                // Declara a fila — será criada se não existir
                await _channel.QueueDeclareAsync(
                    queue: _endpoint.Name,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _defProperties = new BasicProperties
                    {
                        Persistent = true,
                    };

                await Task.WhenAll(_subscribers.Select(sbr => sbr.SubscribeAsync()));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return false;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (!IsConnected)
            {
                return;
            }

            _subscribers.ForEach(sbr => sbr.Unsubscribe());

            _defProperties = null;

            await (_channel?.CloseAsync() ?? Task.CompletedTask);
            _channel = null;

            await (_connection?.CloseAsync() ?? Task.CompletedTask);
            _connection = null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddSubscriberAsync(RmqSubscriber subscriber)
    {
        if (subscriber == null)
        {
            throw new ArgumentNullException(nameof(subscriber));
        }

        await _semaphore.WaitAsync();

        try
        {
            if (_subscribers.Contains(subscriber))
            {
                return;
            }

            _subscribers.Add(subscriber);
            await subscriber.InitializeAsync(this);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal async Task<AsyncEventingBasicConsumer> CreateConsumerAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (!IsConnected)
            {
                throw new ApplicationException("There isn't connection to create a consumer.");
            }

            var consumer = new AsyncEventingBasicConsumer(_channel!);

            await _channel!.BasicConsumeAsync(_endpoint.Name, autoAck: false, consumer);

            return consumer;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}