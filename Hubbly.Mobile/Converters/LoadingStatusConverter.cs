using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class LoadingStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isLoading)
        {
            return isLoading ? "Loading..." : "Ready";
        }
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
