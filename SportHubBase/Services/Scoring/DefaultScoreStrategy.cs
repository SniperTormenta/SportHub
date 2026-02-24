using System;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.ViewModels;

namespace SportHubBase.Services.Scoring
{
    public class DefaultScoreStrategy : ISportScoreStrategy
    {
        public string SportType => "Other";

        public MatchEditContext CreateEditContext(Match match)
        {
            return new SimpleScoreEditContext(match);
        }

        public void FinalizeMatch(Match match, MatchEditContext context)
        {
            // Здесь можно добавить специфичную логику завершения матча для DefaultScoreStrategy.
            // Вся общая логика применения изменений (ApplyChanges) уже вызвана ViewModel'ю.
        }
    }
}
