using System;
using System.IO;
using SportHubBase.Interfaces;

namespace SportHubBase.Services.Export
{
    /// <summary>
    /// Фабрика, возвращающая стратегии кодирования PNG/JPG.
    /// </summary>
    public class ImageEncoderStrategyFactory : IImageEncoderStrategyFactory
    {
        private readonly IImageEncoderStrategy _png;
        private readonly IImageEncoderStrategy _jpg;

        public ImageEncoderStrategyFactory()
        {
            _png = new PngImageEncoderStrategy();
            _jpg = new JpgImageEncoderStrategy();
        }

        public IImageEncoderStrategy GetPngStrategy() => _png;

        public IImageEncoderStrategy GetJpgStrategy() => _jpg;

        public IImageEncoderStrategy GetByExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return _png;

            extension = extension.StartsWith(".") ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();

            switch (extension)
            {
                case ".png":
                    return _png;
                case ".jpg":
                case ".jpeg":
                    return _jpg;
                default:
                    return _png;
            }
        }
    }
}

