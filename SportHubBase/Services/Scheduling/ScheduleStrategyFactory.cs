// Services/Scheduling/ScheduleStrategyFactory.cs
using SportHubBase.Interfaces;
using System;

namespace SportHubBase.Services.Scheduling
{
    /// Фабрика для выбора стратегии генерации расписания на основе формата турнира.
    /// Часть паттерна "Стратегия": возвращает IScheduleStrategy по Tournament.Type.
    /// Для использования в IoC контейнере.
    public class ScheduleStrategyFactory : IScheduleStrategyFactory
    {
        /// Возвращает стратегию по типу турнира (trim и case-insensitive).
        public IScheduleStrategy GetStrategy(string tournamentType)
        {
            if (string.IsNullOrWhiteSpace(tournamentType))
                return null;

            switch (tournamentType.Trim())
            {
                case "Круговой":
                    return new RoundRobinBergerScheduleStrategy();

                case "Швейцарский":
                    return new SwissScheduleStrategy();

                case "Олимпийский":
                    return new OlympicScheduleStrategy();

                case "Поэтапный":
                    return new StagedScheduleStrategy();

                default:
                    return null;
            }
        }
    }
}