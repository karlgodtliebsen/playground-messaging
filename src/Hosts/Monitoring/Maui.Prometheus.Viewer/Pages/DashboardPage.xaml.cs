namespace Maui.Prometheus.Viewer.Pages;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }
    public DashboardPage(DashboardViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
}