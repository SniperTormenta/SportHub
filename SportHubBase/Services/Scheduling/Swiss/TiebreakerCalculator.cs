using System;
using System.Collections.Generic;
using System.Linq;
using SportHubBase.Models;
using SportHubBase.Services.Results;
using SportHubBase.Services.Statistics;
using SportHubBase.Interfaces;

namespace SportHubBase.Services.Scheduling.Swiss
{
    public class TiebreakerCalculator : ITiebreakerCalculator
    {
        // Пользователь просил инжектить IStatisticsCalculator или IStatisticsCalculatorFactory
        private readonly IStatisticsCalculatorFactory _statisticsFactory;

        public TiebreakerCalculator(IStatisticsCalculatorFactory statisticsFactory)
        {
            _statisticsFactory = statisticsFactory;
        }

        public List<SwissTeamProgress> CalculateAndSort(Tournament tournament, IEnumerable<Team> teams, Dictionary<string, HashSet<string>> playedGraph, Dictionary<string, int> byeList)
        {
            var progresses = teams.Select(t => new SwissTeamProgress(t)
            {
                PlayedOpponents = playedGraph.ContainsKey(t.Name) ? playedGraph[t.Name] : new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                ByeCount = byeList.ContainsKey(t.Name) ? byeList[t.Name] : 0
            }).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            var strategy = ScoringStrategyFactory.GetStrategy(tournament);

            // 1. Считаем базовые Points и Wins из истории
            foreach (var match in tournament.Matches)
            {
                if (match.IsBye || string.Equals(match.Team2, "BYE", StringComparison.OrdinalIgnoreCase))
                {
                    // За Bye (пропуск) классически дают очки как за победу
                    if (!string.IsNullOrEmpty(match.Team1) && progresses.TryGetValue(match.Team1, out var p))
                    {
                        p.Wins += 1;
                        // Используем логику стратегии - победа со счетом 1:0 или другой дефолт
                        p.Points += strategy.CalculatePoints(1, 0); 
                    }
                    continue;
                }

                bool isPlayed = string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(match.Status, "Техническое поражение", StringComparison.OrdinalIgnoreCase);
                                
                if (!isPlayed || string.IsNullOrWhiteSpace(match.Team1) || string.IsNullOrWhiteSpace(match.Team2))
                    continue;

                var setsScore = ParsePair(match.SetsScore) ?? ParsePair(match.Team1QuickScore, match.Team2QuickScore);
                if (!setsScore.HasValue) continue;

                if (progresses.TryGetValue(match.Team1, out var p1) && progresses.TryGetValue(match.Team2, out var p2))
                {
                    p1.Points += strategy.CalculatePoints(setsScore.Value.left, setsScore.Value.right);
                    p2.Points += strategy.CalculatePoints(setsScore.Value.right, setsScore.Value.left);

                    if (setsScore.Value.left > setsScore.Value.right) p1.Wins++;
                    else if (setsScore.Value.left < setsScore.Value.right) p2.Wins++;
                }
            }

            // 2. Считаем Buchholz Cut 1 (Сумма очков всех соперников минус самый слабый)
            foreach (var p in progresses.Values)
            {
                var opponentsPoints = new List<double>();
                foreach (var oppName in p.PlayedOpponents)
                {
                    if (progresses.TryGetValue(oppName, out var opp))
                    {
                        opponentsPoints.Add(opp.Points);
                    }
                }

                if (opponentsPoints.Count > 1)
                {
                    var minPoints = opponentsPoints.Min();
                    p.BuchholzCut1 = opponentsPoints.Sum() - minPoints;
                }
                else
                {
                    p.BuchholzCut1 = opponentsPoints.Sum(); // Если всего 1 соперник или 0, вычитать нечего (или сумма равна 0)
                }
            }

            // 3. Сортировка: Points DESC -> BuchholzCut1 DESC -> Wins DESC -> InitialSeed ASC
            return progresses.Values
                .OrderByDescending(p => p.Points)
                .ThenByDescending(p => p.BuchholzCut1)
                .ThenByDescending(p => p.Wins)
                // Для InitialSeed чем меньше число (1, 2, 3...), тем выше приоритет (т.е. Seed 1 лучше чем Seed 10)
                .ThenBy(p => p.InitialSeed > 0 ? p.InitialSeed : int.MaxValue)
                .ToList();
        }

        private static (int left, int right)? ParsePair(string score)
        {
            if (string.IsNullOrWhiteSpace(score)) return null;
            var parts = score.Split(':');
            if (parts.Length != 2) return null;
            if (int.TryParse(parts[0].Trim(), out int l) && int.TryParse(parts[1].Trim(), out int r))
                return (l, r);
            return null;
        }

        private static (int left, int right)? ParsePair(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return null;
            if (int.TryParse(left.Trim(), out int l) && int.TryParse(right.Trim(), out int r))
                return (l, r);
            return null;
        }
    }
}
