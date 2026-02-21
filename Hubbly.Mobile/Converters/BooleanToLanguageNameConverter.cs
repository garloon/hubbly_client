using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class BooleanToLanguageNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRussian)
        {
            return isRussian ? "Русский" : "English";
        }
        return "Русский";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
