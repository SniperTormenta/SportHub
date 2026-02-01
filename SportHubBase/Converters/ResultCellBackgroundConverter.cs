using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SportHubBase.Models;

namespace SportHubBase.Converters
{
    public class ResultCellBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var cell = value as CellResult;
            if (cell == null) return Brushes.White;

            if (cell.IsSelf)
                return new SolidColorBrush(Color.FromRgb(230, 230, 230));      // серый self

            if (!cell.IsPlayed)
                return new SolidColorBrush(Color.FromRgb(249, 250, 251));      // очень светлый

            if (cell.IsWin)
                return new SolidColorBrush(Color.FromRgb(220, 252, 231));      // светло-зелёный

            if (cell.IsLoss)
                return new SolidColorBrush(Color.FromRgb(254, 226, 226));      // светло-красный

            if (cell.IsDraw)
                return new SolidColorBrush(Color.FromRgb(254, 249, 195));      // светло-жёлтый

            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}