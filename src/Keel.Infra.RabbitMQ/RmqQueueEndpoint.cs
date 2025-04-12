namespace Keel.Infra.RabbitMQ;

public class RmqQueueEndpoint
{
    public string Server { get; set; } = null!;
    public int Port { get; set; }
    public string User { get; set; } = null!;
    public string Pwd { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Exchange { get; set; } = null!;

    public override string ToString() => $"{nameof(Server)}: {Server}, {nameof(Port)}: {Port}, {nameof(User)}: {User}, {nameof(Pwd)}: {Pwd}, {nameof(Name)}: {Name}, {nameof(Exchange)}: {Exchange}";
}