using System.Collections.Generic;
using SportHubBase.Models;

namespace SportHubBase.Interfaces
{
    public interface IExcelService
    {
        void ExportResults(string tournamentName, IEnumerable<string> headers, IEnumerable<ResultRow> results, string filePath);
        void ExportTeams(IEnumerable<Team> teams, string filePath);
        void SaveTemplate(string filePath);
        List<Team> ImportTeams(string filePath);
        void SavePlayersTemplate(string filePath);
        List<Player> ImportPlayers(string filePath);
    }
}
