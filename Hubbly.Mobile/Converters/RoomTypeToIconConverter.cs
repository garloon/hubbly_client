using Hubbly.Mobile.Models;
using System.Globalization;

namespace Hubbly.Mobile.Converters;

public class RoomTypeToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RoomType roomType)
        {
            return roomType switch
            {
                RoomType.System => "🌐",  // Globe for system rooms
                RoomType.Public => "💬",  // Chat bubble for public rooms
                RoomType.Private => "🔒", // Lock for private rooms
                _ => "❓"
            };
        }
        return "❓";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
