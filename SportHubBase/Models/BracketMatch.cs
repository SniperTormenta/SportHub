using SportHubBase.ViewModels;
using Newtonsoft.Json;
using System;

namespace SportHubBase.Models
{
    public class BracketMatch : BaseViewModel
    {
        private Team _team1;
        private Team _team2;
        private string _score1;
        private string _score2;
        private bool _isCompleted;

        [JsonProperty("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty("createdByUserId")]
        public Guid? CreatedByUserId { get; set; }

        [JsonProperty("team1")]
        public Team Team1
        {
            get => _team1;
            set { _team1 = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTeam1)); }
        }

        [JsonProperty("team2")]
        public Team Team2
        {
            get => _team2;
            set { _team2 = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTeam2)); }
        }

        [JsonProperty("score1")]
        public string Score1
        {
            get => _score1;
            set { _score1 = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCompleted)); }
        }

        [JsonProperty("score2")]
        public string Score2
        {
            get => _score2;
            set { _score2 = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCompleted)); }
        }

        [JsonProperty("isCompleted")]
        public bool IsCompleted
        {
            get => _isCompleted || (!string.IsNullOrEmpty(Score1) && !string.IsNullOrEmpty(Score2));
            set { _isCompleted = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public BracketMatch NextMatch { get; set; }

        [JsonProperty("nextMatchId")]
        public Guid? NextMatchId { get; set; }

        [JsonProperty("isTeam1InNext")]
        public bool IsTeam1InNext { get; set; }

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

        /// <summary>Координата X для Canvas (вычисляется при построении сетки, не сериализуется).</summary>
        [JsonIgnore]
        public double XPosition { get; set; }

        /// <summary>Координата Y для Canvas (вычисляется при построении сетки, не сериализуется).</summary>
        [JsonIgnore]
        public double YPosition { get; set; }

        [JsonIgnore]
        public bool IsBye => Team2 == null && Team1 != null;

        [JsonIgnore]
        public string DisplayTeam1 => Team1?.Name ?? (IsBye ? "BYE" : "Ожидание...");

        [JsonIgnore]
        public string DisplayTeam2 => Team2?.Name ?? (IsBye ? "" : "Ожидание...");

        [JsonIgnore]
        public Team Winner => !IsCompleted ? null :
                              string.IsNullOrEmpty(Score1) || string.IsNullOrEmpty(Score2) ? null :
                              int.TryParse(Score1, out int s1) && int.TryParse(Score2, out int s2) ?
                                  (s1 > s2 ? Team1 : (s2 > s1 ? Team2 : null)) : null;

        public void TryAdvance()
        {
            if (!IsCompleted || Winner == null) return;

            // Продвигаем победителя в следующий матч
            if (NextMatch != null)
            {
                if (IsTeam1InNext)
                    NextMatch.Team1 = Winner;
                else
                    NextMatch.Team2 = Winner;
            }

            // Временно отключаем бронзу, пока не знаем индекс полуфинала
            // if (BronzeLoserTarget != null && IsSemiFinal())
            // {
            //     Team loser = Winner == Team1 ? Team2 : Team1;
            //     if (loser != null)
            //     {
            //         if (BronzeLoserTarget.Team1 == null)
            //             BronzeLoserTarget.Team1 = loser;
            //         else
            //             BronzeLoserTarget.Team2 = loser;
            //     }
            // }

            OnPropertyChanged(nameof(Winner));
        }
    }
}