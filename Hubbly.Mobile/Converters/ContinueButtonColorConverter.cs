using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class ContinueButtonColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
        {
            return isEnabled ?
                Color.FromArgb("#4F46E5") :  // Enabled - фиолетовый
                Color.FromArgb("#94A3B8");   // Disabled - серый
        }
        return Color.FromArgb("#94A3B8");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
