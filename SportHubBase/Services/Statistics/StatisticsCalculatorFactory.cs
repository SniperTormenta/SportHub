// Services/Statistics/StatisticsCalculatorFactory.cs
using SportHubBase.Models;
using SportHubBase.ViewModels;
using System.Collections.ObjectModel;

namespace SportHubBase.Services.Statistics
{
    /// Фабрика калькуляторов статистики по виду спорта.
    public static class StatisticsCalculatorFactory
    {
        public static IStatisticsCalculator GetCalculator(string sportType)
        {
            if (string.IsNullOrWhiteSpace(sportType))
                return new DefaultStatisticsCalculator();

            switch (sportType.Trim())
            {
                case "Волейбол":
                    return new VolleyballStatisticsCalculator();
                case "Футбол":
                    return new FootballStatisticsCalculator();
                case "Баскетбол":
                    return new BasketballStatisticsCalculator();
                default:
                    return new DefaultStatisticsCalculator();
            }
        }
    }

    // Заглушки
    internal class DefaultStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "По умолчанию";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s) =>
            new TournamentStatistics { SportType = t?.SportType ?? "" };
    }

    internal class VolleyballStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "Волейбол";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s) =>
            new TournamentStatistics { SportType = "Волейбол" };
    }

    internal class FootballStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "Футбол";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s) =>
            new TournamentStatistics { SportType = "Футбол" };
    }

    internal class BasketballStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "Баскетбол";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s) =>
            new TournamentStatistics { SportType = "Баскетбол" };
    }
}