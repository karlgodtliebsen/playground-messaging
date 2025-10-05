using RabbitMQ.Client;

namespace Messaging.RabbitMq.Library.Configuration;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMqOptions";

    public string HostName { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = ConnectionFactory.DefaultVHost;
    public string UserName { get; set; } = ConnectionFactory.DefaultUser;
    public string Password { get; set; } = ConnectionFactory.DefaultPass;
    public TimeSpan Heartbeat { get; set; } = ConnectionFactory.DefaultHeartbeat;
    public TimeSpan DefaultConnectionTimeout { get; set; } = ConnectionFactory.DefaultConnectionTimeout;

}