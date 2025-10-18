using AsyncAwaitBestPractices;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Messaging.EventHub.Library;

using Microsoft.Extensions.Logging;

namespace Maui.Prometheus.Viewer.PageModels;

public partial class SystemMetricsViewModel : ViewModelBase
{
    private readonly IEventHub eventHub;
    private readonly IPrometheusService prometheusService;
    private readonly ILogger<SystemMetricsViewModel> logger;
    private System.Timers.Timer? refreshTimer;
    private int refreshIntervalSeconds = 5;
    private int lookbackPeriodMinutes = 15;
    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string lastUpdated = "Never";
    [ObservableProperty]
    private string connectionStatus = "Disconnected";

    // Scalar Metrics
    [ObservableProperty]
    private double memoryUsageMB;

    [ObservableProperty]
    private double memoryUsageBytes;

    [ObservableProperty]
    private double gcRate;

    [ObservableProperty]
    private string memoryUsageFormatted = "0.0 MB";

    // Chart Data Collections
    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> memoryUsageData = new();

    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> gcCollectionsData = new();

    // Statistics
    [ObservableProperty]
    private double memoryMin;

    [ObservableProperty]
    private double memoryMax;

    [ObservableProperty]
    private double memoryAvg;
    private readonly CancellationTokenSource cancellationTokenSource;

    public SystemMetricsViewModel(IEventHub eventHub, IPrometheusService prometheusService, CancellationTokenSource cancellationTokenSource, ILogger<SystemMetricsViewModel> logger) : base("System Metrics")
    {
        this.cancellationTokenSource = cancellationTokenSource;
        this.eventHub = eventHub;
        this.prometheusService = prometheusService;
        this.logger = logger;
        Add(eventHub.Subscribe("system-metrics-initialize", InitializeAsync));
        Add(eventHub.Subscribe("system-metrics-stop", StopAutoRefresh));
        Add(eventHub.Subscribe("system-metrics-cleanup", Cleanup));
        Add(eventHub.Subscribe("system-metrics-data-all-reload", LoadAllDataAsync));
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await TestConnectionAsync();

        if (IsConnected)
        {
            StartAutoRefresh();
        }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            IsBusy = true;
            IsConnected = await prometheusService.TestConnection(cancellationTokenSource.Token);
            ConnectionStatus = IsConnected ? "✅ Connected to Prometheus" : "❌ Disconnected from Prometheus";
            logger.LogInformation("Prometheus connection test: {Status}", ConnectionStatus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connection test failed");
            IsConnected = false;
            ConnectionStatus = "❌ Connection Failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAllDataAsync(cancellationTokenSource.Token);
    }

    private async Task LoadAllDataAsync(CancellationToken cancellationToken)
    {
        await Task.Run(LoadAllMetricsAsync, cancellationToken);
    }
    private Task LoadAllMetricsAsync()
    {
        if (IsBusy) return Task.CompletedTask;

        try
        {
            var (start, end) = GetPeriod();
            // Load scalar metrics
            LoadCurrentMetricsAsync().SafeFireAndForget();
            // Load time series data
            LoadMemoryUsageDataAsync(start, end).SafeFireAndForget();
            LoadGCCollectionsDataAsync(start, end).SafeFireAndForget();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh System metrics");
        }
        return Task.CompletedTask;
    }

    private async Task LoadCurrentMetricsAsync()
    {
        try
        {
            // Get current memory usage in bytes
            MemoryUsageBytes = await prometheusService.GetCurrentMemoryUsage();

            // Convert to MB for display
            MemoryUsageMB = MemoryUsageBytes / (1024 * 1024);

            // Format for display
            if (MemoryUsageMB > 1024)
            {
                MemoryUsageFormatted = $"{MemoryUsageMB / 1024:F2} GB";
            }
            else
            {
                MemoryUsageFormatted = $"{MemoryUsageMB:F1} MB";
            }

            // Get GC rate
            GcRate = await prometheusService.GetGCRate();

            logger.LogDebug("Current metrics - Memory: {Memory:F1} MB, GC Rate: {GCRate:F2}", MemoryUsageMB, GcRate);
            await CalculateMemoryStatistics();

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load current metrics");
            MemoryUsageMB = 0;
            GcRate = 0;
        }
    }

    private async Task LoadMemoryUsageDataAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetMemoryUsage(start, end, "15s");

            // Convert bytes to MB for chart display
            var dataInMB = data.Select(dp => new DataPoint
            {
                Timestamp = dp.Timestamp,
                Value = dp.Value / (1024 * 1024), // Convert to MB
                Series = dp.Series
            }).ToList();

            await UpdateCollection(MemoryUsageData, dataInMB);

            logger.LogDebug("Loaded {Count} memory usage data points", dataInMB.Count);
            await CalculateMemoryStatistics();

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load memory usage data");
            MemoryUsageData.Clear();
        }
    }

    private async Task LoadGCCollectionsDataAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetGCCollections(start, end, "15s");
            await UpdateCollection(GcCollectionsData, data);

            logger.LogDebug("Loaded {Count} GC collections data points", data.Count);
            await CalculateMemoryStatistics();

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load GC collections data");
            GcCollectionsData.Clear();
        }
    }

    private async Task CalculateMemoryStatistics()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
          {
              if (MemoryUsageData.Count == 0)
              {
                  MemoryMin = 0;
                  MemoryMax = 0;
                  MemoryAvg = 0;
                  return;
              }

              var values = MemoryUsageData.Select(dp => dp.Value).ToList();

              MemoryMin = values.Min();
              MemoryMax = values.Max();
              MemoryAvg = values.Average();
              LastUpdated = DateTime.Now.ToString("HH:mm:ss");
              logger.LogDebug("Memory statistics - Min: {Min:F1} MB, Max: {Max:F1} MB, Avg: {Avg:F1} MB", MemoryMin, MemoryMax, MemoryAvg);
          });
    }
    private (DateTime start, DateTime end) GetPeriod()
    {
        var end = DateTime.UtcNow;
        var start = end.AddMinutes(-lookbackPeriodMinutes);
        return (start, end);
    }

    private async Task UpdateCollection<T>(ObservableRangeCollection<T> collection, List<T> newData)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            collection.ReplaceRange(newData);
        });
    }

    private void StartAutoRefresh()
    {
        // Auto-refresh every 5 seconds
        refreshTimer = new System.Timers.Timer(TimeSpan.FromSeconds(refreshIntervalSeconds));//TODO: settings
        refreshTimer.Elapsed += async (s, e) =>
        {
            try
            {
                await LoadAllDataAsync(cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-refresh failed");
            }
        };
        refreshTimer.Start();
        logger.LogInformation("Auto-refresh started for System metrics");
    }

    private Task StopAutoRefresh(CancellationToken cancellationToken)
    {
        if (refreshTimer != null)
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();
            refreshTimer = null;
            logger.LogInformation("Auto-refresh stopped for System metrics");
        }
        return Task.CompletedTask;
    }

    // Cleanup when view model is disposed or page is navigated away from
    private async Task Cleanup(CancellationToken cancellationToken)
    {
        await StopAutoRefresh(cancellationToken);
    }

}