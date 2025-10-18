using AsyncAwaitBestPractices;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Messaging.EventHub.Library;

using Microsoft.Extensions.Logging;

namespace Maui.Prometheus.Viewer.PageModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IEventHub eventHub = null!;
    private readonly IPrometheusService prometheusService;
    private readonly ILogger<DashboardViewModel> logger;
    private System.Timers.Timer? refreshTimer;
    private int refreshIntervalSeconds = 5;
    private int lookbackPeriodMinutes = 15;
    private string step = "15s";
    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string connectionStatus = "Disconnected";


    [ObservableProperty]
    private string lastUpdated = "Never";

    // EventHub Metrics - Scalar Values
    [ObservableProperty]
    private double totalEventsPerSecond;

    [ObservableProperty]
    private int activeSubscribers;

    [ObservableProperty]
    private int activeChannels;

    [ObservableProperty]
    private double averageProcessingTime;

    [ObservableProperty]
    private double errorRate;

    // System Metrics - Scalar Values
    [ObservableProperty]
    private double memoryUsageMB;

    [ObservableProperty]
    private double gcRate;

    // Chart Data Collections - EventHub
    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> eventRateData = new();



    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> processingDurationData = new();

    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> operationDurationData = new();

    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> errorRateData = new();

    [ObservableProperty]
    private ObservableRangeCollection<KeyValuePair<string, double>> eventDistribution = new();

    // Chart Data Collections - System
    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> memoryUsageData = new();

    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> gcCollectionsData = new();

    // Summary Statistics
    [ObservableProperty]
    private int totalDataPoints;

    [ObservableProperty]
    private string summaryText = "Waiting for data...";
    private readonly CancellationTokenSource cancellationTokenSource;

    public DashboardViewModel(IEventHub eventHub, IPrometheusService prometheusService, CancellationTokenSource cancellationTokenSource, ILogger<DashboardViewModel> logger) : base("Dashboard")
    {
        this.cancellationTokenSource = cancellationTokenSource;
        this.eventHub = eventHub;
        this.prometheusService = prometheusService;
        this.logger = logger;
        Add(eventHub.Subscribe("dashboard-initialize", InitializeAsync));
        Add(eventHub.Subscribe("dashboard-stop", StopAutoRefresh));
        Add(eventHub.Subscribe("dashboard-cleanup", Cleanup));
        Add(eventHub.Subscribe("dashboard-data-all-reload", LoadAllDataAsync));
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing DashboardViewModel");

        await TestConnectionAsync();

        if (IsConnected)
        {
            StartAutoRefresh();
        }
        else
        {
            logger.LogWarning("Dashboard initialization failed - Prometheus not connected");
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

    private Task LoadAllDataAsync(CancellationToken cancellationToken)
    {
        LoadAllMetricsAsync_1().SafeFireAndForget();
        LoadAllMetricsAsync_2().SafeFireAndForget();
        return Task.CompletedTask;
    }

    private (DateTime start, DateTime end) GetPeriod()
    {
        var end = DateTime.UtcNow;
        var start = end.AddMinutes(-lookbackPeriodMinutes);
        return (start, end);
    }

    private async Task LoadAllMetricsAsync_1()
    {
        try
        {
            // Use the aggregate method for efficiency
            var (start, end) = GetPeriod();

            var dashboardMetrics = await prometheusService.GetDashboardMetrics(start, end, step, cancellationTokenSource.Token);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Update scalar metrics from aggregate
                TotalEventsPerSecond = dashboardMetrics.TotalEventsPerSecond;
                ActiveSubscribers = dashboardMetrics.ActiveSubscribers;
                ActiveChannels = dashboardMetrics.ActiveChannels;
                AverageProcessingTime = dashboardMetrics.AverageProcessingTime;
                ErrorRate = dashboardMetrics.ErrorRate;
                MemoryUsageMB = dashboardMetrics.MemoryUsageMB;
                GcRate = dashboardMetrics.GCRate;
            });

            await UpdateCollection(EventRateData, dashboardMetrics.EventRateData.ToList());
            await UpdateCollection(EventDistribution, dashboardMetrics.EventDistribution.ToList());

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load metrics");
            throw;
        }
        await UpdateSummaryText();
    }

    private async Task LoadAllMetricsAsync_2()
    {
        var (start, end) = GetPeriod();
        try
        {
            LoadProcessingDurationAsync(start, end).SafeFireAndForget();
            LoadOperationDurationAsync(start, end).SafeFireAndForget();
            LoadErrorRateDataAsync(start, end).SafeFireAndForget();
            LoadMemoryUsageAsync(start, end).SafeFireAndForget();
            LoadGCCollectionsAsync(start, end).SafeFireAndForget();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load metrics");
            throw;
        }
        await UpdateSummaryText();
    }

    private async Task UpdateTotalDataPoints()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Calculate total data points
            TotalDataPoints = EventRateData.Count +
                              ProcessingDurationData.Count +
                              OperationDurationData.Count +
                              ErrorRateData.Count +
                              MemoryUsageData.Count +
                              GcCollectionsData.Count;
        });

    }

    private async Task LoadProcessingDurationAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetProcessingDuration(start, end, step, cancellationTokenSource.Token);
            await UpdateCollection(ProcessingDurationData, data);
            logger.LogDebug("Loaded {Count} processing duration data points", data.Count);
            await UpdateTotalDataPoints();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load processing duration data");
            ProcessingDurationData.Clear();
        }
    }

    private async Task LoadOperationDurationAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetOperationDuration(start, end, step, cancellationTokenSource.Token);
            await UpdateCollection(OperationDurationData, data);
            logger.LogDebug("Loaded {Count} operation duration data points", data.Count);
            await UpdateTotalDataPoints();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load operation duration data");
            OperationDurationData.Clear();
        }
    }

    private async Task LoadErrorRateDataAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetErrorRateByEvent(start, end, step, cancellationTokenSource.Token);
            await UpdateCollection(ErrorRateData, data);
            logger.LogDebug("Loaded {Count} error rate data points", data.Count);
            await UpdateTotalDataPoints();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load error rate data");
            ErrorRateData.Clear();
        }
    }

    private async Task LoadMemoryUsageAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetMemoryUsage(start, end, step, cancellationTokenSource.Token);

            // Convert bytes to MB for display
            var dataInMB = data.Select(dp => new DataPoint
            {
                Timestamp = dp.Timestamp,
                Value = dp.Value / (1024 * 1024),
                Series = dp.Series
            }).ToList();

            await UpdateCollection(MemoryUsageData, dataInMB);
            logger.LogDebug("Loaded {Count} memory usage data points", dataInMB.Count);
            await UpdateTotalDataPoints();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load memory usage data");
            MemoryUsageData.Clear();
        }
    }

    private async Task LoadGCCollectionsAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetGCCollections(start, end, step, cancellationTokenSource.Token);
            await UpdateCollection(GcCollectionsData, data);
            logger.LogDebug("Loaded {Count} GC collections data points", data.Count);
            await UpdateTotalDataPoints();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load GC collections data");
            GcCollectionsData.Clear();
        }
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
        // Auto-refresh every 'refreshIntervalSeconds' seconds
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

        logger.LogInformation("Auto-refresh started ({refreshIntervalSeconds} second interval)", refreshIntervalSeconds);
    }

    private Task StopAutoRefresh(CancellationToken cancellationToken)
    {
        if (refreshTimer != null)
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();
            refreshTimer = null;
            logger.LogInformation("Auto-refresh stopped");
        }
        return Task.CompletedTask;
    }

    private async Task UpdateSummaryText()
    {
        var lastUpdated = DateTime.Now.ToString("HH:mm:ss");
        var summary = $"📊 {TotalEventsPerSecond:F2} events/s | " +
            $"👥 {ActiveSubscribers} subs | " +
            $"📡 {ActiveChannels} channels | " +
            $"⏱️ {AverageProcessingTime:F1}ms avg | " +
            $"💾 {MemoryUsageMB:F0}MB";

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            LastUpdated = lastUpdated;
            SummaryText = summary;
        });
    }

    // Cleanup method
    private async Task Cleanup(CancellationToken cancellationToken)
    {
        await StopAutoRefresh(cancellationToken);
        logger.LogInformation("DashboardViewModel cleaned up");
    }

    // Optional: Manual connection test
    [RelayCommand]
    private async Task TestConnection()
    {
        await TestConnectionAsync();
    }

    // Optional: Change time range (for future enhancement)
    public async Task ChangeTimeRangeAsync(TimeSpan range)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            logger.LogInformation("Changing time range to {Range}", range);

            var now = DateTime.UtcNow;
            var start = now.Subtract(range);

            await Task.WhenAll(
                LoadProcessingDurationAsync(start, now),
                LoadOperationDurationAsync(start, now),
                LoadErrorRateDataAsync(start, now),
                LoadMemoryUsageAsync(start, now),
                LoadGCCollectionsAsync(start, now)
            );

            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to change time range");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Get health status
    public string GetHealthStatus()
    {
        var issues = new List<string>();

        if (ErrorRate > 1.0)
        {
            issues.Add($"High error rate: {ErrorRate:F2}/s");
        }

        if (MemoryUsageMB > 500)
        {
            issues.Add($"High memory usage: {MemoryUsageMB:F0}MB");
        }

        if (AverageProcessingTime > 100)
        {
            issues.Add($"Slow processing: {AverageProcessingTime:F1}ms");
        }

        if (issues.Count == 0)
        {
            return "✅ All systems healthy";
        }

        return $"⚠️ {issues.Count} issue(s): {string.Join(", ", issues)}";
    }
}