// Interfaces/IScoringStrategy.cs
using SportHubBase.Models;
using System.Collections.Generic;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Стратегия подсчёта очков для волейбола.
    /// Позволяет менять систему (Итальянская, FIVB, Пользовательская) без изменения калькулятора.
    /// </summary>
    public interface IScoringStrategy
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        
        /// <summary>
        /// Рассчитывает очки за один матч.
        /// </summary>
        int CalculatePoints(int wonSets, int lostSets);

        /// <summary>
        /// Сравнивает две строки результатов для определения места.
        /// Возвращает отрицательное если a лучше b, положительное если b лучше a.
        /// </summary>
        int Compare(ResultRow a, ResultRow b);
    }
}
