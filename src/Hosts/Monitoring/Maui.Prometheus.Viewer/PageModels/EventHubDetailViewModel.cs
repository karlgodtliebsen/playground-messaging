using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Messaging.EventHub.Library;

using Microsoft.Extensions.Logging;


namespace Maui.Prometheus.Viewer.PageModels;

public partial class EventHubDetailViewModel : ViewModelBase
{
    private readonly IEventHub eventHub;
    private readonly IPrometheusService prometheusService;
    private readonly ILogger<EventHubDetailViewModel> logger;
    private System.Timers.Timer? refreshTimer;
    private int refreshIntervalSeconds = 5;
    private int lookbackPeriodMinutes = 15;
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string lastUpdated = "Never";
    [ObservableProperty]
    private string connectionStatus = "Disconnected";

    // Scalar Metrics
    [ObservableProperty]
    private double publishedRate;

    [ObservableProperty]
    private double processedRate;

    [ObservableProperty]
    private int activeSubscribers;

    [ObservableProperty]
    private int activeChannels;

    [ObservableProperty]
    private double averageProcessingTime;

    [ObservableProperty]
    private double errorRate;

    // Chart Data Collections
    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> publishedByEventData = new();

    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> processingDurationData = new();

    [ObservableProperty]
    private ObservableRangeCollection<DataPoint> errorRateData = new();

    [ObservableProperty]
    private ObservableRangeCollection<KeyValuePair<string, double>> eventDistribution = new();

    private readonly CancellationTokenSource cancellationTokenSource;

    public EventHubDetailViewModel(IEventHub eventHub, IPrometheusService prometheusService, CancellationTokenSource cancellationTokenSource, ILogger<EventHubDetailViewModel> logger) : base("EventHub Details")
    {
        this.cancellationTokenSource = cancellationTokenSource;
        this.eventHub = eventHub;
        this.prometheusService = prometheusService;
        this.logger = logger;
        Add(eventHub.Subscribe("eventhub-initialize", InitializeAsync));
        Add(eventHub.Subscribe("eventhub-stop", StopAutoRefresh));
        Add(eventHub.Subscribe("eventhub-cleanup", Cleanup));
        Add(eventHub.Subscribe("eventhub-data-all-reload", LoadAllDataAsync));
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await TestConnectionAsync();

        if (IsConnected)
        {
            await LoadAllDataAsync(cancellationToken);
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
        await Task.Run(LoadAllEventDataAsync, cancellationToken);
    }
    private (DateTime start, DateTime end) GetPeriod()
    {
        var end = DateTime.UtcNow;
        var start = end.AddMinutes(-lookbackPeriodMinutes);
        return (start, end);
    }
    private async Task LoadAllEventDataAsync()
    {
        if (IsBusy) return;

        try
        {
            var (start, end) = GetPeriod();

            logger.LogDebug("Refreshing EventHub metrics from {Start} to {End}", start, end);

            // Load scalar metrics in parallel
            var scalarTasks = new Task[]
            {
                Task.Run(async () => PublishedRate = await prometheusService.GetEventsPublishedRate()),
                Task.Run(async () => ProcessedRate = await prometheusService.GetEventsProcessedRate()),
                Task.Run(async () => ActiveSubscribers = (int)await prometheusService.GetActiveSubscribers()),
                Task.Run(async () => ActiveChannels = (int)await prometheusService.GetActiveChannels()),
                Task.Run(async () => AverageProcessingTime = await prometheusService.GetAverageProcessingTime()),
                Task.Run(async () => ErrorRate = await prometheusService.GetErrorRate())
            };

            await Task.WhenAll(scalarTasks);

            // Load time series data in parallel
            var timeSeriesTasks = new[]
            {
                LoadPublishedByEventDataAsync(start,end),
                LoadProcessingDurationDataAsync(start,end),
                LoadErrorRateDataAsync(start,end),
                LoadEventDistributionAsync()
            };
            await Task.WhenAll(timeSeriesTasks);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            });
            logger.LogInformation(
                "EventHub metrics refreshed - Published: {Published:F2}/s, Processed: {Processed:F2}/s, Subscribers: {Subscribers}, Channels: {Channels}",
                PublishedRate, ProcessedRate, ActiveSubscribers, ActiveChannels);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh EventHub metrics");
            // Don't throw - allow UI to continue functioning
        }
    }


    private async Task LoadPublishedByEventDataAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetEventsPublishedRateByEvent(start, end, "15s");
            await UpdateCollection(PublishedByEventData, data);

            logger.LogDebug("Loaded {Count} published event data points", data.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load published by event data");
            PublishedByEventData.Clear();
        }
    }

    private async Task LoadProcessingDurationDataAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetProcessingDuration(start, end, "15s");
            await UpdateCollection(ProcessingDurationData, data);
            logger.LogDebug("Loaded {Count} processing duration data points", data.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load processing duration data");
            ProcessingDurationData.Clear();
        }
    }

    private async Task LoadErrorRateDataAsync(DateTime start, DateTime end)
    {
        try
        {
            var data = await prometheusService.GetErrorRateByEvent(start, end, "15s");
            await UpdateCollection(ErrorRateData, data);

            logger.LogDebug("Loaded {Count} error rate data points", data.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load error rate data");
            ErrorRateData.Clear();
        }
    }

    private async Task LoadEventDistributionAsync()
    {
        try
        {
            var distribution = await prometheusService.GetEventDistribution();
            await UpdateCollection(EventDistribution, distribution.ToList());
            logger.LogDebug("Loaded event distribution with {Count} event types", distribution.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load event distribution");
            EventDistribution.Clear();
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

        logger.LogInformation("Auto-refresh started for EventHub metrics");
    }

    private Task StopAutoRefresh(CancellationToken cancellationToken)
    {
        if (refreshTimer != null)
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();
            refreshTimer = null;
            logger.LogInformation("Auto-refresh stopped for EventHub metrics");
        }
        return Task.CompletedTask;
    }

    // Cleanup when view model is disposed or page is navigated away from
    private async Task Cleanup(CancellationToken cancellationToken)
    {
        await StopAutoRefresh(cancellationToken);
    }
}