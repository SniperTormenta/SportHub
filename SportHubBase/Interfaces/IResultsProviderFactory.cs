// Interfaces/IResultsProviderFactory.cs
using SportHubBase.Models;

namespace SportHubBase.Interfaces
{
    public interface IResultsProviderFactory
    {
        IResultsProvider GetProvider(Tournament tournament);
    }
}
