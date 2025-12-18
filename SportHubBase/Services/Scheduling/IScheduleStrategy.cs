// Services/Scheduling/IScheduleStrategy.cs
using System.Collections.Generic;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling
{
    /// <summary>
    /// Стратегия генерации расписания турнира.
    /// Позволяет подменять алгоритм в зависимости от формата турнира.
    /// </summary>
    public interface IScheduleStrategy
    {
        /// <summary>
        /// Короткое имя / описание стратегии (например, "Круговой (Бергера)").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Флаг, реализован ли алгоритм полностью.
        /// Для заглушек возвращаем false.
        /// </summary>
        bool IsImplemented { get; }

        /// <summary>
        /// Генерация расписания на основе списка команд.
        /// </summary>
        IEnumerable<Match> GenerateSchedule(IList<Team> teams);
    }
}


