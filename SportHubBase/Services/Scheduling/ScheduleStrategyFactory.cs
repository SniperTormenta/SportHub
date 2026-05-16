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

        public static readonly string RoundRobin = "Круговой";
        public static readonly string Olympic = "Олимпийский";
        public static readonly string Swiss = "Швейцарский";
        public static readonly string Staged = "Многоэтапный";

        public IScheduleStrategy GetStrategy(string tournamentType)
        {
            if (string.IsNullOrWhiteSpace(tournamentType))
                return null;

            string type = tournamentType.Trim();

            if (type.Equals("Круговой", StringComparison.OrdinalIgnoreCase))
                return new RoundRobinBergerScheduleStrategy();

            if (type.Equals("Олимпийский", StringComparison.OrdinalIgnoreCase) || 
                type.Equals("Плей-офф", StringComparison.OrdinalIgnoreCase))
                return new OlympicScheduleStrategy();

            if (type.Equals("Швейцарский", StringComparison.OrdinalIgnoreCase) || 
                type.Equals("Швейцарка", StringComparison.OrdinalIgnoreCase))
                return new SwissScheduleStrategy(_tiebreakerCalculator, _playedMatchesService);

            if (type.Equals("Многоэтапный", StringComparison.OrdinalIgnoreCase) || 
                type.Equals("Группы + плей-офф", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Поэтапный", StringComparison.OrdinalIgnoreCase))
                return new StagedScheduleStrategy();

            return null;
        }
    }
}