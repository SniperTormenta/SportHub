// Services/Scheduling/ScheduleStrategyFactory.cs
using System;

namespace SportHubBase.Services.Scheduling
{
    /// Фабрика для выбора стратегии генерации расписания на основе формата турнира.
    /// Часть паттерна "Стратегия": возвращает IScheduleStrategy по Tournament.Type.
    /// Статическая для простоты; в IoC можно инжектировать как singleton.
    public static class ScheduleStrategyFactory
    {
        /// Возвращает стратегию по типу турнира (trim и case-insensitive).
        public static IScheduleStrategy GetStrategy(string tournamentType)
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