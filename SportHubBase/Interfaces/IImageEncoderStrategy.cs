using System.IO;
using System.Windows.Media.Imaging;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Стратегия кодирования изображения таблицы в конкретный формат (PNG, JPG и т.д.).
    /// </summary>
    public interface IImageEncoderStrategy
    {
        string Name { get; }
        string DefaultExtension { get; }

        /// <summary>
        /// Кодирует переданный BitmapSource в поток.
        /// </summary>
        /// <param name="bitmap">Готовое растровое изображение.</param>
        /// <param name="output">Поток для записи.</param>
        void Encode(BitmapSource bitmap, Stream output);
    }
}

