using System.IO;
using System.Windows.Media.Imaging;
using SportHubBase.Interfaces;

namespace SportHubBase.Services.Export
{
    /// <summary>
    /// Стратегия кодирования изображения в JPEG.
    /// </summary>
    public class JpgImageEncoderStrategy : IImageEncoderStrategy
    {
        public string Name => "JPEG";
        public string DefaultExtension => ".jpg";

        public void Encode(BitmapSource bitmap, Stream output)
        {
            var encoder = new JpegBitmapEncoder
            {
                QualityLevel = 90
            };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(output);
        }
    }
}

