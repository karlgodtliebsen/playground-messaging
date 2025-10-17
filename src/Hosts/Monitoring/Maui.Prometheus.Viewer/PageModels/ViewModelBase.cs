using CommunityToolkit.Mvvm.ComponentModel;

namespace Maui.Prometheus.Viewer.PageModels;

public abstract partial class ViewModelBase(string theTitle) : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = theTitle;
}