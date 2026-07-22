using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ContraltoUI.Converters
{
    public class OctalConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int padCount = parameter != null ? System.Convert.ToInt32(parameter) : 6;
            return System.Convert.ToString((ushort)value, 8).PadLeft(padCount, '0');
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
