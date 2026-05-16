// Services/Result/Scoring/FootballScoringStrategy.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;

namespace SportHubBase.Services.Results.Scoring
{
    public class FootballScoringStrategy : IScoringStrategy
    {
        public string Id => "Football";
        public string Name => "Футбол (стандарт)";
        public string Description => "Победа: 3 очка, Ничья: 1 очко, Поражение: 0 очков.\nПри равенстве очков: 1. Разница мячей, 2. Забитые мячи, 3. Личные встречи";

        public int CalculatePoints(int wonSets, int lostSets)
        {
            if (wonSets > lostSets) return 3;
            if (wonSets == lostSets) return 1;
            return 0;
        }

        public int Compare(ResultRow a, ResultRow b)
        {
            // 1. Очки
            if (a.Points != b.Points) return b.Points.CompareTo(a.Points);
            
            // 2. Разница мячей (используем SetsWon/SetsLost, так как туда попадает основной счет)
            int diffA = a.SetsWon - a.SetsLost;
            int diffB = b.SetsWon - b.SetsLost;
            if (diffA != diffB) return diffB.CompareTo(diffA);

            // 3. Забитые мячи
            if (a.SetsWon != b.SetsWon) return b.SetsWon.CompareTo(a.SetsWon);
            
            return 0;
        }
    }
}
