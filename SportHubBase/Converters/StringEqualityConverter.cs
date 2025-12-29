// Converters/StringEqualityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SportHubBase.Converters
{
    public class StringEqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value?.ToString()?.Trim() ?? string.Empty;
            string param = parameter?.ToString()?.Trim() ?? string.Empty;
            return string.Equals(val, param, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}