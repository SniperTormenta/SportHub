// Services/Result/Scoring/BasketballScoringStrategy.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;

namespace SportHubBase.Services.Results.Scoring
{
    public class BasketballScoringStrategy : IScoringStrategy
    {
        public string Id => "Basketball";
        public string Name => "Баскетбол (стандарт)";
        public string Description => "Победа: 2 очка, Поражение: 1 очко.\nПри равенстве очков: 1. Личные встречи, 2. Разница очков, 3. Набранные очки";

        public int CalculatePoints(int wonSets, int lostSets)
        {
            if (wonSets > lostSets) return 2;
            return 1; // За поражение 1 очко
        }

        public int Compare(ResultRow a, ResultRow b)
        {
            // 1. Очки
            if (a.Points != b.Points) return b.Points.CompareTo(a.Points);
            
            // 2. Разница очков (используем SetsWon/SetsLost)
            int diffA = a.SetsWon - a.SetsLost;
            int diffB = b.SetsWon - b.SetsLost;
            if (diffA != diffB) return diffB.CompareTo(diffA);

            // 3. Набранные очки
            if (a.SetsWon != b.SetsWon) return b.SetsWon.CompareTo(a.SetsWon);
            
            return 0;
        }
    }
}
