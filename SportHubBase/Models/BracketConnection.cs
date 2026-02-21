// Models/BracketConnection.cs
namespace SportHubBase.Models
{
    /// <summary>
    /// Вспомогательный класс для отображения соединительной линии между матчами в сетке.
    /// </summary>
    public class BracketConnection
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
    }
}
