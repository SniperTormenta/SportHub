// Services/Scheduling/OlympicScheduleStrategy.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling
{
    /// <summary>
    /// Стратегия для олимпийской системы (single-elimination).
    /// Генерирует структуру BracketMatch с автоматическим продвижением.
    /// </summary>
    public class OlympicScheduleStrategy : IScheduleStrategy
    {
        public string Name => "Олимпийский";

        public bool IsImplemented => true;

        /// <summary>
        /// Возвращает пустой список, так как олимпийская система использует специальную модель Bracket.
        /// </summary>
        public IEnumerable<Match> GenerateSchedule(IList<Team> teams)
        {
            return new List<Match>();
        }

        /// <summary>
        /// Генерирует олимпийскую сетку на 8-16 команд.
        /// </summary>
        public TournamentBracket GenerateBracket(IList<Team> teams)
        {
            if (teams == null || teams.Count == 0)
                return new TournamentBracket();

            if (teams.Count > 32)
                throw new ArgumentException("Максимум 32 команды");

            var bracket = new TournamentBracket();

            int n = teams.Count;

            // 1. Первый раунд: сколько реальных пар и бай
            int realPairs = n / 2;
            int byeCount = n % 2;
            int firstRoundSlots = realPairs + byeCount;

            // Создаём первый раунд
            var firstRound = new BracketRound
            {
                Name = GetRoundName(firstRoundSlots),
                RoundIndex = 0
            };

            for (int i = 0; i < firstRoundSlots; i++)
            {
                firstRound.Matches.Add(new BracketMatch
                {
                    RoundIndex = 0,
                    MatchIndex = i
                });
            }
            bracket.Rounds.Add(firstRound);

            // 2. Остальные раунды
            int currentSlots = firstRoundSlots;
            int roundIndex = 1;

            while (currentSlots > 1)
            {
                var round = new BracketRound
                {
                    Name = GetRoundName(currentSlots / 2),
                    RoundIndex = roundIndex++
                };

                int matches = (currentSlots + 1) / 2;
                for (int i = 0; i < matches; i++)
                {
                    round.Matches.Add(new BracketMatch
                    {
                        RoundIndex = round.RoundIndex,
                        MatchIndex = i,
                        IsFinal = (currentSlots == 2)
                    });
                }

                bracket.Rounds.Add(round);
                currentSlots = matches;
            }

            // 3. Бронза — только если есть полуфинал (минимум 4 команды)
            if (n >= 4)
            {
                bracket.BronzeMatch = new BracketMatch
                {
                    IsBronzeMatch = true,
                    RoundIndex = bracket.Rounds.Count
                };
            }

            // 4. Связи — только если есть куда вести
            for (int r = 0; r < bracket.Rounds.Count - 1; r++)
            {
                var current = bracket.Rounds[r];
                var next = bracket.Rounds[r + 1];

                for (int i = 0; i < current.Matches.Count; i++)
                {
                    var match = current.Matches[i];
                    int targetIndex = i / 2;

                    if (targetIndex < next.Matches.Count)
                    {
                        var target = next.Matches[targetIndex];
                        match.NextMatch = target;
                        match.IsTeam1InNext = (i % 2 == 0);
                    }

                    // Бронза — только если это полуфинал (следующий раунд — финал)
                    if (bracket.BronzeMatch != null && next.Matches.Count == 1 && targetIndex < next.Matches.Count)
                    {
                        match.BronzeLoserTarget = bracket.BronzeMatch;
                    }
                }
            }

            // 5. Заполняем первый раунд
            if (bracket.Rounds.Count > 0)
            {
                var firstRoundMatches = bracket.Rounds[0];
                int teamIdx = 0;

                // Реальные пары
                for (int i = 0; i < realPairs; i++)
                {
                    var match = firstRoundMatches.Matches[i];
                    match.Team1 = teams[teamIdx++];
                    match.Team2 = teams[teamIdx++];
                }

                // Бай — последний слот, если есть
                if (byeCount > 0 && teamIdx < teams.Count)
                {
                    var byeMatch = firstRoundMatches.Matches[firstRoundMatches.Matches.Count - 1];
                    byeMatch.Team1 = teams[teamIdx++];
                    // Team2 = null → IsBye = true
                }
            }

            return bracket;
        }

        private string GetRoundName(int matchesInRound)
        {
            switch (matchesInRound)
            {
                case 1: return "Финал";
                case 2: return "Полуфинал";
                case 4: return "1/4 финала";
                case 8: return "1/8 финала";
                default: return $"Раунд {matchesInRound}";
            }
        }
    }
}
