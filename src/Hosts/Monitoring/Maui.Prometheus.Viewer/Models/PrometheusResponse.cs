using System.Text.Json.Serialization;

namespace Maui.Prometheus.Viewer.PageModels;

public class PrometheusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public PrometheusData Data { get; set; } = new();
}