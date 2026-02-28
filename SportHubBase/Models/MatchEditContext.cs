using System;
using System.Collections.Generic;
using System.Windows.Input;
using SportHubBase.ViewModels;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class MatchEditContext : BaseViewModel
    {
        protected readonly Match _match;

        public string Team1 => _match.Team1;
        public string Team2 => _match.Team2;

        public abstract string DisplayName { get; }
        public abstract string HeaderScore1 { get; }
        public abstract string HeaderScore2 { get; }

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                IsTechnicalDefeatVisible = (value == "Техническое поражение");
                OnStatusChanged();
            }
        }

        private string _referee;
        public string Referee
        {
            get => _referee;
            set { _referee = value; OnPropertyChanged(); }
        }

        private string _location;
        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(); }
        }

        private string _mvp;
        public string Mvp
        {
            get => _mvp;
            set { _mvp = value; OnPropertyChanged(); }
        }

        private bool _isTechnicalDefeatVisible;
        public bool IsTechnicalDefeatVisible
        {
            get => _isTechnicalDefeatVisible;
            set { _isTechnicalDefeatVisible = value; OnPropertyChanged(); }
        }

        private string _technicalDefeatTeam;
        public string TechnicalDefeatTeam
        {
            get => _technicalDefeatTeam;
            set 
            { 
                _technicalDefeatTeam = value; 
                OnPropertyChanged();
                
                if (!string.IsNullOrEmpty(value) && Status == "Техническое поражение")
                {
                    HandleTechnicalDefeat(value);
                }
            }
        }

        public List<string> TechnicalDefeatTeams => new List<string> { Team1, Team2 };

        public ICommand CancelTechnicalDefeatCommand { get; }

        protected MatchEditContext(Match match)
        {
            _match = match ?? throw new ArgumentNullException(nameof(match));
            
            _status = _match.Status;
            _referee = _match.Referee;
            _location = _match.Location;
            _mvp = _match.Mvp;

            CancelTechnicalDefeatCommand = new RelayCommand(_ => CancelTechnicalDefeat());
        }

        private void CancelTechnicalDefeat()
        {
            Status = "Не сыгран";
            TechnicalDefeatTeam = null;
            IsTechnicalDefeatVisible = false;
            OnCancelTechnicalDefeat();
        }

        protected virtual void OnStatusChanged() { }
        
        /// <summary>
        /// Применяет техпоражение (например, ставит 0:3 или обнуляет сеты).
        /// </summary>
        protected abstract void HandleTechnicalDefeat(string losingTeam);
        
        /// <summary>
        /// Очищает последствия техпоражения.
        /// </summary>
        protected abstract void OnCancelTechnicalDefeat();

        /// <summary>
        /// Сохраняет изменения обратно в объект Match.
        /// </summary>
        public virtual void ApplyChanges()
        {
            _match.Status = Status;
            _match.Referee = Referee;
            _match.Location = Location;
            _match.Mvp = Mvp;
        }
    }
}
