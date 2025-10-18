using AsyncAwaitBestPractices;

using Messaging.EventHub.Library;

namespace Maui.Prometheus.Viewer.Pages;

public partial class EventHubDetailPage : ContentPage
{
    private readonly IEventHub eventHub = null!;
    private readonly CancellationTokenSource cancellationTokenSource;

    public EventHubDetailPage()
    {
        InitializeComponent();
    }
    public EventHubDetailPage(IEventHub eventHub, CancellationTokenSource cancellationTokenSource, EventHubDetailViewModel viewModel) : this()
    {
        this.eventHub = eventHub;
        this.cancellationTokenSource = cancellationTokenSource;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Initialize the view model when page appears
        eventHub.Publish("eventhub-initialize", cancellationTokenSource.Token).SafeFireAndForget();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Stop auto-refresh when navigating away
        eventHub.Publish("eventhub-stop", cancellationTokenSource.Token).SafeFireAndForget();
    }

    // Optional: Handle page lifecycle cleanup
    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        eventHub.Publish("eventhub-cleanup", cancellationTokenSource.Token).SafeFireAndForget();
    }
}