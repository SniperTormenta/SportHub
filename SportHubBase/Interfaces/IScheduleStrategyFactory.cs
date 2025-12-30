// Interfaces/IScheduleStrategyFactory.cs
using SportHubBase.Services.Scheduling;

namespace SportHubBase.Interfaces
{
    public interface IScheduleStrategyFactory
    {
        IScheduleStrategy GetStrategy(string tournamentType);
    }
}