// Services/Result/Scoring/CustomScoringStrategy.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;

namespace SportHubBase.Services.Results.Scoring
{
    public class CustomScoringStrategy : IScoringStrategy
    {
        private readonly int _winPoints;
        private readonly int _drawPoints;
        private readonly int _lossPoints;

        public string Id => "Custom";
        public string Name => "Пользовательская";
        public string Description => $"Победа: {_winPoints}, Ничья: {_drawPoints}, Поражение: {_lossPoints}";

        public CustomScoringStrategy(int win, int draw, int loss)
        {
            _winPoints = win;
            _drawPoints = draw;
            _lossPoints = loss;
        }

        public int CalculatePoints(int wonSets, int lostSets)
        {
            if (wonSets > lostSets) return _winPoints;
            if (wonSets == lostSets) return _drawPoints;
            return _lossPoints;
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
