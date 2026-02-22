using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace Hubbly.Mobile.Converters;

/// <summary>
/// Converts device orientation to boolean values
/// </summary>
public class OrientationToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts Orientation to boolean (true for portrait, false for landscape)
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DisplayOrientation orientation)
        {
            var isPortrait = orientation == DisplayOrientation.Portrait;
            return isPortrait;
        }

        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to orientation enum
/// </summary>
public class BoolToOrientationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPortrait)
        {
            return isPortrait ? DisplayOrientation.Portrait : DisplayOrientation.Landscape;
        }

        return DisplayOrientation.Portrait;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns different values based on orientation
/// Parameter format: "portraitValue|landscapeValue"
/// </summary>
public class OrientationToValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DisplayOrientation orientation && parameter is string param)
        {
            var values = param.Split('|');
            var isPortrait = orientation == DisplayOrientation.Portrait;

            return isPortrait ? values[0] : (values.Length > 1 ? values[1] : values[0]);
        }

        return parameter?.ToString()?.Split('|')?.FirstOrDefault() ?? value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Checks if current screen is in landscape orientation
/// </summary>
public class IsLandscapeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DisplayOrientation orientation)
        {
            return orientation == DisplayOrientation.Landscape;
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
