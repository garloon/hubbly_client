using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class GenderTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected)
        {
            return isSelected ?
                Color.FromArgb("#FFFFFF") :  // Selected - white text
                Color.FromArgb("#64748B");   // Not selected - gray text
        }
        return Color.FromArgb("#64748B");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
