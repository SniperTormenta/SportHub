// Converters/ResultCellColorConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SportHubBase.Converters
{
    /// <summary>
    /// Конвертер для определения цвета текста ячейки результата.
    /// "1" - зеленый (победа), "0" - красный (поражение), "½" или пусто - серый (ничья/нет результата)
    /// </summary>
    public class ResultCellColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return new SolidColorBrush(Color.FromRgb(97, 117, 137)); // #617589 - серый для пустых/ничьих

            string cellValue = value.ToString().Trim();

            if (cellValue == "SELF")
                return new SolidColorBrush(Color.FromRgb(97, 117, 137)); // #617589 - серый для Self ячеек
            else if (cellValue == "1" || cellValue == "1.0")
                return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981 - зеленый для победы
            else if (cellValue == "0" || cellValue == "0.0")
                return new SolidColorBrush(Color.FromRgb(244, 63, 94)); // #F43F5E - красный для поражения
            else
                return new SolidColorBrush(Color.FromRgb(97, 117, 137)); // #617589 - серый для ничьих и других значений
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер для определения фона ячейки результата.
    /// Победы - светло-зеленый фон, поражения - светло-красный фон.
    /// </summary>
    public class ResultCellBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return Brushes.Transparent;

            string cellValue = value.ToString().Trim();

            if (cellValue == "SELF")
                return new SolidColorBrush(Color.FromRgb(243, 244, 246)); // #F3F4F6 - серый фон для Self ячеек
            else if (cellValue == "1" || cellValue == "1.0")
                return new SolidColorBrush(Color.FromArgb(51, 16, 185, 129)); // #10B98133 - светло-зеленый фон
            else if (cellValue == "0" || cellValue == "0.0")
                return new SolidColorBrush(Color.FromArgb(51, 244, 63, 94)); // #F43F5E33 - светло-красный фон
            else
                return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер для определения, является ли ячейка "Self" (команда играет сама с собой).
    /// Используется для отображения диагональных линий.
    /// </summary>
    public class IsSelfCellConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            if (values[0] is int rowIndex && values[1] is int colIndex)
            {
                return rowIndex == colIndex;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

