using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class GenderSelectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected)
        {
            return isSelected ?
                Color.FromArgb("#4F46E5") :  // Selected - фиолетовый
                Color.FromArgb("#FFFFFF");   // Not selected - белый
        }
        return Color.FromArgb("#FFFFFF");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
