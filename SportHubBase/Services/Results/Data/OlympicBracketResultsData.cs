// Services/Results/Data/OlympicBracketResultsData.cs
using SportHubBase.Models;
using System.Collections.Generic;
using System.Linq;

namespace SportHubBase.Services.Results.Data
{
    public class OlympicBracketResultsData : ResultsData
    {
        // ───── константы разметки сетки ─────
        private const double CardWidth  = 220;
        private const double CardHeight = 90;
        private const double ColGap     = 80;   // горизонтальный зазор между раундами
        private const double RowGap     = 30;   // вертикальный зазор между матчами

        private TournamentBracket _bracket;

        public TournamentBracket Bracket
        {
            get { return _bracket; }
            set
            {
                _bracket = value;
                RebuildLayout();
            }
        }

        /// <summary>
        /// Плоский список матчей с уже выставленными XPosition / YPosition для Canvas.
        /// </summary>
        public List<BracketMatch> AllMatches { get; private set; } = new List<BracketMatch>();

        /// <summary>
        /// Соединительные линии между матчами.
        /// </summary>
        public List<BracketConnection> Connections { get; private set; } = new List<BracketConnection>();

        /// <summary>
        /// Пересчитывает координаты всех карточек и линий.
        /// Сначала проставляем координаты (проход 1), затем строим линии (проход 2).
        /// </summary>
        private void RebuildLayout()
        {
            AllMatches = new List<BracketMatch>();
            Connections = new List<BracketConnection>();

            if (_bracket == null || _bracket.Rounds.Count == 0)
                return;

            int totalRounds = _bracket.Rounds.Count;
            int maxMatchesInFirstRound = _bracket.Rounds[0].Matches.Count;

            // Высота всей сетки — определяется первым (максимальным) раундом
            double totalHeight = maxMatchesInFirstRound * (CardHeight + RowGap) - RowGap;

            // ── Проход 1: выставляем координаты ──────────────────────────
            for (int ri = 0; ri < totalRounds; ri++)
            {
                var round = _bracket.Rounds[ri];
                int matchCount = round.Matches.Count;
                if (matchCount == 0) continue;

                double slotHeight = totalHeight / matchCount;
                double x = ri * (CardWidth + ColGap);

                for (int mi = 0; mi < matchCount; mi++)
                {
                    var match = round.Matches[mi];
                    double centerY = mi * slotHeight + slotHeight / 2.0;

                    match.XPosition = x;
                    match.YPosition = centerY - CardHeight / 2.0;

                    AllMatches.Add(match);
                }
            }

            // Бронзовый матч — под финалом
            if (_bracket.BronzeMatch != null)
            {
                var bronze = _bracket.BronzeMatch;
                int lastRi = totalRounds - 1;
                bronze.XPosition = lastRi * (CardWidth + ColGap);
                bronze.YPosition = totalHeight + RowGap * 2;
                AllMatches.Add(bronze);
            }

            // ── Проход 2: строим линии (координаты уже выставлены) ──────
            foreach (var match in AllMatches)
            {
                if (match.NextMatch == null) continue;

                double myRightX  = match.XPosition + CardWidth;
                double myMidY    = match.YPosition + CardHeight / 2.0;
                double nextLeftX = match.NextMatch.XPosition;
                double nextMidY  = match.NextMatch.YPosition + CardHeight / 2.0;
                double midX      = (myRightX + nextLeftX) / 2.0;

                // Три отрезка: горизонт → вертикаль → горизонт
                Connections.Add(new BracketConnection
                {
                    StartX = myRightX, StartY = myMidY,
                    EndX   = midX,     EndY   = myMidY
                });
                Connections.Add(new BracketConnection
                {
                    StartX = midX, StartY = myMidY,
                    EndX   = midX, EndY   = nextMidY
                });
                Connections.Add(new BracketConnection
                {
                    StartX = midX,      StartY = nextMidY,
                    EndX   = nextLeftX, EndY   = nextMidY
                });
            }
        }
    }
}
