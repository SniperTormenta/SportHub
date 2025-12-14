// ViewModels/TournamentViewModel.cs (для окна турнира)
using SportHubBase.Models;
using SportHubBase.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SportHubBase.ViewModels
{
    public class TournamentViewModel : BaseViewModel
    {
        private readonly JsonStorageService _storage = new JsonStorageService();

        public Tournament CurrentTournament { get; private set; }

        public ObservableCollection<Team> Teams { get; } = new ObservableCollection<Team>();

        public bool IsLive { get { return CurrentTournament.IsLive; } }

        public ICommand AddTeamCommand { get; }

        public TournamentViewModel(Guid tournamentId)
        {
            var tournaments = _storage.LoadTournaments();
            CurrentTournament = tournaments.Find(t => t.Id == tournamentId);
            if (CurrentTournament != null)
            {
                foreach (var team in CurrentTournament.Teams)
                {
                    Teams.Add(team);
                }
            }

            AddTeamCommand = new RelayCommand(AddTeam);
            OnPropertyChanged(nameof(IsLive)); // Для обновления UI
        }

        private void AddTeam(object parameter)
        {
            // Логика добавления команды
            var newTeam = new Team { Name = "New Team", Captain = "Capt. New", LogoUrl = "default.png" };
            Teams.Add(newTeam);
            CurrentTournament.Teams.Add(newTeam);
            _storage.UpdateTournament(CurrentTournament);
        }
    }
}