// Interfaces/IResultsCalculatorFactory.cs
using SportHubBase.Models;
using SportHubBase.Services.Results;

namespace SportHubBase.Interfaces
{
    public interface IResultsCalculatorFactory
    {
        IResultsCalculator GetCalculator(Tournament tournament);
    }
}