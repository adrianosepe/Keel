namespace Keel.Infra.RabbitMQ;

public class RmqReceivedEventArgs
{
    public RmqReceivedEventArgs(ulong key, ReadOnlyMemory<byte> body)
    {
        Key = key;
        Body = body;
    }

    public ulong Key { get; }

    public ReadOnlyMemory<byte> Body { get; }
}