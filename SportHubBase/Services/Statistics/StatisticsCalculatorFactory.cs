using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.ViewModels;
using System.Collections.ObjectModel;

namespace SportHubBase.Services.Statistics
{
    public class StatisticsCalculatorFactory : IStatisticsCalculatorFactory
    {
        public IStatisticsCalculator GetCalculator(string sportType)
        {
            if (string.IsNullOrWhiteSpace(sportType))
                return new DefaultStatisticsCalculator();

            switch (sportType.Trim().ToLower())
            {
                case "волейбол":
                    return new VolleyballStatisticsCalculator();
                case "футбол":
                    return new FootballStatisticsCalculator();
                case "баскетбол":
                    return new BasketballStatisticsCalculator();
                default:
                    return new DefaultStatisticsCalculator();
            }
        }
    }

    // ИЗМЕНИТЬ internal НА public!
    public class DefaultStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "По умолчанию";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s) =>
            new TournamentStatistics { SportType = t?.SportType ?? "" };
    }

    public class VolleyballStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "Волейбол";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s) =>
            new TournamentStatistics { SportType = "Волейбол" };
    }

    public class FootballStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "Футбол";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s) =>
            new TournamentStatistics { SportType = "Футбол" };
    }

    public class BasketballStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "Баскетбол";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s) =>
            new TournamentStatistics { SportType = "Баскетбол" };
    }
}