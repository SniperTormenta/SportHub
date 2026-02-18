using SportHubBase.Models;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Фабрика для получения провайдера результатов в зависимости от типа турнира.
    /// </summary>
    public interface IResultsProviderFactory
    {
        IResultsProvider GetProvider(Tournament tournament);
    }
}
