// Services/Results/ResultsProviderFactory.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services.Result;
using SportHubBase.Services.Results.Data;
using System.Collections.ObjectModel;

namespace SportHubBase.Services.Results
{
    public class ResultsProviderFactory : IResultsProviderFactory
    {
        public IResultsProvider GetProvider(Tournament tournament)
        {
            if (tournament == null)
                return new StubResultsProvider("Неизвестно");

            var typeLower = tournament.Type?.ToLowerInvariant() ?? "";

            if (typeLower.Contains("кругов") || typeLower == "круговой")
                return new RoundRobinResultsProvider();

            if (typeLower.Contains("олимп") || typeLower.Contains("плей-офф"))
                return new OlympicResultsProvider();   // ← новый класс, ниже

            //if (typeLower.Contains("Швейцарский"))
            //    return new SwissResultsProvider();     // Не реализовано

            return new StubResultsProvider(tournament.Type);
        }
    }

    internal class StubResultsProvider : IResultsProvider
    {
        private readonly string _type;
        public string Name => $"Заглушка: {_type}";

        public StubResultsProvider(string type)
        {
            _type = type;
        }

        public ResultsData ComputeResults(Tournament tournament, ObservableCollection<Match> schedule)
        {
            // Возвращаем пустую структуру с сообщением
            // В зависимости от типа можно вернуть разные заглушки (Olympic, Swiss)
            // Но пока просто базовый с сообщением
            
            // Здесь нужен какой-то конкретный класс, так как ResultsData абстрактный.
            // Можно использовать MultiStageResultsData как "общий" или создать специальный GenericResultsData.
            // Используем наиболее подходящий по названию или просто RoundRobin с сообщением.
            if (string.Equals(_type, "Плей-офф", System.StringComparison.OrdinalIgnoreCase))
                return new OlympicBracketResultsData { StatusMessage = "Сетка плей-офф в разработке." };

            if (string.Equals(_type, "Швейцарка", System.StringComparison.OrdinalIgnoreCase))
                return new SwissResultsData { StatusMessage = "Швейцарская система в разработке." };

            return new RoundRobinResultsData { StatusMessage = $"Тип \"{_type}\" пока не поддерживается." };
        }
    }


}
