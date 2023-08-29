using System.Globalization;
using System;
using System.Windows.Data;
using System.Diagnostics;

namespace NightGlow.Views.Converters;

public class DecToPctConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
            return (int)Math.Round(doubleValue * 100);
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value / 100;
    }
}
