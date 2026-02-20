using System.Collections.Concurrent;
using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class AvatarColorConverter : IValueConverter
{
    private static readonly string[] _colors =
    {
        "#EF4444", "#F59E0B", "#10B981", "#3B82F6",
        "#8B5CF6", "#EC4899", "#14B8A6", "#F97316"
    };

    private static readonly ConcurrentDictionary<string, Color> _userColorCache = new();
    private static readonly Random _random = new Random();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Текущий пользователь - фиолетовый
        if (value is bool isCurrentUser && isCurrentUser)
            return Color.FromArgb("#4F46E5");

        // По никнейму - стабильный цвет
        if (value is string nickname && !string.IsNullOrEmpty(nickname))
        {
            return _userColorCache.GetOrAdd(nickname, key =>
            {
                var hash = key.GetHashCode();
                var index = Math.Abs(hash) % _colors.Length;
                return Color.FromArgb(_colors[index]);
            });
        }

        return Color.FromArgb(_colors[0]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}