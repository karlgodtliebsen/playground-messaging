using AsyncAwaitBestPractices;

using Messaging.EventHub.Library;

namespace Maui.Prometheus.Viewer.Pages;

public partial class SystemMetricsPage : ContentPage
{
    private readonly IEventHub eventHub = null!;
    private readonly CancellationTokenSource cancellationTokenSource;

    public SystemMetricsPage()
    {
        InitializeComponent();
    }

    public SystemMetricsPage(IEventHub eventHub, CancellationTokenSource cancellationTokenSource, SystemMetricsViewModel viewModel) : this()
    {
        this.eventHub = eventHub;
        this.cancellationTokenSource = cancellationTokenSource;
        BindingContext = viewModel;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Initialize the view model when page appears
        eventHub.Publish("system-metrics-initialize", cancellationTokenSource.Token).SafeFireAndForget();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Stop auto-refresh when navigating away
        eventHub.Publish("system-metrics-stop", cancellationTokenSource.Token).SafeFireAndForget();
    }

    // Optional: Handle page lifecycle cleanup
    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        eventHub.Publish("system-metrics-cleanup", cancellationTokenSource.Token).SafeFireAndForget();
    }
}