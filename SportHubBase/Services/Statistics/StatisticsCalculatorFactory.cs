using System;
using System.Collections.Generic;
using System.Linq;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.ViewModels;
using System.Collections.ObjectModel;

namespace SportHubBase.Services.Statistics
{
    public class StatisticsCalculatorFactory : IStatisticsCalculatorFactory
    {
        public IStatisticsCalculator GetCalculator(string sportType)
        {
            if (string.IsNullOrWhiteSpace(sportType))
                return new DefaultStatisticsCalculator();

            switch (sportType.Trim().ToLower())
            {
                case "волейбол":
                    return new VolleyballStatisticsCalculator();
                case "футбол":
                    return new FootballStatisticsCalculator();
                case "баскетбол":
                    return new BasketballStatisticsCalculator();
                default:
                    return new DefaultStatisticsCalculator();
            }
        }
    }

    public class DefaultStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "По умолчанию";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s, IEnumerable<ResultRow> r) =>
            new TournamentStatistics { SportType = t?.SportType ?? "" };
    }

    public class VolleyballStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "Волейбол";
        
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> schedule, IEnumerable<ResultRow> results)
        {
            var stats = new TournamentStatistics { SportType = "Волейбол" };
            if (t == null || schedule == null) return stats;

            // 1. Сводка
            stats.TotalMatches = schedule.Count;
            stats.PlayedMatches = schedule.Count(m => !string.IsNullOrWhiteSpace(m.Status) && 
                (m.Status.Equals("Сыгран", StringComparison.OrdinalIgnoreCase) || 
                 m.Status.Equals("Техническое поражение", StringComparison.OrdinalIgnoreCase)));
            stats.RemainingMatches = stats.TotalMatches - stats.PlayedMatches;
            stats.TechnicalDefeatsCount = schedule.Count(m => !string.IsNullOrWhiteSpace(m.Status) && 
                 m.Status.Equals("Техническое поражение", StringComparison.OrdinalIgnoreCase));

            // Считаем количество пятисетовок и самый результативный матч
            int fiveSetCount = 0;
            int totalPoints = 0;
            int maxMatchPoints = -1;
            Match mostProductiveMatch = null;

            foreach (var match in schedule)
            {
                if (!string.IsNullOrWhiteSpace(match.SetsScore))
                {
                    var setsScore = match.SetsScore.Trim();
                    if (setsScore == "3:2" || setsScore == "2:3")
                    {
                        fiveSetCount++;
                    }
                }
                
                // Считаем общее количество мячей
                if (!string.IsNullOrWhiteSpace(match.TotalScore))
                {
                    var parts = match.TotalScore.Split(':');
                    if (parts.Length == 2 && 
                        int.TryParse(parts[0].Trim(), out int p1) && 
                        int.TryParse(parts[1].Trim(), out int p2))
                    {
                        int currentMatchPoints = p1 + p2;
                        totalPoints += currentMatchPoints;

                        if (currentMatchPoints > maxMatchPoints)
                        {
                            maxMatchPoints = currentMatchPoints;
                            mostProductiveMatch = match;
                        }
                    }
                }
            }
            stats.FiveSetMatches = fiveSetCount;
            stats.AvgGoals = stats.PlayedMatches > 0 ? Math.Round((double)totalPoints / stats.PlayedMatches, 1) : 0;
            
            if (mostProductiveMatch != null)
            {
                stats.MostProductiveMatch = string.Format("{0} — {1} ({2})", 
                    mostProductiveMatch.Team1, mostProductiveMatch.Team2, mostProductiveMatch.TotalScore);
            }

            // 2. Лидер
            if (results != null)
            {
                var leader = results.OrderBy(row => row.Place).FirstOrDefault();
                if (leader != null)
                {
                    stats.LeaderName = leader.TeamName;
                    stats.LeaderPoints = leader.Points;
                    stats.LeaderForm = GetTeamForm(leader.TeamName, schedule);
                }
            }

            // 3. Заполнение ExtraBlocks для динамических карточек
            stats.ExtraBlocks.Clear();
            stats.ExtraBlocks.Add(new StatBlock { Title = "Всего матчей", Value = stats.TotalMatches.ToString(), Color = "#3F51B5" });
            stats.ExtraBlocks.Add(new StatBlock { Title = "Сыграно", Value = stats.PlayedMatches.ToString(), Color = "#4CAF50" });
            stats.ExtraBlocks.Add(new StatBlock { Title = "Осталось", Value = stats.RemainingMatches.ToString(), Color = "#FF9800" });
            stats.ExtraBlocks.Add(new StatBlock { Title = "Пятисетки (3-2)", Value = stats.FiveSetMatches.ToString(), Color = "#E91E63" });
            stats.ExtraBlocks.Add(new StatBlock { Title = "Тех. поражения", Value = stats.TechnicalDefeatsCount.ToString(), Color = "#F44336" });
            stats.ExtraBlocks.Add(new StatBlock { Title = "Лидер", Value = stats.LeaderName, Color = "#9C27B0" });

            // 4. Топ MVP
            var mvpGroups = schedule
                .Where(m => !string.IsNullOrWhiteSpace(m.Mvp) && m.Mvp != "—")
                .GroupBy(m => m.Mvp)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            foreach (var item in mvpGroups)
            {
                // Попробуем найти команду игрока
                string teamName = "Unknown";
                if (t.Teams != null)
                {
                    var team = t.Teams.FirstOrDefault(teamObj => 
                        teamObj.Players != null && 
                        teamObj.Players.Any(p => p.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)));
                    if (team != null) teamName = team.Name;
                }

                stats.TopMvps.Add(new MvpItem 
                { 
                    PlayerName = item.Name, 
                    TeamName = teamName, 
                    Count = item.Count 
                });
            }

            // 4. Последние матчи - показываем команды с их формой
            // Получаем все команды, участвовавшие в турнире
            var allTeams = new HashSet<string>();
            foreach (var match in schedule)
            {
                if (!string.IsNullOrWhiteSpace(match.Team1)) allTeams.Add(match.Team1);
                if (!string.IsNullOrWhiteSpace(match.Team2)) allTeams.Add(match.Team2);
            }

            // Для каждой команды вычисляем форму
            foreach (var teamName in allTeams.OrderBy(t1 => t1).Take(8))
            {
                var form = GetTeamForm(teamName, schedule);
                stats.LastMatches.Add(new TeamMatchHistory
                {
                    TeamName = teamName,
                    Form = form
                });
            }

            return stats;
        }

        private string GetTeamForm(string teamName, IEnumerable<Match> schedule)
        {
            var playedMatches = schedule
                .Where(m => !string.IsNullOrWhiteSpace(m.Status) && 
                            (m.Status.Equals("Сыгран", StringComparison.OrdinalIgnoreCase) || 
                             m.Status.Equals("Техническое поражение", StringComparison.OrdinalIgnoreCase)) &&
                            (m.Team1 == teamName || m.Team2 == teamName))
                .OrderByDescending(m => m.MatchNumber.HasValue ? m.MatchNumber.Value : 0) // Последние матчи первыми
                .Take(5)
                .Reverse(); // Чтобы выводить слева направо (от старого к новому) или наоборот? 
                // Обычно "Form" пишут L W W D W (справа - последний).
                // User asked: "W L W W D" -> давайте сделаем слева направо: самый давний -> самый последний из 5.

            var form = new List<string>();

            foreach (var m in playedMatches)
            {
                bool isTeam1 = m.Team1 == teamName;
                // Определяем победителя. Для волейбола ничьих нет, но код должен быть устойчив.
                // Смотрим SetsScore "3:1"
                // Или используем логику из ViewModel (GetOutcome)
                
                // Простая логика парсинга SetsScore
                int s1 = 0, s2 = 0;
                if (!string.IsNullOrWhiteSpace(m.SetsScore))
                {
                    var parts = m.SetsScore.Split(':');
                    if (parts.Length == 2)
                    {
                        int.TryParse(parts[0], out s1);
                        int.TryParse(parts[1], out s2);
                    }
                }

                if (isTeam1)
                {
                    if (s1 > s2) form.Add("W");
                    else if (s1 < s2) form.Add("L");
                    else form.Add("D");
                }
                else
                {
                    if (s2 > s1) form.Add("W");
                    else if (s2 < s1) form.Add("L");
                    else form.Add("D");
                }
            }

            return string.Join(" ", form);
        }
    }

    public class FootballStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "Футбол";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s, IEnumerable<ResultRow> r) =>
            new TournamentStatistics { SportType = "Футбол" };
    }

    public class BasketballStatisticsCalculator : IStatisticsCalculator
    {
        public string Name => "Баскетбол";
        public TournamentStatistics Calculate(Tournament t, ObservableCollection<Match> s, IEnumerable<ResultRow> r) =>
            new TournamentStatistics { SportType = "Баскетбол" };
    }
}