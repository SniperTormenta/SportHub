// Models/BracketMatch.cs
using SportHubBase.ViewModels;
using Newtonsoft.Json;
using System;

namespace SportHubBase.Models
{
    /// <summary>
    /// Модель одного матча в олимпийской сетке.
    /// Наследует BaseViewModel для использования OnPropertyChanged в биндингах WPF.
    /// </summary>
    public class BracketMatch : BaseViewModel
    {
        private Team _team1;
        private Team _team2;
        private int? _score1;
        private int? _score2;
        private bool _isCompleted;
        private bool _isInProgress;

        [JsonProperty("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Первая команда.
        /// </summary>
        [JsonProperty("team1")]
        public Team Team1
        {
            get => _team1;
            set { _team1 = value; OnPropertyChanged(); OnPropertyChanged("DisplayTeam1"); }
        }

        /// <summary>
        /// Вторая команда.
        /// </summary>
        [JsonProperty("team2")]
        public Team Team2
        {
            get => _team2;
            set { _team2 = value; OnPropertyChanged(); OnPropertyChanged("DisplayTeam2"); }
        }

        /// <summary>
        /// Счёт первой команды.
        /// </summary>
        [JsonProperty("score1")]
        public int? Score1
        {
            get => _score1;
            set { _score1 = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Счёт второй команды.
        /// </summary>
        [JsonProperty("score2")]
        public int? Score2
        {
            get => _score2;
            set { _score2 = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Флаг завершенности матча.
        /// </summary>
        [JsonProperty("isCompleted")]
        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Флаг текущего ("активного") матча.
        /// </summary>
        [JsonProperty("isInProgress")]
        public bool IsInProgress
        {
            get => _isInProgress;
            set { _isInProgress = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Ссылка на следующий матч, куда идет победитель.
        /// </summary>
        [JsonIgnore]
        public BracketMatch NextMatch { get; set; }

        [JsonProperty("nextMatchId")]
        public Guid? NextMatchId { get; set; }

        /// <summary>
        /// Определяет, в какой слот (Team1 или Team2) идет победитель в следующем матче.
        /// </summary>
        [JsonProperty("isTeam1InNext")]
        public bool IsTeam1InNext { get; set; }

        /// <summary>
        /// Ссылка на матч за 3-е место, куда идет проигравший (для полуфиналов).
        /// </summary>
        [JsonIgnore]
        public BracketMatch BronzeLoserTarget { get; set; }

        [JsonProperty("bronzeLoserTargetId")]
        public Guid? BronzeLoserTargetId { get; set; }

        [JsonProperty("roundIndex")]
        public int RoundIndex { get; set; }

        [JsonProperty("matchIndex")]
        public int MatchIndex { get; set; }

        [JsonProperty("isFinal")]
        public bool IsFinal { get; set; }

        [JsonProperty("isBronzeMatch")]
        public bool IsBronzeMatch { get; set; }

        [JsonIgnore]
        public string DisplayTeam1 => Team1 != null ? Team1.Name : (IsBronzeMatch ? "Проигравший 1/2 (1)" : "Ожидание...");

        [JsonIgnore]
        public string DisplayTeam2 => Team2 != null ? Team2.Name : (IsBronzeMatch ? "Проигравший 1/2 (2)" : (Team1 != null && IsByeMatch() ? "BYE" : "Ожидание..."));

        [JsonIgnore]
        public bool IsTeam1Winner => Score1.HasValue && Score2.HasValue && Score1.Value > Score2.Value;

        [JsonIgnore]
        public bool IsTeam2Winner => Score1.HasValue && Score2.HasValue && Score2.Value > Score1.Value;

        private bool IsByeMatch()
        {
            // Логика BYE: если количество команд нечетное, одна может получить BYE.
            return false; // Будет управляться из стратегии расписания
        }

        /// <summary>
        /// Продвигает победителя в следующий раунд.
        /// </summary>
        public void TryAdvanceWinner()
        {
            if (Score1 == null || Score2 == null) return;
            if (Score1 == Score2) return;

            Team winner = Score1 > Score2 ? Team1 : Team2;
            if (NextMatch != null && winner != null)
            {
                if (IsTeam1InNext) NextMatch.Team1 = winner;
                else NextMatch.Team2 = winner;
            }
            IsCompleted = true;
        }

        /// <summary>
        /// Продвигает проигравшего в матч за 3-е место (бронзовый финал).
        /// </summary>
        public void TryAdvanceLoser()
        {
            if (Score1 == null || Score2 == null) return;
            if (Score1 == Score2) return;

            Team loser = Score1 > Score2 ? Team2 : Team1;
            if (BronzeLoserTarget != null && loser != null)
            {
                if (IsTeam1InNext) BronzeLoserTarget.Team1 = loser;
                else BronzeLoserTarget.Team2 = loser;
            }
        }

        /// <summary>
        /// Общая логика продвижения по сетке.
        /// </summary>
        public void TryAdvance()
        {
            TryAdvanceWinner();
            TryAdvanceLoser();
        }
    }
}
