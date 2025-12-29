// Services/Results/ResultsCalculatorFactory.cs
using SportHubBase.Models;
using System.Collections.ObjectModel;

namespace SportHubBase.Services.Results
{
    /// Фабрика калькуляторов результатов по виду спорта (Tournament.SportType).
    /// Статическая для простоты (как ScheduleStrategyFactory).
    /// Легко расширяется новыми видами спорта.
    public static class ResultsCalculatorFactory
    {
        public static IResultsCalculator GetCalculator(Tournament tournament)
        {
            if (tournament == null || string.IsNullOrWhiteSpace(tournament.SportType))
                return null;

            switch (tournament.SportType.Trim())
            {
                case "Волейбол":
                    return new VolleyballResultsCalculator();

                // Заглушки для будущих видов
                case "Футбол":
                case "Баскетбол":
                case "Теннис":
                    return new StubResultsCalculator(tournament.SportType);

                default:
                    return new StubResultsCalculator(tournament.SportType);
            }
        }
    }

    /// Заглушка для нереализованных видов спорта.
    internal class StubResultsCalculator : IResultsCalculator
    {
        private readonly string _sport;
        public string Name => $"Заглушка: {_sport}";

        public StubResultsCalculator(string sport) => _sport = sport;

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