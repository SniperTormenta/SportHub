// Services/Scheduling/ScheduleStrategyFactory.cs
using SportHubBase.Interfaces;
using SportHubBase.Services.Scheduling.Swiss;
using System;

namespace SportHubBase.Services.Scheduling
{
    /// Фабрика для выбора стратегии генерации расписания на основе формата турнира.
    /// Часть паттерна "Стратегия": возвращает IScheduleStrategy по Tournament.Type.
    /// Для использования в IoC контейнере.
    public class ScheduleStrategyFactory : IScheduleStrategyFactory
    {
        private readonly ITiebreakerCalculator _tiebreakerCalculator;
        private readonly IPlayedMatchesService _playedMatchesService;

        public ScheduleStrategyFactory(ITiebreakerCalculator tiebreakerCalculator, IPlayedMatchesService playedMatchesService)
        {
            _tiebreakerCalculator = tiebreakerCalculator;
            _playedMatchesService = playedMatchesService;
        }

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
                    return new SwissScheduleStrategy(_tiebreakerCalculator, _playedMatchesService);

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