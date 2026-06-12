using System.Collections.ObjectModel;

namespace SportHubBase.Services.Results.Data
{
    public class SwissResultRow
    {
        public int Place { get; set; }
        public string TeamName { get; set; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int Byes { get; set; }
        public double Points { get; set; }
        public double BuchholzCut1 { get; set; }
        
        // Список результатов по турам (например, "1", "0.5", "0", "BYE")
        public ObservableCollection<string> RoundResults { get; set; } = new ObservableCollection<string>();
    }
}
