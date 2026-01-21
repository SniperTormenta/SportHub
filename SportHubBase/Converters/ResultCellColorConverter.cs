using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SportHubBase.Converters
{
    public class ResultCellColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return new SolidColorBrush(Color.FromRgb(97, 117, 137)); // #617589 — серый для пустых
            string cellValue = value.ToString().Trim();
            if (cellValue == "SELF")
                return new SolidColorBrush(Color.FromRgb(97, 117, 137)); // Серый для диагонали (сам с собой)
            else if (cellValue == "1" || cellValue == "1.0")
                return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981 — зелёный для победы
            else if (cellValue == "0" || cellValue == "0.0")
                return new SolidColorBrush(Color.FromRgb(244, 63, 94)); // #F43F5E — красный для поражения
            else
                return new SolidColorBrush(Color.FromRgb(97, 117, 137)); // Серый для других (e.g. неопределённо)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Не требуется, так как односторонний биндинг
        }
    }
}