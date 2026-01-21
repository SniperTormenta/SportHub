// Converters/IsSelfCellConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace SportHubBase.Converters
{
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
}