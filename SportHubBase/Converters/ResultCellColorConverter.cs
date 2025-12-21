// Converters/ResultCellColorConverter.cs
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
                return new SolidColorBrush(Color.FromRgb(97, 117, 137)); // #617589

            string cellValue = value.ToString().Trim();

            if (cellValue == "SELF")
                return new SolidColorBrush(Color.FromRgb(97, 117, 137));
            else if (cellValue == "1" || cellValue == "1.0")
                return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981
            else if (cellValue == "0" || cellValue == "0.0")
                return new SolidColorBrush(Color.FromRgb(244, 63, 94)); // #F43F5E
            else
                return new SolidColorBrush(Color.FromRgb(97, 117, 137));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ResultCellBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return Brushes.Transparent;

            string cellValue = value.ToString().Trim();

            if (cellValue == "SELF")
                return new SolidColorBrush(Color.FromRgb(243, 244, 246)); // #F3F4F6
            else if (cellValue == "1" || cellValue == "1.0")
                return new SolidColorBrush(Color.FromArgb(51, 16, 185, 129)); // #10B98133
            else if (cellValue == "0" || cellValue == "0.0")
                return new SolidColorBrush(Color.FromArgb(51, 244, 63, 94)); // #F43F5E33
            else
                return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsSelfCellConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is int row && values[1] is int col)
            {
                return row == col;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}