using System;
using System.Globalization;

namespace SportHubBase.Models
{
    /// <summary>Общее числовое представление счёта. Строки остаются форматом ввода и совместимости.</summary>
    public sealed class ScorePair
    {
        public int Left { get; }
        public int Right { get; }
        public ScorePair(int left, int right)
        {
            if (left < 0 || right < 0) throw new ArgumentException("Счёт не может быть отрицательным.");
            Left = left;
            Right = right;
        }
        public static ScorePair Parse(string value)
        {
            var parts = value?.Split(':');
            return parts?.Length == 2 ? Parse(parts[0], parts[1]) : null;
        }
        public static ScorePair Parse(string left, string right)
        {
            int l, r;
            return int.TryParse(left?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out l) &&
                int.TryParse(right?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out r)
                ? new ScorePair(l, r) : null;
        }
        public static ScorePair ForMatch(Match match) => Parse(match.SetsScore) ?? Parse(match.Team1QuickScore, match.Team2QuickScore);
        public override string ToString() => Left.ToString(CultureInfo.InvariantCulture) + ":" + Right.ToString(CultureInfo.InvariantCulture);
    }
}
