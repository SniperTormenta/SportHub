using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
namespace SportHubBase.Converters
{
    /// 
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
    /// 
    public class ResultCellBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return Brushes.Transparent; // Прозрачный для пустых
            string cellValue = value.ToString().Trim();
            if (cellValue == "SELF")
                return new SolidColorBrush(Color.FromRgb(243, 244, 246)); // #F3F4F6 — светло-серый для диагонали
            else if (cellValue == "1" || cellValue == "1.0")
                return new SolidColorBrush(Color.FromArgb(51, 16, 185, 129)); // #10B98133 — полупрозрачный зелёный
            else if (cellValue == "0" || cellValue == "0.0")
                return new SolidColorBrush(Color.FromArgb(51, 244, 63, 94)); // #F43F5E33 — полупрозрачный красный
            else
                return Brushes.Transparent; // Прозрачный для других
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Не требуется
        }
    }
    /// 
    public class IsSelfCellConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is int row && values[1] is int col)
            {
                return row == col; // True если диагональ — для "SELF"
            }
            return false; // По умолчанию не self
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Не требуется
        }
    }
    /// 
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