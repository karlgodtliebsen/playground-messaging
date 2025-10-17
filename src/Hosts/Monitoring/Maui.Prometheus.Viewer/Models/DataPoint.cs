namespace Maui.Prometheus.Viewer.PageModels;

public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string Series { get; set; } = string.Empty;
}