using System.Text.Json.Serialization;

namespace Maui.Prometheus.Viewer.Data;

[JsonSerializable(typeof(PrometheusData))]
[JsonSerializable(typeof(PrometheusResponse))]
[JsonSerializable(typeof(PrometheusResponse))]

public partial class JsonContext : JsonSerializerContext
{
}