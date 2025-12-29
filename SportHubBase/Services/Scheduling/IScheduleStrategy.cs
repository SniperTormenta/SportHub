// Services/Scheduling/IScheduleStrategy.cs
using System.Collections.Generic;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling
{
    /// Стратегия генерации расписания турнира.
    /// Позволяет подменять алгоритм в зависимости от формата турнира.
    /// Часть паттерна "Стратегия": реализуется конкретными классами (e.g. RoundRobinBergerScheduleStrategy), выбирается фабрикой.
    /// В MVVM: Вызывается из ViewModels (e.g. для генерации Tournament.Matches); IsImplemented для UI-логики.
    /// Улучшение: Добавить параметры (e.g. даты, рандомизация) в GenerateSchedule.
    public interface IScheduleStrategy
    {
        /// Короткое имя / описание стратегии (например, "Круговой (Бергера)").
        string Name { get; }

        /// Флаг, реализован ли алгоритм полностью.
        /// Для заглушек возвращаем false.
        bool IsImplemented { get; }

        /// Генерация расписания на основе списка команд.
        IEnumerable<Match> GenerateSchedule(IList<Team> teams);
    }
}