// Services/Result/ScoringStrategyFactory.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services.Results.Scoring;
using System;

namespace SportHubBase.Services.Results
{
    public static class ScoringStrategyFactory
    {
        public static IScoringStrategy GetStrategy(Tournament tournament)
        {
            if (tournament == null || string.IsNullOrEmpty(tournament.ScoringSystem))
                return new ItalianScoringStrategy(); // По умолчанию

            switch (tournament.ScoringSystem)
            {
                case "FIVB":
                    return new FivbScoringStrategy();
                case "Пользовательская":
                    return new CustomScoringStrategy(
                        tournament.CustomWinPoints, 
                        tournament.CustomDrawPoints, 
                        tournament.CustomLossPoints);
                case "Итальянская":
                default:
                    return new ItalianScoringStrategy();
            }
        }
    }
}
