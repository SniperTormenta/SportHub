using System;
using System.Collections.Generic;
using System.Linq;
using SportHubBase.Models;
using SportHubBase.Services.Results;

namespace SportHubBase.Services.Scheduling.Swiss
{
    public interface ITiebreakerCalculator
    {
        List<SwissTeamProgress> CalculateAndSort(Tournament tournament, IEnumerable<Team> teams, Dictionary<string, HashSet<string>> playedGraph, Dictionary<string, int> byeList);
    }
}
