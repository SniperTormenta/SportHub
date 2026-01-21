// Services/Results/ResultsCalculatorFactory.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services.Results;
using System.Collections.ObjectModel;

namespace SportHubBase.Services.Results
{
    public class ResultsCalculatorFactory : IResultsCalculatorFactory
    {
        public IResultsCalculator GetCalculator(Tournament tournament)
        {
            if (tournament == null || string.IsNullOrWhiteSpace(tournament.SportType))
                return new StubResultsCalculator("Неизвестно");

            string sport = tournament.SportType.Trim();

            switch (sport)
            {
                case "Волейбол":
                    return new VolleyballResultsCalculator();

                case "Футбол":
                case "Баскетбол":
                case "Теннис":
                    return new StubResultsCalculator(sport);

                default:
                    return new StubResultsCalculator(sport);
            }
        }
    }

    internal class StubResultsCalculator : IResultsCalculator
    {
        private readonly string _sport;

        public string Name => $"Заглушка: {_sport}";

        public StubResultsCalculator(string sport)
        {
            _sport = sport;
        }

        public void Calculate(Tournament tournament,
                              ObservableCollection<Match> schedule,
                              ObservableCollection<ResultRow> resultsTable,
                              out string resultsMessage)
        {
            resultsTable.Clear();
            resultsMessage = $"Расчёт результатов для вида спорта \"{_sport}\" — в разработке.";
        }
    }
}