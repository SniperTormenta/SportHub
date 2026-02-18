using System;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Models.Results;
using System.Collections.Generic;

namespace SportHubBase.Services.Results
{
    /// <summary>
    /// Фабрика провайдеров результатов.
    /// Выбирает нужную стратегию расчёта (круг, плей-офф и т.д.) на основе типа турнира.
    /// </summary>
    public class ResultsProviderFactory : IResultsProviderFactory
    {
        public IResultsProvider GetProvider(Tournament tournament)
        {
            if (tournament == null || string.IsNullOrWhiteSpace(tournament.Type))
            {
                return new StubResultsProvider("Неизвестный тип");
            }

            string type = tournament.Type.Trim();

            // В будущем здесь будет switch по типам турниров
            if (string.Equals(type, "Круговой", StringComparison.OrdinalIgnoreCase))
            {
                return new RoundRobinResultsProvider();
            }

            // Остальные системы пока как заглушки
            return new StubResultsProvider(type);
        }
    }

    /// <summary>
    /// Заглушка для систем в разработке.
    /// </summary>
    internal class StubResultsProvider : IResultsProvider
    {
        private readonly string _type;

        public StubResultsProvider(string type)
        {
            _type = type;
        }

        public ResultsData ComputeResults(Tournament tournament, IEnumerable<Match> matches)
        {
            // Возвращаем базовый объект с сообщением "в разработке"
            // В зависимости от типа можно возвращать соответствующие заглушки-данные
            
            ResultsData data;
            
            if (string.Equals(_type, "Плей-офф", StringComparison.OrdinalIgnoreCase))
                data = new OlympicBracketResultsData();
            else if (string.Equals(_type, "Швейцарка", StringComparison.OrdinalIgnoreCase))
                data = new SwissResultsData();
            else if (string.Equals(_type, "Группы + плей-офф", StringComparison.OrdinalIgnoreCase))
                data = new MultiStageResultsData();
            else
                data = new OlympicBracketResultsData(); // Fallback

            data.StatusMessage = string.Format("Таблица результатов для формата \"{0}\" — в разработке.", _type);
            data.LastUpdate = DateTime.Now;
            data.TournamentName = tournament?.Name ?? "Турнир";
            
            return data;
        }
    }
}
