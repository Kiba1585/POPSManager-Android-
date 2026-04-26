using System;
using System.Globalization;
using System.Windows.Data;

namespace POPSManager.Core.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.Equals(parameter) == true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? parameter : Binding.DoNothing;
}