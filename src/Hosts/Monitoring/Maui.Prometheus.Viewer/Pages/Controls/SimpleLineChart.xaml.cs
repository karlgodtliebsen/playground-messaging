using Microcharts;

using SkiaSharp;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Maui.Prometheus.Viewer.Pages.Controls;

public partial class SimpleLineChart : ContentView
{
    public static readonly BindableProperty DataPointsProperty =
        BindableProperty.Create(
            nameof(DataPoints),
            typeof(ObservableCollection<DataPoint>),
            typeof(SimpleLineChart),
            null,
            propertyChanged: OnDataPointsChanged);

    public ObservableCollection<DataPoint>? DataPoints
    {
        get => (ObservableCollection<DataPoint>?)GetValue(DataPointsProperty);
        set => SetValue(DataPointsProperty, value);
    }


    private ObservableCollection<DataPoint>? currentCollection;

    public SimpleLineChart()
    {
        InitializeComponent();
    }

    private static void OnDataPointsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SimpleLineChart control)
        {
            // Unsubscribe from old collection
            if (control.currentCollection != null)
            {
                control.currentCollection.CollectionChanged -= control.OnCollectionChanged;
            }

            // Subscribe to new collection
            if (newValue is ObservableCollection<DataPoint> newCollection)
            {
                control.currentCollection = newCollection;
                newCollection.CollectionChanged += control.OnCollectionChanged;
            }
            else
            {
                control.currentCollection = null;
            }

            control.UpdateChart();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateChart();
    }

    private void UpdateChart()
    {
        try
        {
            if (DataPoints == null || DataPoints.Count == 0)
            {
                chartView.Chart = null;
                chartView.InvalidateSurface();
                return;
            }

            chartView.Chart = null;
            chartView.InvalidateSurface();

            var entries = DataPoints.Select(dp => new ChartEntry((float)dp.Value)
            {
                Label = dp.Timestamp.ToString("HH:mm"),
                ValueLabel = dp.Value.ToString("F2"),
                Color = SKColor.Parse("#2196F3")
            }).ToList();

            var chart = new LineChart
            {
                Entries = entries,
                LineMode = LineMode.Straight,
                LineSize = 3,
                PointMode = PointMode.Circle,
                PointSize = 10,
                BackgroundColor = SKColors.Transparent,
                LabelTextSize = 30,
                ValueLabelTextSize = 0, // Hide value labels on points
                LabelOrientation = Orientation.Horizontal,
                IsAnimated = false
            };
            chartView.Chart = chart;
            chartView.InvalidateSurface();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating chart: {ex.Message}");
        }
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // Ensure chart updates when control is added to visual tree
        if (Handler != null)
        {
            UpdateChart();
        }
    }
}