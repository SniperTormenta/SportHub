using SportHubBase.Interfaces;
using System;
using System.Collections.Generic;

namespace SportHubBase.Services.Scoring
{
    public class SportScoreStrategyFactory : ISportScoreStrategyFactory
    {
        private readonly Dictionary<string, ISportScoreStrategy> _strategies = new Dictionary<string, ISportScoreStrategy>(StringComparer.OrdinalIgnoreCase);
        private readonly ISportScoreStrategy _defaultStrategy;

        public SportScoreStrategyFactory(IEnumerable<ISportScoreStrategy> strategies)
        {
            foreach (var strategy in strategies)
            {
                if (strategy.SportType == "Other")
                    _defaultStrategy = strategy;
                else
                    _strategies[strategy.SportType] = strategy;
            }

            if (_defaultStrategy == null)
            {
                _defaultStrategy = new DefaultScoreStrategy();
            }
        }

        public ISportScoreStrategy GetStrategy(string sportType)
        {
            if (string.IsNullOrEmpty(sportType))
                return _defaultStrategy;

            return _strategies.TryGetValue(sportType, out var strategy) ? strategy : _defaultStrategy;
        }
    }
}
