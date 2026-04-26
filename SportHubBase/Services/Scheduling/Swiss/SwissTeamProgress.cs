using System.Collections.Generic;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling.Swiss
{
    /// <summary>
    /// DTO-сущность для отслеживания прогресса команды в швейцарской системе.
    /// Используется для сортировки и жадного парования в рамках score-groups.
    /// </summary>
    public class SwissTeamProgress
    {
        public Team Team { get; set; }

        public string Name => Team.Name;
        
        // --- Критерии тай-брейка ---
        public double Points { get; set; }
        public double BuchholzCut1 { get; set; }
        public int Wins { get; set; }
        public int InitialSeed => Team.InitialSeed;

        // --- Информация для парования ---
        public int ByeCount { get; set; }
        public HashSet<string> PlayedOpponents { get; set; } = new HashSet<string>();

        public SwissTeamProgress(Team team)
        {
            Team = team;
        }

        public override string ToString() => $"{Name} (P:{Points}, B:{BuchholzCut1}, W:{Wins})";
    }
}
