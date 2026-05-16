// Converters/SportTypeToHeaderConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace SportHubBase.Converters
{
    public class SportTypeToHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string sportType = value as string;
            string headerType = parameter as string; // "Won" or "Lost"

            if (string.Equals(sportType, "Футбол", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sportType, "Баскетбол", StringComparison.OrdinalIgnoreCase))
            {
                return headerType == "Won" ? "ЗМ" : "ПМ";
            }

            // По умолчанию (Волейбол)
            return headerType == "Won" ? "ВП" : "ПП";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SportTypeToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string sportType = value as string;
            string headerType = parameter as string;

            if (string.Equals(sportType, "Футбол", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sportType, "Баскетбол", StringComparison.OrdinalIgnoreCase))
            {
                return headerType == "Won" ? "Забитые мячи/очки" : "Пропущенные мячи/очки";
            }

            return headerType == "Won" ? "Выигранные сеты" : "Проигранные сеты";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
