using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SportHubBase.Converters
{
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

    /// <summary>
    /// Преобразует строковый идентификатор изображения серой ячейки (SelfImageData)
    /// во встроенную векторную иконку мяча или в пользовательский BitmapImage.
    /// </summary>
    public class SelfCellImageConverter : IValueConverter
    {
        private static readonly Dictionary<string, ImageSource> _cache = new Dictionary<string, ImageSource>(StringComparer.Ordinal);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string data = value as string;
            if (string.IsNullOrWhiteSpace(data))
                return null;

            ImageSource cached;
            if (_cache.TryGetValue(data, out cached))
                return cached;

            try
            {
                ImageSource created = null;

                if (string.Equals(data, "preset:volleyball", StringComparison.OrdinalIgnoreCase))
                {
                    created = CreateVolleyballImage();
                }
                else if (string.Equals(data, "preset:football", StringComparison.OrdinalIgnoreCase))
                {
                    created = CreateFootballImage();
                }
                else if (string.Equals(data, "preset:basketball", StringComparison.OrdinalIgnoreCase))
                {
                    created = CreateBasketballImage();
                }
                else if (data.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
                {
                    string base64 = data.Substring("base64:".Length);
                    byte[] bytes = System.Convert.FromBase64String(base64);
                    using (var ms = new MemoryStream(bytes))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                        bmp.Freeze();
                        created = bmp;
                    }
                }
                else if (File.Exists(data))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(data, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    created = bmp;
                }

                if (created != null)
                {
                    if (_cache.Count > 32)
                        _cache.Clear();
                    _cache[data] = created;
                }

                return created;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static ImageSource CreateVolleyballImage()
        {
            var group = new DrawingGroup();

            var circle = new EllipseGeometry(new Point(24, 24), 20, 20);
            var fillBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            var strokePen = new Pen(new SolidColorBrush(Color.FromRgb(30, 64, 175)), 2.2);
            group.Children.Add(new GeometryDrawing(fillBrush, strokePen, circle));

            var seamPen = new Pen(new SolidColorBrush(Color.FromRgb(37, 99, 235)), 1.8);
            var yellowAccent = new GeometryDrawing(
                new SolidColorBrush(Color.FromRgb(254, 240, 138)),
                seamPen,
                Geometry.Parse("M 8,14 A 22,22 0 0,1 40,14 A 18,18 0 0,0 8,14 Z"));
            group.Children.Add(yellowAccent);

            var curves = Geometry.Parse("M 6,24 Q 24,14 42,24 M 12,8 Q 24,24 12,40 M 36,8 Q 24,24 36,40");
            group.Children.Add(new GeometryDrawing(null, seamPen, curves));

            group.ClipGeometry = circle;
            group.Children.Add(new GeometryDrawing(null, strokePen, circle));

            var img = new DrawingImage(group);
            img.Freeze();
            return img;
        }

        private static ImageSource CreateFootballImage()
        {
            var group = new DrawingGroup();

            var circle = new EllipseGeometry(new Point(24, 24), 20, 20);
            var fillBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            var strokePen = new Pen(new SolidColorBrush(Color.FromRgb(30, 41, 59)), 2.2);
            group.Children.Add(new GeometryDrawing(fillBrush, strokePen, circle));

            var darkBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            var pentagon = Geometry.Parse("M 24,15 L 32,21 L 29,30 L 19,30 L 16,21 Z");
            group.Children.Add(new GeometryDrawing(darkBrush, null, pentagon));

            var seamsPen = new Pen(darkBrush, 1.8);
            var seams = Geometry.Parse("M 24,15 L 24,4 M 32,21 L 43,17 M 29,30 L 36,40 M 19,30 L 12,40 M 16,21 L 5,17");
            group.Children.Add(new GeometryDrawing(null, seamsPen, seams));

            var img = new DrawingImage(group);
            img.Freeze();
            return img;
        }

        private static ImageSource CreateBasketballImage()
        {
            var group = new DrawingGroup();

            var circle = new EllipseGeometry(new Point(24, 24), 20, 20);
            var fillBrush = new SolidColorBrush(Color.FromRgb(249, 115, 22));
            var strokePen = new Pen(new SolidColorBrush(Color.FromRgb(124, 45, 18)), 2.2);
            group.Children.Add(new GeometryDrawing(fillBrush, strokePen, circle));

            var seamPen = new Pen(new SolidColorBrush(Color.FromRgb(67, 20, 7)), 1.8);
            var seams = Geometry.Parse("M 4,24 L 44,24 M 24,4 L 24,44 M 10,10 Q 22,24 10,38 M 38,10 Q 26,24 38,38");
            group.Children.Add(new GeometryDrawing(null, seamPen, seams));

            var img = new DrawingImage(group);
            img.Freeze();
            return img;
        }
    }
}