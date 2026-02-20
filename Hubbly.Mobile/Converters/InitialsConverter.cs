using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class InitialsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string nickname && !string.IsNullOrEmpty(nickname))
        {
            var parts = nickname.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            return nickname.Length >= 2 ? nickname.Substring(0, 2).ToUpper() : nickname.ToUpper();
        }
        return "??";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
