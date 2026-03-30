using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace SportHubBase.ViewModels
{
    public class MatchDetailsViewModel : BaseViewModel
    {
        private readonly Match _match;
        private readonly IStorage _storage;
        private readonly IMatchService _matchService;
        private readonly ISportScoreStrategy _strategy;

        public MatchEditContext EditContext { get; }
        
        public bool CanEdit { get; }
        public bool IsReadOnly => !CanEdit;

        public string Team1 => _match.Team1;
        public string Team2 => _match.Team2;
        public string Team1Logo => "https://via.placeholder.com/80";
        public string Team2Logo => "https://via.placeholder.com/80";

        public ObservableCollection<string> PlayersList { get; } = new ObservableCollection<string>();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action RequestClose;

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public int? MatchNumber
        {
            get => _match.MatchNumber;
            set
            {
                if (_match.MatchNumber != value)
                {
                    _match.MatchNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public MatchDetailsViewModel(Match match, Guid tournamentId, IStorage storage, IMatchService matchService, ISportScoreStrategy strategy, bool canEdit)
        {
            CanEdit = canEdit;
            _match = match ?? throw new ArgumentNullException(nameof(match));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _matchService = matchService ?? throw new ArgumentNullException(nameof(matchService));
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

            EditContext = _strategy.CreateEditContext(_match);

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            LoadPlayersFromTeams(tournamentId);
        }

        private void LoadPlayersFromTeams(Guid tournamentId)
        {
            PlayersList.Clear();
            
            var tournaments = _storage.LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == tournamentId);
            
            if (tournament == null)
                return;

            var team1 = tournament.Teams.FirstOrDefault(t => 
                string.Equals(t.Name, _match.Team1, StringComparison.OrdinalIgnoreCase));
            var team2 = tournament.Teams.FirstOrDefault(t => 
                string.Equals(t.Name, _match.Team2, StringComparison.OrdinalIgnoreCase));

            if (team1 != null && team1.Players != null)
            {
                foreach (var player in team1.Players)
                {
                    if (!string.IsNullOrWhiteSpace(player.Name) && !PlayersList.Contains(player.Name))
                        PlayersList.Add(player.Name);
                }
            }

            if (team2 != null && team2.Players != null)
            {
                foreach (var player in team2.Players)
                {
                    if (!string.IsNullOrWhiteSpace(player.Name) && !PlayersList.Contains(player.Name))
                        PlayersList.Add(player.Name);
                }
            }
        }

        private void Save()
        {
            ErrorMessage = string.Empty;

            if (MatchNumber.HasValue)
            {
                var tournaments = _storage.LoadTournaments();
                var tournament = tournaments.Find(t => t.Matches != null && t.Matches.Any(m => m.Id == _match.Id));
                
                if (tournament != null)
                {
                    if (!_matchService.ValidateUniqueNumber(_match, tournament.Matches))
                    {
                        ErrorMessage = $"Номер матча {MatchNumber} уже занят.";
                        return;
                    }
                }
            }

            EditContext.ApplyChanges();
            _strategy.FinalizeMatch(_match, EditContext);

            RequestClose?.Invoke();
        }

        private void Cancel()
        {
            RequestClose?.Invoke();
        }
    }
}