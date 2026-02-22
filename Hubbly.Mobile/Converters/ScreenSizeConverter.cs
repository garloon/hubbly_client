using System.Globalization;
using Microsoft.Maui.Controls;

namespace Hubbly.Mobile.Converters;

/// <summary>
/// Classifies screen size for responsive design
/// </summary>
public enum ScreenSize
{
    Small,    // phones portrait (< 360dp width)
    Medium,   // phones landscape / small tablets (360-600dp)
    Large,    // tablets portrait (600-840dp)
    XLarge    // tablets landscape / desktop (> 840dp)
}

/// <summary>
/// Converts width/height to ScreenSize enum
/// </summary>
public class WidthToScreenSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            // Convert from pixels to device-independent units (approximate)
            var density = DeviceDisplay.Current.MainDisplayInfo.Density;
            var dpWidth = width / density;

            return dpWidth switch
            {
                < 360 => ScreenSize.Small,
                < 600 => ScreenSize.Medium,
                < 840 => ScreenSize.Large,
                _ => ScreenSize.XLarge
            };
        }

        return ScreenSize.Medium;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts height to ScreenSize (for orientation-specific layouts)
/// </summary>
public class HeightToScreenSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double height)
        {
            var density = DeviceDisplay.Current.MainDisplayInfo.Density;
            var dpHeight = height / density;

            return dpHeight switch
            {
                < 480 => ScreenSize.Small,
                < 800 => ScreenSize.Medium,
                < 1200 => ScreenSize.Large,
                _ => ScreenSize.XLarge
            };
        }

        return ScreenSize.Medium;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns different values based on screen size
/// Parameter format: "small|medium|large|xlarge"
/// </summary>
public class ScreenSizeToValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ScreenSize screenSize && parameter is string param)
        {
            var values = param.Split('|');
            int index = screenSize switch
            {
                ScreenSize.Small => 0,
                ScreenSize.Medium => 1,
                ScreenSize.Large => 2,
                ScreenSize.XLarge => 3,
                _ => 1
            };

            return values.Length > index ? values[index] : values[0];
        }

        return parameter?.ToString()?.Split('|')?.FirstOrDefault() ?? value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
