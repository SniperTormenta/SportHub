using System.Collections.Generic;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling.Swiss
{
    public interface IPlayedMatchesService
    {
        /// <summary>
        /// Получает всех уникальных соперников для пула команд на основе истории.
        /// Возвращает словарь [НазваниеКоманды] -> [Список сыгранных соперников (Имена)]
        /// </summary>
        Dictionary<string, HashSet<string>> BuildPlayedOpponentsGraph(IEnumerable<Team> teams, IEnumerable<Match> history);

        /// <summary>
        /// Возвращает количество Bye для каждой команды
        /// </summary>
        Dictionary<string, int> BuildByeCountsList(IEnumerable<Team> teams, IEnumerable<Match> history);
    }
}
