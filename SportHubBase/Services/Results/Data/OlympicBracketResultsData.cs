// Services/Results/Data/OlympicBracketResultsData.cs
using SportHubBase.Models;

namespace SportHubBase.Services.Results.Data
{
    public class OlympicBracketResultsData : ResultsData
    {
        // Пока пусто, будет сетка плей-офф
        public TournamentBracket Bracket { get; set; }
    }
}
