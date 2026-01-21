// Converters/ResultCellBackgroundConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SportHubBase.Converters
{
    public class ResultCellBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return System.Windows.Media.Brushes.Transparent; // Прозрачный для пустых
            string cellValue = value.ToString().Trim();
            if (cellValue == "SELF")
                return new SolidColorBrush(Color.FromRgb(243, 244, 246)); // #F3F4F6 — светло-серый для диагонали
            else if (cellValue == "1" || cellValue == "1.0")
                return new SolidColorBrush(Color.FromArgb(51, 16, 185, 129)); // #10B98133 — полупрозрачный зелёный
            else if (cellValue == "0" || cellValue == "0.0")
                return new SolidColorBrush(Color.FromArgb(51, 244, 63, 94)); // #F43F5E33 — полупрозрачный красный
            else
                return System.Windows.Media.Brushes.Transparent; // Прозрачный для других
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Не требуется
        }
    }
}