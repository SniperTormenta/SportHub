namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Фабрика стратегий кодирования изображений.
    /// </summary>
    public interface IImageEncoderStrategyFactory
    {
        IImageEncoderStrategy GetPngStrategy();
        IImageEncoderStrategy GetJpgStrategy();
        IImageEncoderStrategy GetByExtension(string extension);
    }
}

