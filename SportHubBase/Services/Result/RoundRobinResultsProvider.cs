using System;
using System.Collections.Generic;
using System.Linq;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Models.Results;
using SportHubBase.Services.Results.Scoring;

namespace SportHubBase.Services.Results
{
    /// <summary>
    /// Провайдер результатов для круговых турниров.
    /// Переносит логику волейбольного расчёта в новую полиморфную модель.
    /// </summary>
    public class RoundRobinResultsProvider : IResultsProvider
    {
        public ResultsData ComputeResults(Tournament tournament, IEnumerable<Match> matches)
        {
            var data = new RoundRobinResultsData();
            data.LastUpdate = DateTime.Now;
            data.TournamentName = tournament?.Name ?? "Турнир";

            if (tournament == null)
            {
                data.StatusMessage = "Турнир не найден.";
                return data;
            }

            if (tournament.Teams == null || tournament.Teams.Count == 0)
            {
                data.StatusMessage = "Команды ещё не добавлены.";
                return data;
            }

            if (!string.Equals(tournament.Type, "Круговой", StringComparison.OrdinalIgnoreCase))
            {
                data.StatusMessage = $"Круговой провайдер не поддерживает формат \"{tournament.Type}\".";
                return data;
            }

            try
            {
                IScoringStrategy scoringStrategy = ScoringStrategyFactory.GetStrategy(tournament);
                var sortedTeams = tournament.Teams.OrderBy(t => t.Name).ToList();
                int teamCount = sortedTeams.Count;

                // Базовая структура таблицы
                var initialRows = new List<ResultRow>();
                for (int i = 0; i < teamCount; i++)
                {
                    var row = new ResultRow
                    {
                        Index = i + 1,
                        TeamName = sortedTeams[i].Name
                    };

                    for (int j = 0; j < teamCount; j++)
                    {
                        var cell = new CellResult();
                        if (i == j)
                        {
                            cell.Outcome = "SELF";
                            cell.DisplayText = "";
                        }
                        else
                        {
                            cell.Outcome = "NOT_PLAYED";
                            cell.DisplayText = "-";
                        }
                        row.Cells.Add(cell);
                    }
                    initialRows.Add(row);
                }

                // Статистика (локальный словарь)
                var teamStats = sortedTeams.ToDictionary(
                    t => t.Name,
                    t => new ResultRow
                    {
                        TeamName = t.Name,
                        Wins = 0,
                        Losses = 0,
                        SetsWon = 0,
                        SetsLost = 0,
                        Points = 0,
                        PointsScored = 0,
                        PointsConceded = 0
                    },
                    StringComparer.OrdinalIgnoreCase);

                var nameToIndex = sortedTeams
                    .Select((team, index) => new { team.Name, index })
                    .ToDictionary(k => k.Name, v => v.index, StringComparer.OrdinalIgnoreCase);

                // Расчёт матчей
                var matchesList = matches.ToList();
                foreach (var match in matchesList)
                {
                    if (match == null || string.IsNullOrWhiteSpace(match.Team1) || string.IsNullOrWhiteSpace(match.Team2))
                        continue;

                    if (!teamStats.TryGetValue(match.Team1, out var team1Stats) ||
                        !teamStats.TryGetValue(match.Team2, out var team2Stats))
                        continue;

                    bool isPlayed = string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase);
                    bool isTechDefeat = string.Equals(match.Status, "Техническое поражение", StringComparison.OrdinalIgnoreCase);

                    if (!isPlayed && !isTechDefeat)
                        continue;

                    var setsScore = ParsePair(match.SetsScore) ?? ParsePair(match.Team1QuickScore, match.Team2QuickScore);
                    if (!setsScore.HasValue) continue;

                    int t1Sets = setsScore.Value.left;
                    int t2Sets = setsScore.Value.right;

                    var totalScore = ParsePair(match.TotalScore);
                    int t1Points = totalScore?.left ?? 0;
                    int t2Points = totalScore?.right ?? 0;

                    team1Stats.SetsWon += t1Sets;
                    team1Stats.SetsLost += t2Sets;
                    team2Stats.SetsWon += t2Sets;
                    team2Stats.SetsLost += t1Sets;

                    team1Stats.PointsScored += t1Points;
                    team1Stats.PointsConceded += t2Points;
                    team2Stats.PointsScored += t2Points;
                    team2Stats.PointsConceded += t1Points;

                    team1Stats.Points += scoringStrategy.CalculatePoints(t1Sets, t2Sets);
                    team2Stats.Points += scoringStrategy.CalculatePoints(t2Sets, t1Sets);

                    if (t1Sets > t2Sets) { team1Stats.Wins++; team2Stats.Losses++; }
                    else if (t1Sets < t2Sets) { team1Stats.Losses++; team2Stats.Wins++; }

                    // Заполняем ячейки
                    if (nameToIndex.TryGetValue(match.Team1, out int idx1) &&
                        nameToIndex.TryGetValue(match.Team2, out int idx2))
                    {
                        var cell1 = new CellResult { HomeSets = t1Sets, AwaySets = t2Sets, DisplayText = string.Format("{0}:{1}", t1Sets, t2Sets), IsTechnicalDefeat = isTechDefeat };
                        var cell2 = new CellResult { HomeSets = t2Sets, AwaySets = t1Sets, DisplayText = string.Format("{0}:{1}", t2Sets, t1Sets), IsTechnicalDefeat = isTechDefeat };

                        if (t1Sets > t2Sets) { cell1.Outcome = "WIN"; cell2.Outcome = "LOSS"; }
                        else if (t1Sets < t2Sets) { cell1.Outcome = "LOSS"; cell2.Outcome = "WIN"; }
                        else { cell1.Outcome = "DRAW"; cell2.Outcome = "DRAW"; }

                        initialRows[idx1].Cells[idx2] = cell1;
                        initialRows[idx2].Cells[idx1] = cell2;
                    }
                }

                // Коэффициенты
                foreach (var s in teamStats.Values)
                {
                    s.SetsRatio = s.SetsLost > 0 ? (double)s.SetsWon / s.SetsLost : (s.SetsWon > 0 ? double.MaxValue : 0);
                    s.PointsRatio = s.PointsConceded > 0 ? (double)s.PointsScored / s.PointsConceded : (s.PointsScored > 0 ? double.MaxValue : 0);
                }

                // Сортировка
                var sorted = teamStats.Values
                    .OrderBy(s => s, new ResultRowComparer(scoringStrategy))
                    .ToList();

                // Личные встречи (оставляем логику)
                var headToHead = BuildHeadToHead(matchesList);
                ApplyHeadToHeadTieBreaker(sorted, headToHead, scoringStrategy);

                // Места
                int place = 1;
                for (int i = 0; i < sorted.Count; i++)
                {
                    if (i > 0 && scoringStrategy.Compare(sorted[i - 1], sorted[i]) < 0)
                        place = i + 1;
                    sorted[i].Place = place;
                }

                // Финальное построение Rows для RoundRobinResultsData
                foreach (var stats in sorted)
                {
                    var originalRow = initialRows.FirstOrDefault(r => string.Equals(r.TeamName, stats.TeamName, StringComparison.OrdinalIgnoreCase));
                    
                    var newRow = new ResultRow
                    {
                        Index = data.Rows.Count + 1,
                        TeamName = stats.TeamName,
                        Wins = stats.Wins,
                        Losses = stats.Losses,
                        SetsWon = stats.SetsWon,
                        SetsLost = stats.SetsLost,
                        Points = stats.Points,
                        Place = stats.Place,
                        SetsRatio = stats.SetsRatio,
                        PointsRatio = stats.PointsRatio,
                        PointsScored = stats.PointsScored,
                        PointsConceded = stats.PointsConceded
                    };

                    // Копируем ячейки в новом порядке
                    foreach (var opponentStats in sorted)
                    {
                        var opponentName = opponentStats.TeamName;
                        if (string.Equals(newRow.TeamName, opponentName, StringComparison.OrdinalIgnoreCase))
                        {
                            newRow.Cells.Add(new CellResult { Outcome = "SELF", DisplayText = "" });
                        }
                        else if (originalRow != null)
                        {
                            int oppIdx = sortedTeams.FindIndex(t => string.Equals(t.Name, opponentName, StringComparison.OrdinalIgnoreCase));
                            if (oppIdx >= 0 && oppIdx < originalRow.Cells.Count)
                            {
                                newRow.Cells.Add(originalRow.Cells[oppIdx]);
                            }
                            else
                            {
                                newRow.Cells.Add(new CellResult { Outcome = "NOT_PLAYED", DisplayText = "-" });
                            }
                        }
                        else
                        {
                            newRow.Cells.Add(new CellResult { Outcome = "NOT_PLAYED", DisplayText = "-" });
                        }
                    }
                    data.Rows.Add(newRow);
                }

                // Заголовки (1..N)
                var headers = new List<int>();
                for (int i = 1; i <= data.Rows.Count; i++) headers.Add(i);
                data.HeaderNumbers = headers;

                data.StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                data.StatusMessage = "Ошибка при расчёте результатов: " + ex.Message;
            }

            return data;
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

        private class ResultRowComparer : IComparer<ResultRow>
        {
            private readonly IScoringStrategy _strategy;
            public ResultRowComparer(IScoringStrategy strategy) => _strategy = strategy;
            public int Compare(ResultRow x, ResultRow y) => _strategy.Compare(x, y);
        }

        private static Dictionary<string, Dictionary<string, int>> BuildHeadToHead(List<Match> matches)
        {
            var dict = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            var playedStatuses = new List<string> { "Сыгран", "Техническое поражение" };
            
            foreach (var m in matches.Where(m => m != null && playedStatuses.Contains(m.Status)))
            {
                var score = ParsePair(m.SetsScore) ?? ParsePair(m.Team1QuickScore, m.Team2QuickScore);
                if (!score.HasValue) continue;

                int result = score.Value.left > score.Value.right ? 1 : (score.Value.left < score.Value.right ? -1 : 0);

                if (!dict.ContainsKey(m.Team1)) dict[m.Team1] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (!dict.ContainsKey(m.Team2)) dict[m.Team2] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                dict[m.Team1][m.Team2] = result;
                dict[m.Team2][m.Team1] = -result;
            }
            return dict;
        }

        private static void ApplyHeadToHeadTieBreaker(List<ResultRow> sorted, Dictionary<string, Dictionary<string, int>> headToHead, IScoringStrategy strategy)
        {
            for (int i = 0; i < sorted.Count; i++)
            {
                var current = sorted[i];
                var equals = sorted.Skip(i + 1)
                    .TakeWhile(t => strategy.Compare(current, t) == 0)
                    .ToList();

                if (equals.Count > 0)
                {
                    var group = new List<ResultRow> { current };
                    group.AddRange(equals);

                    group = group.OrderByDescending(t =>
                    {
                        int h2h = 0;
                        if (headToHead.TryGetValue(t.TeamName, out var opponents))
                        {
                            foreach (var other in group.Where(o => o != t))
                            {
                                if (opponents.TryGetValue(other.TeamName, out int res))
                                    h2h += res;
                            }
                        }
                        return h2h;
                    }).ToList();

                    for (int j = 0; j < group.Count; j++)
                        sorted[i + j] = group[j];
                }
            }
        }
    }
}
