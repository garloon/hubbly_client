using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class IsNotSystemMessageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = value is string senderId && senderId != "system";
        // Uncomment for debugging:
        // Console.WriteLine($"IsNotSystemMessageConverter: Input='{value}', Result={result}");
        return result;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}