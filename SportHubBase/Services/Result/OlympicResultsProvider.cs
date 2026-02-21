using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services.Results.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportHubBase.Services.Result
{
    public class OlympicResultsProvider : IResultsProvider
    {
        public string Name => "Olympic Bracket Provider";

        public ResultsData ComputeResults(Tournament tournament, ObservableCollection<Match> schedule)
        {
            return new OlympicBracketResultsData
            {
                StatusMessage = "Олимпийская сетка (плей-офф)",
                LastUpdate = DateTime.Now,
                Bracket = tournament.Bracket ?? new TournamentBracket() // на всякий
            };
        }
    }
}
