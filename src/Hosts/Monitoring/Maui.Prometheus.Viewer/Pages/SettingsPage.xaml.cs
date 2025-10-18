using Maui.Prometheus.Viewer.Pages.Controls;

using Messaging.EventHub.Library;

namespace Maui.Prometheus.Viewer.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel viewModel = null!;
    public SettingsPage()
    {
        InitializeComponent();
    }
    public SettingsPage(IEventHub eventHub, SettingsViewModel viewModel) : this()
    {
        BindingContext = this.viewModel = viewModel;
    }

    private void ThemeToggle_OnSelectionChanged(object? sender, SelectedIndexChangedEventArgs e)
    {
        //TODO: wrong style, fix later
        viewModel.ThemeIndex = e.SelectedIndex;
    }
}