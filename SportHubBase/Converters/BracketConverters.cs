using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SportHubBase.Converters
{
    public class MatchWinnerBoldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isWinner = (bool)value;
            return isWinner ? FontWeights.Bold : FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MatchWinnerColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isWinner = (bool)value;
            return isWinner ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#137FEC")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ScoreNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "-";
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            if (string.IsNullOrWhiteSpace(s) || s == "-") return null;
            if (int.TryParse(s, out int res)) return res;
            return null;
        }
    }

    public class IsLiveColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isLive = (bool)value;
            return isLive ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")) : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
