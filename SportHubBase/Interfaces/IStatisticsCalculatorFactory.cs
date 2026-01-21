using SportHubBase.Services.Statistics;

namespace SportHubBase.Interfaces
{
    public interface IStatisticsCalculatorFactory
    {
        IStatisticsCalculator GetCalculator(string sportType);
    }
}