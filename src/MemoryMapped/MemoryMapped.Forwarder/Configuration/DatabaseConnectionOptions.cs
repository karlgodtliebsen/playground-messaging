namespace MemoryMapped.Forwarder.Configuration;

public class DatabaseConnectionOptions
{
    public const string SectionName = "DatabaseConnection";

    public string ConnectionString { get; set; } = null!;
}