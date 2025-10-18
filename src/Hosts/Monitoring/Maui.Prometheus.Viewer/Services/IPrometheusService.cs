namespace Maui.Prometheus.Viewer.Services;

public interface IPrometheusService
{
    Task<List<DataPoint>> QueryRange(string query, DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default);
    Task<double> QueryInstant(string query, CancellationToken cancellationToken = default);
    Task<bool> TestConnection(CancellationToken cancellationToken = default);
    Task<double> GetEventsPublishedRate(CancellationToken cancellationToken = default);
    Task<List<DataPoint>> GetEventsPublishedRateByEvent(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default);
    Task<double> GetEventsProcessedRate(CancellationToken cancellationToken = default);
    Task<double> GetAverageProcessingTime(CancellationToken cancellationToken = default);
    Task<List<DataPoint>> GetProcessingDuration(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default);
    Task<double> GetActiveSubscribers(CancellationToken cancellationToken = default);
    Task<double> GetActiveChannels(CancellationToken cancellationToken = default);
    Task<double> GetErrorRate(CancellationToken cancellationToken = default);
    Task<List<DataPoint>> GetErrorRateByEvent(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default);
    Task<List<DataPoint>> GetOperationDuration(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default);
    Task<Dictionary<string, double>> GetEventDistribution(CancellationToken cancellationToken = default);
    Task<List<DataPoint>> GetMemoryUsage(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default);
    Task<double> GetCurrentMemoryUsage(CancellationToken cancellationToken = default);
    Task<List<DataPoint>> GetGCCollections(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default);
    Task<double> GetGCRate(CancellationToken cancellationToken = default);
    Task<DashboardMetrics> GetDashboardMetrics(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default);
}