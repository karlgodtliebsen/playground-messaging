namespace Maui.Prometheus.Viewer.PageModels;

public class MetricInfo
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = "0";
    public string Unit { get; set; } = string.Empty;
    public Color Color { get; set; } = Colors.Blue;
}