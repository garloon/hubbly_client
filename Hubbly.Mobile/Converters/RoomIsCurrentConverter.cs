using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Hubbly.Mobile.Models;

namespace Hubbly.Mobile.Converters;

/// <summary>
/// Converts IsCurrent flag to background color for room highlighting
/// Returns PrimaryColor for current room, CardBackgroundColor for others
/// </summary>
public class RoomIsCurrentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isCurrent && isCurrent)
        {
            // Return a highlight color for current room
            return Color.FromArgb("#8b5cf6"); // Primary purple with some opacity or use resource
        }
        // Return default card background (will be set by DynamicResource)
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}