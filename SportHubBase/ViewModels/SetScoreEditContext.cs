using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SportHubBase.Models;

namespace SportHubBase.ViewModels
{
    public class SetScoreEditContext : MatchEditContext
    {
        public override string DisplayName => "Счетчики по сетам";
        public override string HeaderScore1 => SetsScoreLeft;
        public override string HeaderScore2 => SetsScoreRight;

        public ObservableCollection<SetScore> SetsList { get; } = new ObservableCollection<SetScore>();

        public string SetsScoreLeft => CalculateSetsWon(true).ToString();
        public string SetsScoreRight => CalculateSetsWon(false).ToString();
        public string TotalScoreLeft => CalculateTotalScore(true).ToString();
        public string TotalScoreRight => CalculateTotalScore(false).ToString();
        public bool HasSets => SetsList.Count > 0;

        public ICommand AddSetCommand { get; }
        public ICommand RemoveSetCommand { get; }

        public SetScoreEditContext(Match match) : base(match)
        {
            AddSetCommand = new RelayCommand(_ => AddSet());
            RemoveSetCommand = new RelayCommand(RemoveSet, CanRemoveSet);

            LoadSetsFromString();

            SetsList.CollectionChanged += (s, e) => 
            {
                RaiseCalculatedProperties();
                if (e.NewItems != null)
                {
                    foreach (SetScore set in e.NewItems)
                        set.PropertyChanged += (sender, args) => RaiseCalculatedProperties();
                }
            };
            foreach (var set in SetsList)
                set.PropertyChanged += (s, e) => RaiseCalculatedProperties();
        }

        private void LoadSetsFromString()
        {
            SetsList.Clear();
            if (string.IsNullOrWhiteSpace(_match.SetsBySet)) return;

            var sets = _match.SetsBySet.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            int number = 1;

            foreach (var setStr in sets)
            {
                var parts = setStr.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int s1) && int.TryParse(parts[1].Trim(), out int s2))
                {
                    var setScore = new SetScore { Number = number++ };
                    setScore.Score1 = s1;
                    setScore.Score2 = s2;
                    SetsList.Add(setScore);
                }
            }
        }

        private void AddSet()
        {
            SetsList.Add(new SetScore { Number = SetsList.Count + 1 });
        }

        private void RemoveSet(object parameter)
        {
            if (parameter is SetScore set)
            {
                SetsList.Remove(set);
                for (int i = 0; i < SetsList.Count; i++) SetsList[i].Number = i + 1;
            }
        }

        private bool CanRemoveSet(object parameter) => parameter is SetScore;

        private int CalculateSetsWon(bool forTeam1) => SetsList.Count(s => forTeam1 ? s.Score1 > s.Score2 : s.Score2 > s.Score1);
        private int CalculateTotalScore(bool forTeam1) => SetsList.Sum(s => forTeam1 ? s.Score1 : s.Score2);

        private void RaiseCalculatedProperties()
        {
            OnPropertyChanged(nameof(SetsScoreLeft));
            OnPropertyChanged(nameof(SetsScoreRight));
            OnPropertyChanged(nameof(TotalScoreLeft));
            OnPropertyChanged(nameof(TotalScoreRight));
            OnPropertyChanged(nameof(HasSets));
            OnPropertyChanged(nameof(HeaderScore1));
            OnPropertyChanged(nameof(HeaderScore2));
        }

        protected override void HandleTechnicalDefeat(string losingTeam)
        {
            SetsList.Clear();
            for (int i = 1; i <= 3; i++)
            {
                SetsList.Add(new SetScore 
                { 
                    Number = i, 
                    Score1 = losingTeam == Team1 ? 0 : 25, 
                    Score2 = losingTeam == Team1 ? 25 : 0 
                });
            }
        }

        protected override void OnCancelTechnicalDefeat()
        {
            SetsList.Clear();
        }

        public override void ApplyChanges()
        {
            base.ApplyChanges();

            if (SetsList.Count == 0)
            {
                _match.SetsBySet = string.Empty;
                _match.SetsScore = string.Empty;
                _match.TotalScore = string.Empty;
            }
            else
            {
                _match.SetsBySet = string.Join(";", SetsList.Select(s => $"{s.Score1}:{s.Score2}"));
                _match.SetsScore = $"{CalculateSetsWon(true)}:{CalculateSetsWon(false)}";
                _match.TotalScore = $"{CalculateTotalScore(true)}:{CalculateTotalScore(false)}";
                
                _match.Team1QuickScore = CalculateSetsWon(true).ToString();
                _match.Team2QuickScore = CalculateSetsWon(false).ToString();
            }

            if (SetsList.Any())
            {
                if (Status != "Техническое поражение")
                    Status = "Сыгран";
                _match.Status = Status;
            }
        }
    }
}
