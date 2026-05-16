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
            if (tournament == null)
                return new ItalianScoringStrategy();

            string sport = tournament.SportType;
            string system = tournament.ScoringSystem;

            if (string.Equals(system, "Пользовательская", StringComparison.OrdinalIgnoreCase))
                return new CustomScoringStrategy(tournament.CustomWinPoints, tournament.CustomDrawPoints, tournament.CustomLossPoints, tournament.TieBreakerRule);

            if (string.Equals(sport, "Футбол", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(system, "Баскетбол", StringComparison.OrdinalIgnoreCase)) // На всякий случай
                    return new BasketballScoringStrategy();
                return new FootballScoringStrategy();
            }

            if (string.Equals(sport, "Баскетбол", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(system, "Футбол", StringComparison.OrdinalIgnoreCase))
                    return new FootballScoringStrategy();
                return new BasketballScoringStrategy();
            }

            // Волейбол или по умолчанию
            if (string.Equals(system, "FIVB", StringComparison.OrdinalIgnoreCase))
                return new FivbScoringStrategy();

            return new ItalianScoringStrategy();
        }
    }
}
