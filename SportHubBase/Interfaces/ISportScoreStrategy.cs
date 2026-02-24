using SportHubBase.Models;

namespace SportHubBase.Interfaces
{
    public interface ISportScoreStrategy
    {
        string SportType { get; }
        MatchEditContext CreateEditContext(Match match);
        void FinalizeMatch(Match match, MatchEditContext context);
    }
}
