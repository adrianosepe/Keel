namespace Keel.Infra.Broker.RabbitMQ;

public interface IKeelProducer
{
}

public interface IKeelProducer<in TMessage> : IKeelProducer
{
    Task ProduceAsync(TMessage message);
}
