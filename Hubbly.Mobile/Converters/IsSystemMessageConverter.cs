using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class IsSystemMessageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var result = value is string senderId && senderId == "system";
        // Раскомментируйте для отладки:
        // Console.WriteLine($"IsSystemMessageConverter: Input='{value}', Result={result}");
        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}