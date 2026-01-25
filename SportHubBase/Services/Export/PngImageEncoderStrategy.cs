using System.IO;
using System.Windows.Media.Imaging;
using SportHubBase.Interfaces;

namespace SportHubBase.Services.Export
{
    /// <summary>
    /// Стратегия кодирования изображения в PNG.
    /// </summary>
    public class PngImageEncoderStrategy : IImageEncoderStrategy
    {
        public string Name => "PNG";
        public string DefaultExtension => ".png";

        public void Encode(BitmapSource bitmap, Stream output)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(output);
        }
    }
}

