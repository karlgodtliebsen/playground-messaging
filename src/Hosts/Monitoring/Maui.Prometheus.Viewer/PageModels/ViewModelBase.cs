using CommunityToolkit.Mvvm.ComponentModel;

namespace Maui.Prometheus.Viewer.PageModels;

public abstract partial class ViewModelBase(string theTitle) : ObservableObject, IDisposable, IAsyncDisposable
{
    private readonly IList<IDisposable> disposables = new List<IDisposable>();
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = theTitle;

    protected void Add(IDisposable disposable)
    {
        disposables.Add(disposable);
    }


    public void Dispose()
    {
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
        disposables.Clear();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}