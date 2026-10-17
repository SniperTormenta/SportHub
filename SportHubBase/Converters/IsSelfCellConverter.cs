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

    /// <summary>
    /// Конвертер заднего фона карточки матча/турнира.
    /// Если пользовательский фон не задан, возвращает фирменный фон ВОЛЕЙБОЛ АЛВ.
    /// </summary>
    public class CardBackgroundImageConverter : IValueConverter
    {
        private static ImageSource _defaultBackground;
        private static readonly SelfCellImageConverter _innerConverter = new SelfCellImageConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string data = value as string;
            if (!string.IsNullOrWhiteSpace(data) && !string.Equals(data, "default", StringComparison.OrdinalIgnoreCase))
            {
                var customImg = _innerConverter.Convert(data, targetType, parameter, culture) as ImageSource;
                if (customImg != null)
                {
                    return customImg;
                }
            }

            return GetDefaultBackground();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static ImageSource GetDefaultBackground()
        {
            if (_defaultBackground != null)
                return _defaultBackground;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri("pack://application:,,,/Resources/alv_card_bg.jpg", UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                _defaultBackground = bmp;
                return _defaultBackground;
            }
            catch
            {
                try
                {
                    string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "alv_card_bg.jpg");
                    if (File.Exists(localPath))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(localPath, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        _defaultBackground = bmp;
                        return _defaultBackground;
                    }
                }
                catch
                {
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Вспомогательный класс для нормализации и кодирования изображений (логотипов и фонов) в строку base64.
    /// </summary>
    public static class ImageStorageHelper
    {
        public static string EncodeImageFileToBase64(string filePath, int maxWidth, int maxHeight, bool encodeAsJpeg)
        {
            byte[] rawBytes = File.ReadAllBytes(filePath);
            using (var ms = new MemoryStream(rawBytes))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();

                int width = bmp.PixelWidth;
                int height = bmp.PixelHeight;

                double scale = 1.0;
                if (width > maxWidth || height > maxHeight)
                {
                    scale = Math.Min((double)maxWidth / width, (double)maxHeight / height);
                }

                BitmapSource sourceToEncode = bmp;
                if (scale < 1.0)
                {
                    var transformed = new TransformedBitmap(bmp, new ScaleTransform(scale, scale));
                    transformed.Freeze();
                    sourceToEncode = transformed;
                }

                BitmapEncoder encoder;
                if (encodeAsJpeg)
                {
                    encoder = new JpegBitmapEncoder { QualityLevel = 90 };
                }
                else
                {
                    encoder = new PngBitmapEncoder();
                }

                encoder.Frames.Add(BitmapFrame.Create(sourceToEncode));
                using (var outMs = new MemoryStream())
                {
                    encoder.Save(outMs);
                    return "base64:" + System.Convert.ToBase64String(outMs.ToArray());
                }
            }
        }
    }
}