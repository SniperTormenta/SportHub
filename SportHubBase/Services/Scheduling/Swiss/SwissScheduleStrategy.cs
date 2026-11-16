using System;
using System.Collections.Generic;
using System.Linq;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling.Swiss
{
    public class SwissScheduleStrategy : ISequentialScheduleStrategy
    {
        public string Name => "Швейцарская система";
        public bool IsImplemented => true;
        private readonly ITiebreakerCalculator _tiebreakerCalculator;
        private readonly IPlayedMatchesService _playedMatchesService;

        public SwissScheduleStrategy(ITiebreakerCalculator tiebreakerCalculator, IPlayedMatchesService playedMatchesService)
        {
            _tiebreakerCalculator = tiebreakerCalculator;
            _playedMatchesService = playedMatchesService;
        }

        public IEnumerable<Match> GenerateSchedule(IList<Team> teams) => GenerateNextRound(new Tournament { Teams = teams.ToList() }, 1);
        public TournamentBracket GenerateBracket(IList<Team> teams) => throw new NotSupportedException("Швейцарская система не использует сетку плей-офф.");

        public IEnumerable<Match> GenerateNextRound(Tournament tournament, int roundNumber)
        {
            if (roundNumber < 1) throw new ArgumentException("Номер тура должен быть положительным.");
            TournamentIntegrityService.Normalize(tournament);
            var teams = tournament.Teams.Where(t => !t.IsBye).ToList();
            if (teams.Count < 2) return new List<Match>();
            var history = (tournament.Matches ?? new List<Match>()).Where(m => m.Round < roundNumber).ToList();
            if (history.Any(m => !m.IsBye && !IsPlayed(m)))
                throw new InvalidOperationException("Завершите матчи предыдущих туров перед жеребьёвкой.");
            var graph = _playedMatchesService.BuildPlayedOpponentsGraph(teams, history);
            var byes = _playedMatchesService.BuildByeCountsList(teams, history);
            // Расчёт рейтинга не должен учитывать результаты будущих туров.
            var snapshot = new Tournament { Teams = teams, Matches = history, SportType = tournament.SportType,
                ScoringSystem = tournament.ScoringSystem, CustomWinPoints = tournament.CustomWinPoints,
                CustomDrawPoints = tournament.CustomDrawPoints, CustomLossPoints = tournament.CustomLossPoints };
            var ranked = _tiebreakerCalculator.CalculateAndSort(snapshot, teams, graph, byes);
            if (roundNumber == 1) ranked = ranked.OrderBy(p => p.InitialSeed).ThenBy(p => p.Team.Id).ToList();
            var byeCandidates = teams.Count % 2 == 0
                ? new List<SwissTeamProgress> { null }
                : ranked.Where(p => p.ByeCount == ranked.Min(x => x.ByeCount)).Reverse().ToList();
            foreach (var bye in byeCandidates)
            {
                var remaining = ranked.Where(p => p != bye).ToList();
                var matches = new List<Match>();
                int budget = 250000;
                if (!Pair(remaining, graph, roundNumber, matches, ref budget)) continue;
                if (bye != null) matches.Add(new Match { Round = roundNumber, RoundName = "Тур " + roundNumber,
                    Team1Id = bye.Team.Id, Team1 = bye.Name, Team2 = "BYE", IsBye = true,
                    WinnerId = bye.Team.Id, Status = "Сыгран", Team1QuickScore = "1", Team2QuickScore = "0" });
                return matches;
            }
            throw new InvalidOperationException("Не удалось построить тур без повторных встреч в пределах лимита поиска. Измените число туров или состав участников; некорректные пары не созданы.");
        }

        private static bool IsPlayed(Match match) => string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase)
            || string.Equals(match.Status, "Техническое поражение", StringComparison.OrdinalIgnoreCase);

        private static bool Pair(List<SwissTeamProgress> remaining, Dictionary<string, HashSet<string>> graph,
            int round, List<Match> matches, ref int budget)
        {
            if (remaining.Count == 0) return true;
            if (--budget <= 0) return false;
            var first = remaining[0];
            foreach (var opponent in remaining.Skip(1).Where(p => !graph[first.Name].Contains(p.Name))
                .OrderBy(p => Math.Abs(p.Points - first.Points)).ToList())
            {
                var match = new Match { Round = round, RoundName = "Тур " + round, Team1Id = first.Team.Id,
                    Team2Id = opponent.Team.Id, Team1 = first.Name, Team2 = opponent.Name };
                matches.Add(match);
                if (Pair(remaining.Where(p => p != first && p != opponent).ToList(), graph, round, matches, ref budget)) return true;
                matches.RemoveAt(matches.Count - 1);
            }
            return false;
        }
    }
}