// Models/TournamentBracket.cs
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace SportHubBase.Models
{
    /// <summary>
    /// Полная модель олимпийской сетки турнира.
    /// Хранит структуру раундов и специальные матчи. Подлежит JSON-сериализации.
    /// </summary>
    public class TournamentBracket
    {
        /// <summary>
        /// Список раундов (столбцов) сетки.
        /// </summary>
        [JsonProperty("rounds")]
        public ObservableCollection<BracketRound> Rounds { get; set; } = new ObservableCollection<BracketRound>();

        /// <summary>
        /// Ссылка на матч за 3-е место (бронзовый финал). 
        /// В UI обычно отображается под основным финалом.
        /// </summary>
        [JsonProperty("bronzeMatch")]
        public BracketMatch BronzeMatch { get; set; }

        /// <summary>
        /// Вспомогательный метод для восстановления объектных ссылок (NextMatch, BronzeLoserTarget) 
        /// после десериализации из JSON, используя сохраненные ID.
        /// </summary>
        public void ReconnectReferences()
        {
            // Собираем все матчи в плоский словарь для быстрого поиска
            var allMatches = Rounds
                .SelectMany(r => r.Matches)
                .ToDictionary(m => m.Id);

            if (BronzeMatch != null && !allMatches.ContainsKey(BronzeMatch.Id))
                allMatches[BronzeMatch.Id] = BronzeMatch;

            foreach (var match in allMatches.Values)
            {
                if (match.NextMatchId.HasValue && allMatches.TryGetValue(match.NextMatchId.Value, out var next))
                {
                    match.NextMatch = next;
                }

                if (match.BronzeLoserTargetId.HasValue && allMatches.TryGetValue(match.BronzeLoserTargetId.Value, out var bronze))
                {
                    match.BronzeLoserTarget = bronze;
                }
            }
        }
    }
}
