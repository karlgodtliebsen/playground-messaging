using System.Collections.ObjectModel;

namespace Maui.Prometheus.Viewer.Pages.Controls;

public class SimplePieChart : GraphicsView
{
    private readonly PieChartDrawable drawable = new();

    public static readonly BindableProperty DataPointsProperty =
        BindableProperty.Create(
            nameof(DataPoints),
            typeof(ObservableCollection<KeyValuePair<string, double>>),
            typeof(SimplePieChart),
            null,
            propertyChanged: OnDataPointsChanged);

    public ObservableCollection<KeyValuePair<string, double>>? DataPoints
    {
        get => (ObservableCollection<KeyValuePair<string, double>>?)GetValue(DataPointsProperty);
        set => SetValue(DataPointsProperty, value);
    }

    public SimplePieChart()
    {
        Drawable = drawable;
        HeightRequest = 300;
    }

    private static void OnDataPointsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SimplePieChart chart)
        {
            if (oldValue is ObservableCollection<KeyValuePair<string, double>> oldCollection)
            {
                oldCollection.CollectionChanged -= chart.OnCollectionChanged;
            }

            if (newValue is ObservableCollection<KeyValuePair<string, double>> newCollection)
            {
                newCollection.CollectionChanged += chart.OnCollectionChanged;
                chart.drawable.DataPoints = newCollection.ToList();
                chart.Invalidate();
            }
        }
    }

    private void OnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (DataPoints != null)
        {
            drawable.DataPoints = DataPoints.ToList();
            Invalidate();
        }
    }

    private class PieChartDrawable : IDrawable
    {
        public List<KeyValuePair<string, double>> DataPoints { get; set; } = new();

        private readonly Color[] colors = new[]
        {
            Color.FromArgb("#2196F3"), // Blue
            Color.FromArgb("#4CAF50"), // Green
            Color.FromArgb("#FF9800"), // Orange
            Color.FromArgb("#9C27B0"), // Purple
            Color.FromArgb("#F44336"), // Red
            Color.FromArgb("#00BCD4"), // Cyan
            Color.FromArgb("#FF5722"), // Deep Orange
            Color.FromArgb("#3F51B5"), // Indigo
            Color.FromArgb("#8BC34A"), // Light Green
            Color.FromArgb("#FFC107"), // Amber
        };

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (DataPoints == null || DataPoints.Count == 0)
            {
                DrawNoData(canvas, dirtyRect);
                return;
            }

            var total = DataPoints.Sum(dp => dp.Value);
            if (total <= 0)
            {
                DrawNoData(canvas, dirtyRect);
                return;
            }

            var centerX = dirtyRect.Width / 2;
            var centerY = dirtyRect.Height / 2;
            var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.35f;
            var legendStartY = 20f;
            var legendItemHeight = 25f;

            float startAngle = -90; // Start from top

            for (int i = 0; i < DataPoints.Count; i++)
            {
                var dataPoint = DataPoints[i];
                var percentage = (float)(dataPoint.Value / total);
                var sweepAngle = percentage * 360;

                // Draw pie slice
                var color = colors[i % colors.Length];
                canvas.FillColor = color;

                var path = new PathF();
                path.MoveTo(centerX, centerY);

                // Arc
                var endAngle = startAngle + sweepAngle;
                var steps = Math.Max(2, (int)(sweepAngle / 5));

                for (int step = 0; step <= steps; step++)
                {
                    var angle = startAngle + (sweepAngle * step / steps);
                    var radians = angle * Math.PI / 180;
                    var x = centerX + radius * (float)Math.Cos(radians);
                    var y = centerY + radius * (float)Math.Sin(radians);
                    path.LineTo(x, y);
                }

                path.LineTo(centerX, centerY);
                canvas.FillPath(path);

                // Draw legend
                var legendX = dirtyRect.Width * 0.7f;
                var legendY = legendStartY + (i * legendItemHeight);

                // Legend color box
                canvas.FillColor = color;
                canvas.FillRectangle(legendX, legendY, 15, 15);

                // Legend text
                canvas.FontColor = Colors.White;
                canvas.FontSize = 12;
                var legendText = $"{dataPoint.Key}: {percentage:P1}";
                canvas.DrawString(legendText, legendX + 20, legendY + 12, HorizontalAlignment.Left);

                startAngle += sweepAngle;
            }
        }

        private void DrawNoData(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FontColor = Colors.Gray;
            canvas.FontSize = 14;
            canvas.DrawString("No data available",
                dirtyRect.Width / 2,
                dirtyRect.Height / 2,
                HorizontalAlignment.Center);
        }
    }
}