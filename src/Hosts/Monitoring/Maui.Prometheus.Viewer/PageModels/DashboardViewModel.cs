using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Maui.Prometheus.Viewer.PageModels;

public partial class DashboardViewModel : ViewModelBase
{
    private System.Threading.Timer? refreshTimer;


    [ObservableProperty]
    private ObservableCollection<DataPoint> eventRateData = new();

    [ObservableProperty]
    private double totalEventsPerSecond;

    [ObservableProperty]
    private int activeSubscribers;

    [ObservableProperty]
    private double averageProcessingTime;

    [ObservableProperty]
    private string lastUpdated = "Never";

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string connectionStatus = "Disconnected";

    private readonly IPrometheusService prometheus;

    /// <inheritdoc/>
    public DashboardViewModel(IPrometheusService prometheus) : base("Prometheus Dashboard")
    {
        this.prometheus = prometheus;
        StartAutoRefresh();
    }

    private void StartAutoRefresh()
    {
        refreshTimer = new System.Threading.Timer(
            async _ => await RefreshAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            // Test connection first
            IsConnected = await prometheus.TestConnection();
            ConnectionStatus = IsConnected ? "Connected" : "Disconnected";

            if (!IsConnected)
            {
                return;
            }

            // Load all metrics
            await Task.WhenAll(
                LoadEventRate(),
                LoadTotalRate(),
                LoadActiveSubscribers(),
                LoadAverageProcessingTime()
            );

            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Refresh error: {ex.Message}");
            ConnectionStatus = $"Error: {ex.Message}";
            IsConnected = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadEventRate()
    {
        var end = DateTime.UtcNow;
        var start = end.AddMinutes(-15);

        var query = "sum(rate(messaging_events_published_count_total[1m]))";
        var data = await prometheus.QueryRange(query, start, end);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            EventRateData.Clear();
            foreach (var point in data)
            {
                EventRateData.Add(point);
                Debug.WriteLine($"Added point: {point.Timestamp:HH:mm:ss} = {point.Value:F2}");
            }
            Debug.WriteLine($"EventRateData now has {EventRateData.Count} items");
        });
    }

    private async Task LoadTotalRate()
    {
        var query = "sum(rate(messaging_events_published_count_total[1m]))";
        TotalEventsPerSecond = await prometheus.QueryInstant(query);
    }

    private async Task LoadActiveSubscribers()
    {
        var query = "messaging_active_subscribers_count";
        ActiveSubscribers = (int)await prometheus.QueryInstant(query);
    }

    private async Task LoadAverageProcessingTime()
    {
        var query = @"
            rate(messaging_event_processing_duration_milliseconds_sum[5m]) 
            / 
            rate(messaging_event_processing_duration_milliseconds_count[5m])";
        AverageProcessingTime = await prometheus.QueryInstant(query);
    }

    public void Dispose()
    {
        refreshTimer?.Dispose();
    }


}