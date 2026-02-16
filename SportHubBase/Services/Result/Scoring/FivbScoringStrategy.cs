// Services/Result/Scoring/FivbScoringStrategy.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;

namespace SportHubBase.Services.Results.Scoring
{
    public class FivbScoringStrategy : IScoringStrategy
    {
        public string Id => "FIVB";
        public string Name => "Система FIVB";
        public string Description => "3 очка: 3:0, 3:1\n2 очка: 3:2\n1 очко: 2:3\n0 очков: 0:3, 1:3\nПри равенстве очков: 1. Кол-во побед, 2. Коэф. сетов, 3. Коэф. мячей";

        public int CalculatePoints(int wonSets, int lostSets)
        {
            if (wonSets == 3 && lostSets <= 1) return 3;
            if (wonSets == 3 && lostSets == 2) return 2;
            if (wonSets == 2 && lostSets == 3) return 1;
            return 0;
        }

        public int Compare(ResultRow a, ResultRow b)
        {
            // 1. Очки
            if (a.Points != b.Points) return b.Points.CompareTo(a.Points);
            // 2. Количество побед (Различие с итальянской)
            if (a.Wins != b.Wins) return b.Wins.CompareTo(a.Wins);
            // 3. Коэф. сетов
            if (Math.Abs(a.SetsRatio - b.SetsRatio) > 0.000001) return b.SetsRatio.CompareTo(a.SetsRatio);
            // 4. Коэф. мячей
            if (Math.Abs(a.PointsRatio - b.PointsRatio) > 0.000001) return b.PointsRatio.CompareTo(a.PointsRatio);
            
            return 0;
        }
    }
}
