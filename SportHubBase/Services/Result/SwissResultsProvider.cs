using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services.Results.Data;
using SportHubBase.Services.Scheduling.Swiss;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportHubBase.Services.Result
{
    public class SwissResultsProvider : IResultsProvider
    {
        private readonly ITiebreakerCalculator _tiebreakerCalculator;
        private readonly IPlayedMatchesService _playedMatchesService;

        public string Name => "Швейцарская система";

        public SwissResultsProvider(ITiebreakerCalculator tiebreakerCalculator, IPlayedMatchesService playedMatchesService)
        {
            _tiebreakerCalculator = tiebreakerCalculator;
            _playedMatchesService = playedMatchesService;
        }

        public ResultsData ComputeResults(Tournament tournament, ObservableCollection<Match> schedule)
        {
            var data = new SwissResultsData();
            
            if (tournament == null || tournament.Teams == null || !tournament.Teams.Any())
                return data;

            int totalRounds = tournament.TotalRounds > 0 ? tournament.TotalRounds : 7;
            for (int i = 1; i <= totalRounds; i++)
            {
                data.HeaderNumbers.Add(i);
            }

            // Используем калькулятор для получения ранжированного списка
            var matchesList = schedule?.ToList() ?? new System.Collections.Generic.List<Match>();
            var playedGraph = _playedMatchesService.BuildPlayedOpponentsGraph(tournament.Teams, matchesList);
            var byeList = _playedMatchesService.BuildByeCountsList(tournament.Teams, matchesList);
            var sortedProgress = _tiebreakerCalculator.CalculateAndSort(tournament, tournament.Teams.ToList(), playedGraph, byeList);

            int place = 1;
            foreach (var progress in sortedProgress)
            {
                var myMatches = matchesList.Where(m => 
                    (m.Team1 == progress.Team.Name || m.Team2 == progress.Team.Name) && 
                    !m.IsBye && m.Status == "Сыгран").ToList();

                var row = new SwissResultRow
                {
                    Place = place++,
                    TeamName = progress.Team.Name,
                    MatchesPlayed = myMatches.Count,
                    Wins = progress.Wins,
                    Losses = myMatches.Count(m => {
                        var score = ParsePair(m.SetsScore) ?? ParsePair(m.Team1QuickScore, m.Team2QuickScore);
                        if (!score.HasValue) return false;
                        return (m.Team1 == progress.Team.Name && score.Value.left < score.Value.right) || 
                               (m.Team2 == progress.Team.Name && score.Value.right < score.Value.left);
                    }),
                    Draws = myMatches.Count(m => {
                        var score = ParsePair(m.SetsScore) ?? ParsePair(m.Team1QuickScore, m.Team2QuickScore);
                        if (!score.HasValue) return false;
                        return score.Value.left == score.Value.right;
                    }),
                    Byes = progress.ByeCount,
                    Points = progress.Points,
                    BuchholzCut1 = progress.BuchholzCut1
                };

                // Заполняем итоги по турам
                for (int r = 1; r <= totalRounds; r++)
                {
                    var roundMatch = matchesList.FirstOrDefault(m => m.Round == r && (m.Team1 == progress.Team.Name || m.Team2 == progress.Team.Name));
                    if (roundMatch == null)
                    {
                        row.RoundResults.Add("-");
                    }
                    else if (roundMatch.IsBye)
                    {
                        row.RoundResults.Add("BYE");
                    }
                    else if (roundMatch.Status != "Сыгран" && roundMatch.Status != "Техническое поражение")
                    {
                        row.RoundResults.Add("-");
                    }
                    else
                    {
                        var score = ParsePair(roundMatch.SetsScore) ?? ParsePair(roundMatch.Team1QuickScore, roundMatch.Team2QuickScore);
                        if (!score.HasValue)
                        {
                            row.RoundResults.Add("-");
                        }
                        else
                        {
                            bool isTeam1 = roundMatch.Team1 == progress.Team.Name;
                            int myScore = isTeam1 ? score.Value.left : score.Value.right;
                            int oppScore = isTeam1 ? score.Value.right : score.Value.left;

                            if (myScore > oppScore) row.RoundResults.Add("1");
                            else if (myScore < oppScore) row.RoundResults.Add("0");
                            else row.RoundResults.Add("0.5");
                        }
                    }
                }

                data.Rows.Add(row);
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
    }
}
