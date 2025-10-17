namespace Maui.Prometheus.Viewer.Services;

public interface IPrometheusService
{
    Task<List<DataPoint>> QueryRange(string query, DateTime start, DateTime end, string step = "15s");
    Task<double> QueryInstant(string query);
    Task<bool> TestConnection();
}