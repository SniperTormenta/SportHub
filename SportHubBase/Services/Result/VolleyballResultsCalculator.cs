// Services/Results/VolleyballResultsCalculator.cs
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static System.StringComparer;

namespace SportHubBase.Services.Results
{
    /// Калькулятор результатов для волейбола.
    /// Содержит всю логику, ранее бывшую в TournamentViewModel.UpdateResultsFromMatches.
    /// Итальянская система: 3-2-1-0 очки, коэффициенты по сетам/мячам, личные встречи.
    /// Только для кругового формата (другие — в будущем).
    public class VolleyballResultsCalculator : IResultsCalculator
    {
        public string Name => "Волейбол (итальянская система)";

        public void Calculate(Tournament tournament,
                              ObservableCollection<Match> schedule,
                              ObservableCollection<ResultRow> resultsTable,
                              out string resultsMessage)
        {
            resultsMessage = string.Empty;

            if (tournament == null)
            {
                resultsMessage = "Турнир не найден.";
                resultsTable.Clear();
                return;
            }

            if (tournament.Teams == null || tournament.Teams.Count == 0)
            {
                resultsMessage = "Команды ещё не добавлены.";
                resultsTable.Clear();
                return;
            }

            if (!string.Equals(tournament.Type, "Круговой", StringComparison.OrdinalIgnoreCase))
            {
                resultsMessage = $"Таблица результатов для формата \"{tournament.Type}\" — в разработке.";
                resultsTable.Clear();
                return;
            }

            // Очищаем и строим базовую структуру таблицы (алфавитный порядок)
            resultsTable.Clear();
            var sortedTeams = tournament.Teams.OrderBy(t => t.Name).ToList();
            int teamCount = sortedTeams.Count;

            for (int i = 0; i < teamCount; i++)
            {
                var row = new ResultRow
                {
                    Index = i + 1,
                    TeamName = sortedTeams[i].Name
                };
                for (int j = 0; j < teamCount; j++)
                {
                    row.Cells.Add(i == j ? "SELF" : string.Empty);
                }
                resultsTable.Add(row);
            }

            // Словарь статистики по командам
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

            // Обрабатываем сыгранные матчи
            foreach (var match in schedule)
            {
                if (match == null || string.IsNullOrWhiteSpace(match.Team1) || string.IsNullOrWhiteSpace(match.Team2))
                    continue;

                if (!teamStats.TryGetValue(match.Team1, out var team1Stats) ||
                    !teamStats.TryGetValue(match.Team2, out var team2Stats))
                    continue;

                if (!string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Парсинг счёта по сетам (SetsScore или QuickScore)
                var setsScore = ParsePair(match.SetsScore) ??
                                ParsePair(match.Team1QuickScore, match.Team2QuickScore);

                if (!setsScore.HasValue) continue;

                int team1Sets = setsScore.Value.left;
                int team2Sets = setsScore.Value.right;

                // Общий счёт мячей
                var totalScore = ParsePair(match.TotalScore);
                int team1Points = totalScore?.left ?? 0;
                int team2Points = totalScore?.right ?? 0;

                // Статистика сетов
                team1Stats.SetsWon += team1Sets;
                team1Stats.SetsLost += team2Sets;
                team2Stats.SetsWon += team2Sets;
                team2Stats.SetsLost += team1Sets;

                // Статистика мячей
                team1Stats.PointsScored += team1Points;
                team1Stats.PointsConceded += team2Points;
                team2Stats.PointsScored += team2Points;
                team2Stats.PointsConceded += team1Points;

                // Итальянские очки
                team1Stats.Points += CalculateItalianPoints(team1Sets, team2Sets);
                team2Stats.Points += CalculateItalianPoints(team2Sets, team1Sets);

                // Победы/поражения
                if (team1Sets > team2Sets) { team1Stats.Wins++; team2Stats.Losses++; }
                else if (team1Sets < team2Sets) { team1Stats.Losses++; team2Stats.Wins++; }

                // Ячейки таблицы
                if (nameToIndex.TryGetValue(match.Team1, out int t1) &&
                    nameToIndex.TryGetValue(match.Team2, out int t2))
                {
                    string cell1 = team1Sets > team2Sets ? "1" : (team1Sets < team2Sets ? "0" : "½");
                    string cell2 = cell1 == "1" ? "0" : (cell1 == "0" ? "1" : "½");

                    resultsTable[t1].Cells[t2] = cell1;
                    resultsTable[t2].Cells[t1] = cell2;
                }
            }

            // Коэффициенты
            foreach (var stats in teamStats.Values)
            {
                stats.SetsRatio = stats.SetsLost > 0 ? (double)stats.SetsWon / stats.SetsLost :
                                  stats.SetsWon > 0 ? double.MaxValue : 0;

                stats.PointsRatio = stats.PointsConceded > 0 ? (double)stats.PointsScored / stats.PointsConceded :
                                    stats.PointsScored > 0 ? double.MaxValue : 0;
            }

            // Личные встречи (для сортировки равных)
            var headToHead = BuildHeadToHead(schedule);

            // Сортировка по очкам → коэф.сетам → коэф.мячам → личные встречи
            var sorted = teamStats.Values
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.SetsRatio)
                .ThenByDescending(s => s.PointsRatio)
                .ToList();

            ApplyHeadToHeadTieBreaker(sorted, headToHead);

            // Места
            int place = 1;
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i > 0 && IsBetter(sorted[i - 1], sorted[i]))
                    place = i + 1;
                sorted[i].Place = place;
            }

            // Перестраиваем таблицу в порядке мест
            var oldTable = resultsTable.ToList();
            resultsTable.Clear();

            foreach (var stats in sorted)
            {
                var oldRow = oldTable.FirstOrDefault(r => r.TeamName.Equals(stats.TeamName, StringComparison.OrdinalIgnoreCase));
                var newRow = new ResultRow
                {
                    Index = resultsTable.Count + 1,
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

                newRow.Cells = new ObservableCollection<string>();
                for (int j = 0; j < sorted.Count; j++)
                {
                    var opponent = sorted[j].TeamName;
                    if (stats.TeamName == opponent)
                        newRow.Cells.Add("SELF");
                    else if (oldRow != null)
                    {
                        var oldIdx = oldTable.FindIndex(r => r.TeamName.Equals(opponent, StringComparison.OrdinalIgnoreCase));
                        newRow.Cells.Add(oldIdx >= 0 ? oldRow.Cells[oldIdx] : string.Empty);
                    }
                    else
                        newRow.Cells.Add(string.Empty);
                }
                resultsTable.Add(newRow);
            }
        }

        // Вспомогательные методы (перенесены из VM)
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

        private static int CalculateItalianPoints(int won, int lost)
        {
            if (won == 3 && lost <= 1) return 3;
            if (won == 3 && lost == 2) return 2;
            if (won == 2 && lost == 3) return 1;
            return 0;
        }

        private static Dictionary<string, Dictionary<string, int>> BuildHeadToHead(ObservableCollection<Match> schedule)
        {
            var dict = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in schedule.Where(m => string.Equals(m.Status, "Сыгран", StringComparison.OrdinalIgnoreCase)))
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

        private static void ApplyHeadToHeadTieBreaker(List<ResultRow> sorted, Dictionary<string, Dictionary<string, int>> headToHead)
        {
            for (int i = 0; i < sorted.Count; i++)
            {
                var current = sorted[i];
                var equals = sorted.Skip(i + 1)
                    .TakeWhile(t => t.Points == current.Points &&
                                    Math.Abs(t.SetsRatio - current.SetsRatio) < 0.000001 &&
                                    Math.Abs(t.PointsRatio - current.PointsRatio) < 0.000001)
                    .ToList();

                if (equals.Count > 0)
                {
                    var group = new List<ResultRow> { current };
                    group.AddRange(equals);
                    int groupSize = group.Count;

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

                    // Заменяем в исходном списке
                    for (int j = 0; j < group.Count; j++)
                        sorted[i + j] = group[j];
                }
            }
        }

        private static bool IsBetter(ResultRow a, ResultRow b) =>
            a.Points > b.Points ||
            (Math.Abs(a.Points - b.Points) < 0.001 &&
             (a.SetsRatio > b.SetsRatio ||
              (Math.Abs(a.SetsRatio - b.SetsRatio) < 0.000001 &&
               a.PointsRatio > b.PointsRatio)));
    }
}