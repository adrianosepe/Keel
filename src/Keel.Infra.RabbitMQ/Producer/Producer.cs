using System.Text.Json;
using RabbitMQ.Client;

namespace Keel.Infra.RabbitMQ.Producer;

public class Producer<TEnvelope>(IConnection connection) : IProducer<TEnvelope>
    where TEnvelope : IEnvelope
{
    public async Task PublishAsync(TEnvelope envelope, CancellationToken cancellationToken)
    {
        if (envelope == null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        var payload = new MemoryStream();
        await JsonSerializer.SerializeAsync(payload, envelope.Payload, cancellationToken: cancellationToken);
        payload.Position = 0;

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await channel!.BasicPublishAsync(
                exchange: envelope.Exchange,
                routingKey: envelope.QueueName,
                mandatory: true,
                basicProperties: new BasicProperties
                {
                    Persistent = true,
                },
                body: payload.ToArray(),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}

public interface IProducer<in TEnvelope>
    where TEnvelope : IEnvelope
{
    Task PublishAsync(TEnvelope envelope, CancellationToken cancellationToken);
}

public interface IEnvelope
{
    string Exchange { get; }
    string QueueName { get; }
    object Payload { get; }
}