using Maui.Prometheus.Viewer.Pages.Controls;

namespace Maui.Prometheus.Viewer;

public partial class AppShellViewModel : ViewModelBase
{
    public AppShellViewModel() : base("AppShell")
    {
    }
}

public partial class AppShell : Shell
{
    //[ObservableProperty]
    //private int themeIndex;
    public int ThemeIndex { get; set; }

    public AppShell()
    {
        InitializeComponent();
        var currentTheme = Application.Current!.RequestedTheme;
        this.ThemeToggle.SelectedIndex = ThemeIndex = currentTheme == AppTheme.Light ? 0 : 1;
        // Register routes for navigation
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        // Main navigation routes (already defined in XAML, but can be registered here too)
        Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
        Routing.RegisterRoute(nameof(EventHubDetailPage), typeof(EventHubDetailPage));
        Routing.RegisterRoute(nameof(SystemMetricsPage), typeof(SystemMetricsPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

        // Optional: Add additional routes for modal navigation or deep linking
        // Routing.RegisterRoute("details/eventhub", typeof(EventHubDetailPage));
    }



    private void OnThemeSelectionChanged(object? sender, SelectedIndexChangedEventArgs e)
    {
        var themeIndex = e.SelectedIndex;
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = themeIndex switch
            {
                0 => AppTheme.Light,
                1 => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }

    //public static async Task DisplaySnackbarAsync(string message)
    //{
    //    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    //    var snackbarOptions = new SnackbarOptions
    //    {
    //        BackgroundColor = Color.FromArgb("#FF3300"),
    //        TextColor = Colors.White,
    //        ActionButtonTextColor = Colors.Yellow,
    //        CornerRadius = new CornerRadius(0),
    //        Font = Font.SystemFontOfSize(18),
    //        ActionButtonFont = Font.SystemFontOfSize(14)
    //    };

    //    var snackbar = Snackbar.Make(message, visualOptions: snackbarOptions);
    //    await snackbar.Show(cancellationTokenSource.Token);
    //}
    //public static async Task DisplayToastAsync(string message)
    //{
    //    // Toast is currently not working in MCT on Windows
    //    if (OperatingSystem.IsWindows())
    //        return;

    //    var toast = Toast.Make(message, textSize: 18);

    //    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    //    await toast.Show(cts.Token);
    //}

}