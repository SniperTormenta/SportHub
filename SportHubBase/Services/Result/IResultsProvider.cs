// Services/Results/IResultsProvider.cs
using SportHubBase.Models;
using SportHubBase.Services.Results.Data;
using System.Collections.ObjectModel;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Интерфейс провайдера результатов.
    /// Возвращает полиморфный объект ResultsData в зависимости от реализации.
    /// </summary>
    public interface IResultsProvider
    {
        /// <summary>
        /// Короткое имя провайдера (для отладки).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Основной метод: рассчитывает и возвращает объект данных результатов.
        /// </summary>
        ResultsData ComputeResults(Tournament tournament, ObservableCollection<Match> schedule);
    }
}