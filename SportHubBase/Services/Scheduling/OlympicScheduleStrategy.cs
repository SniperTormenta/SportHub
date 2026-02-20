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
            if (teams == null || teams.Count == 0) return null;
            if (teams.Count > 16)
                throw new ArgumentException("Максимум 16 команд для олимпийской системы.");

            int teamsCount = teams.Count;
            // Определяем количество участников в сетке (ближайшая степень 2)
            int powerOf2 = 8;
            if (teamsCount > 8) powerOf2 = 16;
            else if (teamsCount <= 4) powerOf2 = 4;
            else if (teamsCount <= 2) powerOf2 = 2;

            if (powerOf2 < 8) powerOf2 = 8; // По задаче минимум 8

            var bracket = new TournamentBracket();
            int roundCount = (int)Math.Log(powerOf2, 2);

            // 1. Создаем раунды и матчи
            for (int r = 0; r < roundCount; r++)
            {
                int matchesInRound = powerOf2 / (int)Math.Pow(2, r + 1);
                var round = new BracketRound
                {
                    Name = GetRoundName(matchesInRound),
                    RoundIndex = r
                };

                for (int m = 0; m < matchesInRound; m++)
                {
                    round.Matches.Add(new BracketMatch
                    {
                        RoundIndex = r,
                        MatchIndex = m,
                        IsFinal = (matchesInRound == 1)
                    });
                }
                bracket.Rounds.Add(round);
            }

            // 2. Создаем матч за 3-е место
            bracket.BronzeMatch = new BracketMatch
            {
                IsBronzeMatch = true,
                RoundIndex = roundCount - 1,
                MatchIndex = 1 // Визуально рядом с финалом
            };

            // 3. Настраиваем связи (графовая структура)
            for (int r = 0; r < roundCount - 1; r++)
            {
                var currentRound = bracket.Rounds[r];
                var nextRound = bracket.Rounds[r + 1];

                for (int m = 0; m < currentRound.Matches.Count; m++)
                {
                    var match = currentRound.Matches[m];
                    var targetMatch = nextRound.Matches[m / 2];

                    match.NextMatch = targetMatch;
                    match.NextMatchId = targetMatch.Id;
                    match.IsTeam1InNext = (m % 2 == 0);

                    // Если текущий раунд — полуфинал (в следующем раунде 1 матч), 
                    // то проигравший идет в матч за 3-е место.
                    if (nextRound.Matches.Count == 1)
                    {
                        match.BronzeLoserTarget = bracket.BronzeMatch;
                        match.BronzeLoserTargetId = bracket.BronzeMatch.Id;
                    }
                }
            }

            // 4. Заполняем первый раунд командами (с поддержкой BYE)
            var firstRoundMatches = bracket.Rounds[0].Matches;
            for (int i = 0; i < firstRoundMatches.Count; i++)
            {
                int teamIndex1 = i * 2;
                int teamIndex2 = i * 2 + 1;

                if (teamIndex1 < teams.Count)
                    firstRoundMatches[i].Team1 = teams[teamIndex1];

                if (teamIndex2 < teams.Count)
                    firstRoundMatches[i].Team2 = teams[teamIndex2];
                else
                {
                    // Вторая команда отсутствует — это BYE.
                    // Победитель (Team1) должен быть продвинут автоматически при старте.
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
