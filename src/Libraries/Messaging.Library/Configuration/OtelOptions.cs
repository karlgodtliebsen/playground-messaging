namespace Messaging.Library.Configuration;

public sealed class OtelOptions
{
    public const string SectionName = "OTEL";

    public string? ServiceName { get; set; }
    public string? ServiceVersion { get; set; }
    public string? Endpoint { get; set; } // e.g. "http://localhost:4317" or "http://localhost:4318"
    public bool UseGrpc { get; set; } = true; // false => HTTP/protobuf
    public bool ExportTraces { get; set; } = true;
    public bool ExportMetrics { get; set; } = true;
    public bool ExportLogs { get; set; } = true; // Only if you are NOT using Serilog’s OTLP sink
    public double SamplingRatio { get; set; } = 0.2;
    public bool DebugMode { get; set; } = false;

    // Instrumentation toggles
    public bool AspNetCore { get; set; } = false;
    public bool HttpClient { get; set; } = false;
    public bool SqlClient { get; set; } = false;
    public bool Runtime { get; set; } = true;
    public bool Process { get; set; } = true;

    // Custom sources/meters you emit yourself
    public string[] Sources { get; set; } = [];
    public string[] Meters { get; set; } = [];
}