using Microsoft.Extensions.Logging;

using System.Net.Http.Json;

namespace Maui.Prometheus.Viewer.Services;

public class PrometheusService(IHttpClientFactory httpClientFactory, ILogger<PrometheusService> logger) : IPrometheusService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("PrometheusClient");

    public async Task<List<DataPoint>> QueryRange(string query, DateTime start, DateTime end, string step = "15s")
    {
        try
        {
            var startUnix = new DateTimeOffset(start).ToUnixTimeSeconds();
            var endUnix = new DateTimeOffset(end).ToUnixTimeSeconds();

            var url = $"/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={startUnix}&end={endUnix}&step={step}";

            var response = await httpClient.GetFromJsonAsync<PrometheusResponse>(url);

            if (response?.Status != "success" || response.Data.Result.Count == 0)
                return new List<DataPoint>();

            var dataPoints = new List<DataPoint>();

            foreach (var result in response.Data.Result)
            {
                var seriesName = result.Metric.ContainsKey("event_name")
                    ? result.Metric["event_name"]
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

    public async Task<double> QueryInstant(string query)
    {
        try
        {
            var url = $"/api/v1/query?query={Uri.EscapeDataString(query)}";
            var response = await httpClient.GetFromJsonAsync<PrometheusResponse>(url);

            if (response?.Status != "success" || response.Data.Result.Count == 0)
                return 0;

            // Handle instant query response (has "value" not "values")
            var value = response.Data.Result[0].Value?[1].GetString()
                        ?? response.Data.Result[0].Values.FirstOrDefault()?[1].GetString();

            return double.TryParse(value, out var result) ? result : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error querying Prometheus: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> TestConnection()
    {
        try
        {
            var url = "/api/v1/query?query=up";
            var response = await httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}