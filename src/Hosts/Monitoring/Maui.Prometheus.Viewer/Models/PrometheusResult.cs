using System.Text.Json.Serialization;

namespace Maui.Prometheus.Viewer.PageModels;

public class PrometheusResult
{
    [JsonPropertyName("metric")]
    public Dictionary<string, string> Metric { get; set; } = new();

    [JsonPropertyName("values")]
    public List<List<System.Text.Json.JsonElement>> Values { get; set; } = new();

    [JsonPropertyName("value")]
    public List<System.Text.Json.JsonElement>? Value { get; set; }
}