// Services/Result/Scoring/ItalianScoringStrategy.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;

namespace SportHubBase.Services.Results.Scoring
{
    public class ItalianScoringStrategy : IScoringStrategy
    {
        public string Id => "Italian";
        public string Name => "Итальянская система";
        public string Description => "3 очка: победа 3:0 или 3:1\n2 очка: победа 3:2\n1 очко: поражение 2:3\n0 очков: поражение 0:3 или 1:3";

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
            // 2. Коэф. сетов
            if (Math.Abs(a.SetsRatio - b.SetsRatio) > 0.000001) return b.SetsRatio.CompareTo(a.SetsRatio);
            // 3. Коэф. мячей
            if (Math.Abs(a.PointsRatio - b.PointsRatio) > 0.000001) return b.PointsRatio.CompareTo(a.PointsRatio);
            
            return 0;
        }
    }
}
