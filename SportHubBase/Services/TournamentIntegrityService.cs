using SportHubBase.Models;
using System;
using System.Linq;

namespace SportHubBase.Services
{
    public static class TournamentIntegrityService
    {
        public static void Normalize(Tournament tournament, System.Collections.Generic.IEnumerable<Match> matches = null)
        {
            if (tournament == null) throw new ArgumentNullException(nameof(tournament));
            var teams = tournament.Teams ?? throw new ArgumentException("Не задан список команд.");
            if (teams.Any(t => t.Id == Guid.Empty) || teams.GroupBy(t => t.Id).Any(g => g.Count() > 1))
                throw new ArgumentException("Идентификаторы команд должны быть непустыми и уникальными.");
            if (teams.Where(t => !t.IsBye).GroupBy(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                throw new ArgumentException("Названия команд должны быть уникальными.");
            foreach (var match in matches ?? tournament.Matches ?? Enumerable.Empty<Match>())
            {
                var left = Resolve(teams, match.Team1Id, match.Team1);
                var right = Resolve(teams, match.Team2Id, match.Team2);
                if (left != null) { match.Team1Id = left.Id; match.Team1 = left.Name; }
                if (right != null) { match.Team2Id = right.Id; match.Team2 = right.Name; }
                if (left != null && right != null && left.Id == right.Id)
                    throw new ArgumentException("Команда не может играть сама с собой.");
            }
            if (tournament.Bracket != null)
            {
                var bracketMatches = tournament.Bracket.Rounds.SelectMany(r => r.Matches).ToList();
                if (tournament.Bracket.BronzeMatch != null) bracketMatches.Add(tournament.Bracket.BronzeMatch);
                foreach (var match in bracketMatches)
                {
                    var left = match.Team1 == null ? null : Resolve(teams, match.Team1.Id, match.Team1.Name);
                    var right = match.Team2 == null ? null : Resolve(teams, match.Team2.Id, match.Team2.Name);
                    if (left != null && !ReferenceEquals(left, match.Team1)) match.Team1 = left;
                    if (right != null && !ReferenceEquals(right, match.Team2)) match.Team2 = right;
                }
            }
        }

        private static Team Resolve(System.Collections.Generic.IEnumerable<Team> teams, Guid? id, string name)
        {
            // Идентификатор имеет приоритет: переименование не разрывает связи со старыми матчами.
            return id.HasValue ? teams.FirstOrDefault(t => t.Id == id.Value)
                : teams.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
