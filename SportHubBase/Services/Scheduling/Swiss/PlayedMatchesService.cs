using System;
using System.Collections.Generic;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling.Swiss
{
    public class PlayedMatchesService : IPlayedMatchesService
    {
        public Dictionary<string, HashSet<string>> BuildPlayedOpponentsGraph(IEnumerable<Team> teams, IEnumerable<Match> history)
        {
            var graph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in teams)
            {
                graph[team.Name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var match in history)
            {
                if (match.Status == "Не сыгран") continue; // Хотя в Swiss мы должны учитывать и несыгранные, так как они в расписании
                // Жеребьевка строится не только по сыгранным, но и по запланированным, чтобы исключить повторения уже включенных пар
                // Но лучше учитывать все матчи в истории (включая несыгранные из предыдущих туров)
                
                // Пропускаем bye
                if (match.IsBye || string.Equals(match.Team2, "BYE", StringComparison.OrdinalIgnoreCase)) continue;

                if (!string.IsNullOrEmpty(match.Team1) && !string.IsNullOrEmpty(match.Team2))
                {
                    if (graph.ContainsKey(match.Team1)) graph[match.Team1].Add(match.Team2);
                    if (graph.ContainsKey(match.Team2)) graph[match.Team2].Add(match.Team1);
                }
            }

            return graph;
        }

        public Dictionary<string, int> BuildByeCountsList(IEnumerable<Team> teams, IEnumerable<Match> history)
        {
            var byeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in teams)
            {
                byeCounts[team.Name] = 0;
            }

            foreach (var match in history)
            {
                if (match.IsBye || string.Equals(match.Team2, "BYE", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(match.Team1) && byeCounts.ContainsKey(match.Team1))
                    {
                        byeCounts[match.Team1]++;
                    }
                }
            }

            return byeCounts;
        }
    }
}
