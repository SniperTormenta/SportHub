using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SportHubBase.Models;

namespace SportHubBase.Converters
{
    public class ResultCellColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var cell = value as CellResult;
            if (cell == null) return Brushes.Black;

            if (cell.IsSelf || !cell.IsPlayed)
                return Brushes.Gray;

            if (cell.IsWin)
                return new SolidColorBrush(Color.FromRgb(22, 163, 74));   // насыщенный зелёный

            if (cell.IsLoss)
                return new SolidColorBrush(Color.FromRgb(185, 28, 28));   // насыщенный красный

            if (cell.IsDraw)
                return new SolidColorBrush(Colors.DarkGoldenrod);

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}