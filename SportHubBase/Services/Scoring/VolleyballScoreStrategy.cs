using System;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.ViewModels;

namespace SportHubBase.Services.Scoring
{
    public class VolleyballScoreStrategy : ISportScoreStrategy
    {
        public string SportType => "Волейбол";

        public MatchEditContext CreateEditContext(Match match)
        {
            return new SetScoreEditContext(match);
        }

        public void FinalizeMatch(Match match, MatchEditContext context)
        {
            // Здесь специфичная логика завершения волейбольного матча.
        }
    }
}
