using SportHubBase.Models;

namespace SportHubBase.Interfaces
{
    public interface ISportScoreStrategyFactory
    {
        ISportScoreStrategy GetStrategy(string sportType);
    }
}
