using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Hubbly.Mobile.Converters;

/// <summary>
/// Converts a boolean to text based on parameters
/// Parameter format: "TrueText|FalseText"
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}