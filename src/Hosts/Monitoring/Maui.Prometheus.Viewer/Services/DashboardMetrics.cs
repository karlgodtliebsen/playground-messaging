namespace Maui.Prometheus.Viewer.Services;

// Model for dashboard overview
public class DashboardMetrics
{
    public double TotalEventsPerSecond { get; set; }
    public int ActiveSubscribers { get; set; }
    public int ActiveChannels { get; set; }
    public double AverageProcessingTime { get; set; }
    public double ErrorRate { get; set; }
    public double MemoryUsageMB { get; set; }
    public double GCRate { get; set; }
    public List<DataPoint> EventRateData { get; set; } = new();
    public Dictionary<string, double> EventDistribution { get; set; } = new();
}