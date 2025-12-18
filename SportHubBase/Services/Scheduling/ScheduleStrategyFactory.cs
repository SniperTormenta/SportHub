// Services/Scheduling/ScheduleStrategyFactory.cs
using System;

namespace SportHubBase.Services.Scheduling
{
    /// <summary>
    /// Фабрика для выбора стратегии генерации расписания на основе формата турнира.
    /// </summary>
    public static class ScheduleStrategyFactory
    {
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


