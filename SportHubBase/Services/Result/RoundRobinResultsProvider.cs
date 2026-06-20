// Services/Results/RoundRobinResultsProvider.cs
using SportHubBase.Models;
using SportHubBase.Interfaces;
using SportHubBase.Services.Results.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportHubBase.Services.Results
{
    /// <summary>
    /// Провайдер результатов для кругового турнира.
    /// Реализует IResultsProvider, возвращает RoundRobinResultsData.
    /// </summary>
    public class RoundRobinResultsProvider : IResultsProvider
    {
        public string Name => "Круговой (включая волейбол/футбол)";

        private IScoringStrategy _scoringStrategy;

        public ResultsData ComputeResults(Tournament tournament, ObservableCollection<Match> schedule)
        {
            var data = new RoundRobinResultsData();

            if (tournament == null)
            {
                data.StatusMessage = "Турнир не найден.";
                return data;
            }

            data.SportType = tournament.SportType;

            // Инициализируем стратегию подсчёта очков (она нужна для Compare и CalculatePoints)
            // Фабрика сама разберётся по tournament.ScoringSystem / SportType
            _scoringStrategy = ScoringStrategyFactory.GetStrategy(tournament);

            if (tournament.Teams == null || tournament.Teams.Count == 0)
            {
                data.StatusMessage = "Команды ещё не добавлены.";
                return data;
            }

            if (!string.Equals(tournament.Type, "Круговой", StringComparison.OrdinalIgnoreCase))
            {
                data.StatusMessage = $"Формат \"{tournament.Type}\" не поддерживается этим провайдером.";
                return data;
            }

            // 1. Создаём структуру таблицы (Rows)
            var sortedTeams = tournament.Teams.ToList();
            int teamCount = sortedTeams.Count;

            // Заполняем HeaderNumbers (1..N)
            var headers = new List<int>();
            for (int i = 1; i <= teamCount; i++) headers.Add(i);
            data.HeaderNumbers = new ReadOnlyCollection<int>(headers);

            // Инициализация строк
            var rows = new List<ResultRow>();
            for (int i = 0; i < teamCount; i++)
            {
                var row = new ResultRow
                {
                    Index = i + 1,
                    TeamName = sortedTeams[i].Name,
                    Cells = new ObservableCollection<CellResult>() // Важно инициализировать
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
                rows.Add(row);
            }

            // 2. Считаем статистику
            // Словарь для быстрого доступа к статистике команды по имени
            var teamStats = sortedTeams.ToDictionary(
                t => t.Name,
                t => new ResultRow
                {
                    TeamName = t.Name,
                    // Остальные поля 0 по умолчанию
                    Cells = new ObservableCollection<CellResult>()
                },
                StringComparer.OrdinalIgnoreCase);

            var nameToIndex = sortedTeams
                .Select((team, index) => new { team.Name, index })
                .ToDictionary(k => k.Name, v => v.index, StringComparer.OrdinalIgnoreCase);

            foreach (var match in schedule)
            {
                if (match == null || string.IsNullOrWhiteSpace(match.Team1) || string.IsNullOrWhiteSpace(match.Team2))
                    continue;

                if (!teamStats.TryGetValue(match.Team1, out var stats1) ||
                    !teamStats.TryGetValue(match.Team2, out var stats2))
                    continue;

                bool isPlayed = string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(match.Status, "Техническое поражение", StringComparison.OrdinalIgnoreCase);
                
                if (!isPlayed) continue;

                // Парсинг счёта
                var setsScore = ParsePair(match.SetsScore) ?? ParsePair(match.Team1QuickScore, match.Team2QuickScore);
                if (!setsScore.HasValue) continue;

                var totalScore = ParsePair(match.TotalScore);
                
                // Обновляем статистику
                UpdateStats(stats1, stats2, setsScore.Value, totalScore, _scoringStrategy);

                // Заполняем ячейки
                if (nameToIndex.TryGetValue(match.Team1, out int idx1) &&
                    nameToIndex.TryGetValue(match.Team2, out int idx2))
                {
                    UpdateCells(rows[idx1].Cells[idx2], rows[idx2].Cells[idx1], 
                        setsScore.Value.left, setsScore.Value.right, 
                        string.Equals(match.Status, "Техническое поражение", StringComparison.OrdinalIgnoreCase));
                }
            }

            // 3. Расчёт коэффициентов
            foreach (var stats in teamStats.Values)
            {
                stats.SetsRatio = stats.SetsLost > 0 ? (double)stats.SetsWon / stats.SetsLost : (stats.SetsWon > 0 ? double.MaxValue : 0);
                stats.PointsRatio = stats.PointsConceded > 0 ? (double)stats.PointsScored / stats.PointsConceded : (stats.PointsScored > 0 ? double.MaxValue : 0);
            }

            // 4. Сортировка
            var headToHead = BuildHeadToHead(schedule);
            var sortedStats = teamStats.Values
                .OrderBy(s => s, new ResultRowComparer(_scoringStrategy))
                .ToList();
            
            ApplyHeadToHeadTieBreaker(sortedStats, headToHead, _scoringStrategy);

            // 5. Присвоение мест и формирование итогового списка Rows
            int place = 1;
            for (int i = 0; i < sortedStats.Count; i++)
            {
                if (i > 0 && _scoringStrategy.Compare(sortedStats[i - 1], sortedStats[i]) < 0)
                    place = i + 1;
                
                var stats = sortedStats[i];
                
                // Находим строку таблицы, соответствующую этой команде
                var row = rows.First(r => r.TeamName == stats.TeamName);
                
                // Копируем подсчитанную статистику в строку отображения
                row.Place = place;
                row.Wins = stats.Wins;
                row.Losses = stats.Losses;
                row.SetsWon = stats.SetsWon;
                row.SetsLost = stats.SetsLost;
                row.Points = stats.Points;
                row.PointsScored = stats.PointsScored;
                row.PointsConceded = stats.PointsConceded;
                row.SetsRatio = stats.SetsRatio;
                row.PointsRatio = stats.PointsRatio;

            }

            foreach (var row in rows)
            {
                data.Rows.Add(row);
            }

            data.StatusMessage = ""; // Всё ок
            return data;
        }

        private void UpdateStats(ResultRow s1, ResultRow s2, (int s1, int s2) sets, (int p1, int p2)? points, IScoringStrategy strategy)
        {
            s1.SetsWon += sets.s1;
            s1.SetsLost += sets.s2;
            s2.SetsWon += sets.s2;
            s2.SetsLost += sets.s1;

            if (points.HasValue)
            {
                s1.PointsScored += points.Value.p1;
                s1.PointsConceded += points.Value.p2;
                s2.PointsScored += points.Value.p2;
                s2.PointsConceded += points.Value.p1;
            }

            s1.Points += strategy.CalculatePoints(sets.s1, sets.s2);
            s2.Points += strategy.CalculatePoints(sets.s2, sets.s1);

            if (sets.s1 > sets.s2) { s1.Wins++; s2.Losses++; }
            else if (sets.s1 < sets.s2) { s1.Losses++; s2.Wins++; }
        }

        private void UpdateCells(CellResult c1, CellResult c2, int s1, int s2, bool isTech)
        {
            c1.HomeSets = s1;
            c1.AwaySets = s2;
            c1.DisplayText = $"{s1}:{s2}";
            c1.IsTechnicalDefeat = isTech;

            c2.HomeSets = s2;
            c2.AwaySets = s1;
            c2.DisplayText = $"{s2}:{s1}";
            c2.IsTechnicalDefeat = isTech;

            if (s1 > s2)
            {
                c1.Outcome = "WIN";
                c2.Outcome = "LOSS";
            }
            else if (s1 < s2)
            {
                c1.Outcome = "LOSS";
                c2.Outcome = "WIN";
            }
            else
            {
                c1.Outcome = "DRAW";
                c2.Outcome = "DRAW";
            }
        }

        // --- Вспомогательные методы (копии из старого VolleyballResultsCalculator) ---

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

        private static Dictionary<string, Dictionary<string, int>> BuildHeadToHead(ObservableCollection<Match> schedule)
        {
            var dict = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            var playedStatuses = new[] { "Сыгран", "Техническое поражение" };
            
            foreach (var m in schedule.Where(m => playedStatuses.Contains(m.Status, StringComparer.OrdinalIgnoreCase)))
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

                    // Сортировка внутри группы равных по личным встречам
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
