using System.Globalization;

namespace Maui.Prometheus.Viewer.Utilities;

/// <summary>
/// Checks if a value is greater than the parameter
/// </summary>
public class GreaterThanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string paramStr)
        {
            if (double.TryParse(paramStr, out double threshold))
            {
                return doubleValue > threshold;
            }
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}