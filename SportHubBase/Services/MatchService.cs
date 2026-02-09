using SportHubBase.Interfaces;
using SportHubBase.Models;
using System.Collections.Generic;
using System.Linq;

namespace SportHubBase.Services
{
    public class MatchService : IMatchService
    {
        public void AssignAutoNumbers(List<Match> matches)
        {
            if (matches == null || !matches.Any()) return;

            int maxNumber = matches.Max(m => m.MatchNumber) ?? 0;

            foreach (var match in matches)
            {
                if (match.MatchNumber == null)
                {
                    maxNumber++;
                    match.MatchNumber = maxNumber;
                }
            }
        }

        public bool ValidateUniqueNumber(Match match, List<Match> allMatches)
        {
            if (match == null || match.MatchNumber == null) return true; // Null is allowed (will be auto-assigned later) or valid

            // Check if any *other* match has the same number
            return !allMatches.Any(m => m.Id != match.Id && m.MatchNumber == match.MatchNumber);
        }
    }
}
