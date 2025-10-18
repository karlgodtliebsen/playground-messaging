using Microsoft.Extensions.Logging;

using System.Net.Http.Json;

namespace Maui.Prometheus.Viewer.Services;


public class PrometheusService(IHttpClientFactory httpClientFactory, ILogger<PrometheusService> logger) : IPrometheusService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("PrometheusClient");

    public async Task<List<DataPoint>> QueryRange(string query, DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default)
    {
        try
        {
            var startUnix = new DateTimeOffset(start).ToUnixTimeSeconds();
            var endUnix = new DateTimeOffset(end).ToUnixTimeSeconds();

            var url = $"/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={startUnix}&end={endUnix}&step={step}";

            var response = await httpClient.GetFromJsonAsync<PrometheusResponse>(url, cancellationToken);

            if (response?.Status != "success" || response.Data.Result.Count == 0)
                return [];

            var dataPoints = new List<DataPoint>();

            foreach (var result in response.Data.Result)
            {
                var seriesName = result.Metric.TryGetValue("event_name", out var v)
                    ? v
                    : result.Metric.ContainsKey("__name__") ? result.Metric["__name__"] : "default";

                foreach (var value in result.Values)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeSeconds((long)value[0].GetDouble()).DateTime;
                    var metricValue = double.Parse(value[1].GetString() ?? "0");

                    dataPoints.Add(new DataPoint
                    {
                        Timestamp = timestamp,
                        Value = metricValue,
                        Series = seriesName
                    });
                }
            }

            return dataPoints;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying Prometheus at EndPoint: {EndPoint}", httpClient.BaseAddress);
            System.Diagnostics.Debug.WriteLine($"Error querying Prometheus: {ex.Message}");
            return new List<DataPoint>();
        }
    }

    public async Task<double> QueryInstant(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/v1/query?query={Uri.EscapeDataString(query)}";
            var response = await httpClient.GetFromJsonAsync<PrometheusResponse>(url, cancellationToken);

            if (response?.Status != "success" || response.Data.Result.Count == 0)
                return 0;

            // Handle instant query response (has "value" not "values")
            var value = response.Data.Result[0].Value?[1].GetString()
                        ?? response.Data.Result[0].Values.FirstOrDefault()?[1].GetString();

            return double.TryParse(value, out var result) ? result : 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying Prometheus at EndPoint: {EndPoint}", httpClient.BaseAddress);
            System.Diagnostics.Debug.WriteLine($"Error querying Prometheus: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> TestConnection(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = "/api/v1/query?query=up";
            var response = await httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // EventHub Metrics
    public async Task<double> GetEventsPublishedRate(CancellationToken cancellationToken = default)
    {
        var query = "sum(rate(messaging_events_published_count_total[1m]))";
        return await QueryInstant(query, cancellationToken);
    }

    public async Task<List<DataPoint>> GetEventsPublishedRateByEvent(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default)
    {
        var query = "sum(rate(messaging_events_published_count_total[1m])) by (event_name)";
        return await QueryRange(query, start, end, step, cancellationToken);
    }

    public async Task<double> GetEventsProcessedRate(CancellationToken cancellationToken = default)
    {
        var query = "sum(rate(messaging_events_processed_count_total[1m]))";
        return await QueryInstant(query, cancellationToken);
    }

    public async Task<double> GetAverageProcessingTime(CancellationToken cancellationToken = default)
    {
        var query = "rate(messaging_event_processing_duration_milliseconds_sum[5m]) / rate(messaging_event_processing_duration_milliseconds_count[5m])";
        return await QueryInstant(query, cancellationToken);
    }

    public async Task<List<DataPoint>> GetProcessingDuration(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default)
    {
        var queries = new Dictionary<string, string>
        {
            { "avg", "rate(messaging_event_processing_duration_milliseconds_sum[5m]) / rate(messaging_event_processing_duration_milliseconds_count[5m])" },
            { "p95", "histogram_quantile(0.95, rate(messaging_event_processing_duration_milliseconds_bucket[5m]))" }
        };

        var allPoints = new List<DataPoint>();
        foreach (var kvp in queries)
        {
            var points = await QueryRange(kvp.Value, start, end, step, cancellationToken);
            foreach (var point in points)
            {
                point.Series = $"{kvp.Key}: {point.Series}";
            }
            allPoints.AddRange(points);
        }
        return allPoints;
    }

    public async Task<double> GetActiveSubscribers(CancellationToken cancellationToken = default)
    {
        var query = "messaging_active_subscribers_count";
        return await QueryInstant(query, cancellationToken);
    }

    public async Task<double> GetActiveChannels(CancellationToken cancellationToken = default)
    {
        var query = "messaging_active_channels_count";
        return await QueryInstant(query, cancellationToken);
    }

    public async Task<double> GetErrorRate(CancellationToken cancellationToken = default)
    {
        var query = "rate(messaging_handler_errors_total[1m]) or vector(0)";
        return await QueryInstant(query, cancellationToken);
    }

    public async Task<List<DataPoint>> GetErrorRateByEvent(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default)
    {
        var query = "rate(messaging_handler_errors_total[1m]) or vector(0)";
        return await QueryRange(query, start, end, step, cancellationToken);
    }

    // ActivitySource Metrics
    public async Task<List<DataPoint>> GetOperationDuration(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default)
    {
        var queries = new Dictionary<string, string>
        {
            { "avg", "rate(messaging_operation_duration_milliseconds_sum[5m]) / rate(messaging_operation_duration_milliseconds_count[5m])" },
            { "p95", "histogram_quantile(0.95, rate(messaging_operation_duration_milliseconds_bucket[5m]))" }
        };

        var allPoints = new List<DataPoint>();
        foreach (var kvp in queries)
        {
            var points = await QueryRange(kvp.Value, start, end, step, cancellationToken);
            foreach (var point in points)
            {
                point.Series = $"{kvp.Key}: {point.Series}";
            }
            allPoints.AddRange(points);
        }
        return allPoints;
    }

    // Event Distribution (Pie Chart)
    public async Task<Dictionary<string, double>> GetEventDistribution(CancellationToken cancellationToken = default)
    {
        var query = "sum(rate(messaging_events_published_count_total[5m])) by (event_name)";
        var url = $"/api/v1/query?query={Uri.EscapeDataString(query)}";

        try
        {
            var response = await httpClient.GetFromJsonAsync<PrometheusResponse>(url, cancellationToken);
            if (response?.Status != "success")
                return new Dictionary<string, double>();

            var distribution = new Dictionary<string, double>();
            foreach (var result in response.Data.Result)
            {
                var eventName = result.Metric.TryGetValue("event_name", out var v) ? v : "unknown";
                var value = double.Parse(result.Value?[1].GetString() ?? "0");
                distribution[eventName] = value;
            }
            return distribution;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting event distribution");
            return new Dictionary<string, double>();
        }
    }

    // System Metrics
    public async Task<List<DataPoint>> GetMemoryUsage(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default)
    {
        var query = "messaging_process_memory_usage_bytes";
        return await QueryRange(query, start, end, step, cancellationToken);
    }

    public async Task<double> GetCurrentMemoryUsage(CancellationToken cancellationToken = default)
    {
        var query = "messaging_process_memory_usage_bytes";
        return await QueryInstant(query, cancellationToken);
    }

    public async Task<List<DataPoint>> GetGCCollections(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default)
    {
        var query = "rate(messaging_dotnet_gc_collections_total[1m])";
        return await QueryRange(query, start, end, step, cancellationToken);
    }

    public async Task<double> GetGCRate(CancellationToken cancellationToken = default)
    {
        var query = "sum(rate(messaging_dotnet_gc_collections_total[1m]))";
        return await QueryInstant(query, cancellationToken);
    }

    // Aggregate metrics for dashboard overview
    public async Task<DashboardMetrics> GetDashboardMetrics(DateTime start, DateTime end, string step = "15s", CancellationToken cancellationToken = default)
    {
        return new DashboardMetrics
        {
            TotalEventsPerSecond = await GetEventsPublishedRate(cancellationToken),
            ActiveSubscribers = (int)await GetActiveSubscribers(cancellationToken),
            ActiveChannels = (int)await GetActiveChannels(cancellationToken),
            AverageProcessingTime = await GetAverageProcessingTime(cancellationToken),
            ErrorRate = await GetErrorRate(cancellationToken),
            MemoryUsageMB = await GetCurrentMemoryUsage(cancellationToken) / (1024 * 1024),
            GCRate = await GetGCRate(cancellationToken),
            EventRateData = await GetEventsPublishedRateByEvent(start, end, step, cancellationToken),
            EventDistribution = await GetEventDistribution(cancellationToken)
        };
    }
}

