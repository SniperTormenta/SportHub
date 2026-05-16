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
        private readonly string _tieBreakerRule;

        public string Id => "Custom";
        public string Name => "Пользовательская";
        public string Description => $"Победа: {_winPoints}, Ничья: {_drawPoints}, Поражение: {_lossPoints}. Правило: {_tieBreakerRule ?? "Стандарт"}";

        public CustomScoringStrategy(int win, int draw, int loss, string tieBreakerRule = "Стандарт")
        {
            _winPoints = win;
            _drawPoints = draw;
            _lossPoints = loss;
            _tieBreakerRule = tieBreakerRule;
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

            if (string.Equals(_tieBreakerRule, "Футбол", StringComparison.OrdinalIgnoreCase))
            {
                // 2. Разница мячей (SetsWon/SetsLost)
                int diffA = a.SetsWon - a.SetsLost;
                int diffB = b.SetsWon - b.SetsLost;
                if (diffA != diffB) return diffB.CompareTo(diffA);

                // 3. Забитые мячи
                if (a.SetsWon != b.SetsWon) return b.SetsWon.CompareTo(a.SetsWon);
            }
            else if (string.Equals(_tieBreakerRule, "Баскетбол", StringComparison.OrdinalIgnoreCase))
            {
                // 2. Разница очков (SetsWon/SetsLost)
                int diffA = a.SetsWon - a.SetsLost;
                int diffB = b.SetsWon - b.SetsLost;
                if (diffA != diffB) return diffB.CompareTo(diffA);

                // 3. Забитые очки
                if (a.SetsWon != b.SetsWon) return b.SetsWon.CompareTo(a.SetsWon);
            }
            else
            {
                // Стандарт (Волейбол)
                // 2. Коэф. сетов
                if (Math.Abs(a.SetsRatio - b.SetsRatio) > 0.000001) return b.SetsRatio.CompareTo(a.SetsRatio);
                // 3. Коэф. мячей
                if (Math.Abs(a.PointsRatio - b.PointsRatio) > 0.000001) return b.PointsRatio.CompareTo(a.PointsRatio);
            }
            
            return 0;
        }
    }
}
