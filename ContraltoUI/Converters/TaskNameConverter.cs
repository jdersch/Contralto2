using Avalonia.Data.Converters;
using Contralto.CPU;
using ContraltoUI.ViewModels;
using System;
using System.Globalization;


namespace ContraltoUI.Converters
{
    public class TaskNameConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return DebuggerViewModel.GetTextForTask((TaskType)value);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
