namespace Messaging.Kafka.Library.Configuration;

public class KafkaOptions
{
    public const string SectionName = "KafkaOptions";

    public string HostName { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9094;

    public string ConsumerGroup { get; set; } = "the-consumer-group";
    public string ClientId { get; set; } = "the-client_id";


}