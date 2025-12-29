// Services/Statistics/IStatisticsCalculator.cs
using SportHubBase.Models;
using SportHubBase.ViewModels;
using System.Collections.ObjectModel;

namespace SportHubBase.Services.Statistics
{
    /// Интерфейс калькулятора общей статистики турнира.
    public interface IStatisticsCalculator
    {
        string Name { get; }
        /// Вычисляет статистику и возвращает готовый объект.
        TournamentStatistics Calculate(Tournament tournament, ObservableCollection<Match> schedule);
    }
}