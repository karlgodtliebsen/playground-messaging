using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Maui.Prometheus.Viewer.PageModels;

/// <summary>
/// ObservableCollection that supports adding/removing ranges of items with a single notification
/// </summary>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    private bool suppressNotification = false;

    /// <summary>
    /// Adds multiple items to the collection and raises a single CollectionChanged event
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        suppressNotification = true;
        DynamicData.ListEx.AddRange<T>(this, items);
        suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Removes multiple items from the collection and raises a single CollectionChanged event
    /// </summary>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        suppressNotification = true;

        foreach (var item in items)
        {
            Remove(item);
        }

        suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Replaces all items in the collection with new items, raising a single CollectionChanged event
    /// </summary>
    public void ReplaceRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        suppressNotification = true;

        Clear();
        DynamicData.ListEx.AddRange<T>(this, items);
        suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!suppressNotification)
        {
            base.OnPropertyChanged(e);
        }
    }
}