using SportHubBase.Models;
using System.Collections.Generic;

namespace SportHubBase.Interfaces
{
    public interface IMatchService
    {
        void AssignAutoNumbers(List<Match> matches);
        bool ValidateUniqueNumber(Match match, List<Match> allMatches);
    }
}
