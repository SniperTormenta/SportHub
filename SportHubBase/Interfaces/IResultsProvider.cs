using System.Collections.Generic;
using SportHubBase.Models;
using SportHubBase.Models.Results;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Интерфейс для формирования данных результатов турнира.
    /// В отличие от старого калькулятора, провайдер возвращает объект ResultsData,
    /// не мутируя внешние коллекции во ViewModel.
    /// </summary>
    public interface IResultsProvider
    {
        /// <summary>
        /// Вычисляет результаты турнира на основе текущих матчей.
        /// </summary>
        ResultsData ComputeResults(Tournament tournament, IEnumerable<Match> matches);
    }
}
