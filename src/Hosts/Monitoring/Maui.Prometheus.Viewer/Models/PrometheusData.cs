using System.Text.Json.Serialization;

namespace Maui.Prometheus.Viewer.PageModels;

public class PrometheusData
{
    [JsonPropertyName("resultType")]
    public string ResultType { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public List<PrometheusResult> Result { get; set; } = new();
}