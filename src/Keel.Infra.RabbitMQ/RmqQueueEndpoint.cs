using Microsoft.Extensions.Configuration;

namespace Keel.Infra.RabbitMQ;

public class RmqQueueEndpoint
{
    public string Server { get; set; }
    public int Port { get; set; }
    public string User { get; set; }
    public string Pwd { get; set; }
    public string Name { get; set; }
    public string Exchange { get; set; }

    public override string ToString() => $"{nameof(Server)}: {Server}, {nameof(Port)}: {Port}, {nameof(User)}: {User}, {nameof(Pwd)}: {Pwd}, {nameof(Name)}: {Name}, {nameof(Exchange)}: {Exchange}";
}