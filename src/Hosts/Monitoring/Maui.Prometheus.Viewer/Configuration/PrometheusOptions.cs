namespace Maui.Prometheus.Viewer.Configuration;

public class PrometheusOptions
{
    public const string SectionName = "PrometheusOptions";
    public string Endpoint { get; set; } = "http://127.0.0.1:9090";
}