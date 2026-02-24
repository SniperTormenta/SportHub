using System;
using SportHubBase.Models;

namespace SportHubBase.ViewModels
{
    public class SimpleScoreEditContext : MatchEditContext
    {
        public override string DisplayName => "Простой счетчик";
        public override string HeaderScore1 => Team1QuickScore;
        public override string HeaderScore2 => Team2QuickScore;

        private string _team1QuickScore;
        public string Team1QuickScore
        {
            get => _team1QuickScore;
            set { _team1QuickScore = value; OnPropertyChanged(); OnPropertyChanged(nameof(HeaderScore1)); }
        }

        private string _team2QuickScore;
        public string Team2QuickScore
        {
            get => _team2QuickScore;
            set { _team2QuickScore = value; OnPropertyChanged(); OnPropertyChanged(nameof(HeaderScore2)); }
        }

        public SimpleScoreEditContext(Match match) : base(match)
        {
            _team1QuickScore = _match.Team1QuickScore;
            _team2QuickScore = _match.Team2QuickScore;
        }

        protected override void HandleTechnicalDefeat(string losingTeam)
        {
            if (losingTeam == Team1)
            {
                Team1QuickScore = "0";
                Team2QuickScore = "3";
            }
            else
            {
                Team1QuickScore = "3";
                Team2QuickScore = "0";
            }
        }

        protected override void OnCancelTechnicalDefeat()
        {
            Team1QuickScore = string.Empty;
            Team2QuickScore = string.Empty;
        }

        public override void ApplyChanges()
        {
            base.ApplyChanges();
            _match.Team1QuickScore = Team1QuickScore;
            _match.Team2QuickScore = Team2QuickScore;
            
            // Если введен счет, то статус завершен
            if (!string.IsNullOrWhiteSpace(Team1QuickScore) || !string.IsNullOrWhiteSpace(Team2QuickScore))
            {
                if (Status != "Техническое поражение")
                    Status = "Сыгран";
                _match.Status = Status;
            }
        }
    }
}
