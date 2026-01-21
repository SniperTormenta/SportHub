// Converters/ProgressConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace SportHubBase.Converters
{
    public class ProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return 0.0;
            if (int.TryParse(value.ToString(), out int current) &&
            int.TryParse(parameter.ToString(), out int max))
            {
                if (max == 0) return 0.0; // Избежать деления на 0
                                          // Возвращаем процент от 200 (примерная ширина контейнера)
                double percentage = (double)current / max;
                double width = percentage * 200;
                return Math.Min(Math.Max(width, 0), 200); // Ограничить 0-200
            }
            return 0.0; // По умолчанию 0
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Не требуется
        }
    }
}