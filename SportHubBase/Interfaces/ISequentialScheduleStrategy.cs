using System.Collections.Generic;
using SportHubBase.Models;
using SportHubBase.Services.Scheduling;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Расширенный интерфейс стратегии генерации расписания, поддерживающий последовательную генерацию по турам.
    /// Используется для систем, где расписание следующего тура зависит от результатов предыдущих (Швейцарская система).
    /// </summary>
    public interface ISequentialScheduleStrategy : IScheduleStrategy
    {
        IEnumerable<Match> GenerateNextRound(Tournament tournament, int roundNumber);
    }
}
