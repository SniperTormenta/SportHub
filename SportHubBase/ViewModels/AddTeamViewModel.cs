using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SportHubBase.Models;
using SportHubBase.Services;

namespace SportHubBase.ViewModels
{
    public class AddTeamViewModel : BaseViewModel
    {
        private readonly JsonStorageService _storage = new JsonStorageService();
        private readonly Guid _tournamentId;

        public string TeamName { get; set; }

        // Выбранный капитан из ComboBox (индекс или имя — здесь имя игрока)
        private string _selectedCaptainName;
        public string SelectedCaptainName
        {
            get => _selectedCaptainName;
            set
            {
                _selectedCaptainName = value;
                UpdateCaptainInPlayers();
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Player> Players { get; } = new ObservableCollection<Player>();

        public int PlayersCount => Players.Count;

        public ICommand AddPlayerCommand { get; }
        public ICommand RemovePlayerCommand { get; }
        public ICommand SaveTeamCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool> RequestClose;

        public AddTeamViewModel(Guid tournamentId)
        {
            _tournamentId = tournamentId;

            AddPlayerCommand = new RelayCommand(_ => AddPlayer());
            RemovePlayerCommand = new RelayCommand(RemovePlayer, p => p is Player);
            SaveTeamCommand = new RelayCommand(_ => SaveTeam(), _ => CanSaveTeam());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }

        private void AddPlayer()
        {
            Players.Add(new Player { Name = "" }); // Пустое имя — пользователь введёт
            OnPropertyChanged(nameof(PlayersCount));
        }

        private void RemovePlayer(object parameter)
        {
            if (parameter is Player player)
            {
                Players.Remove(player);
                OnPropertyChanged(nameof(PlayersCount));
                UpdateCaptainInPlayers(); // На случай удаления капитана
            }
        }

        // Обновляем IsCaptain у всех игроков
        private void UpdateCaptainInPlayers()
        {
            foreach (var player in Players)
            {
                player.IsCaptain = player.Name == SelectedCaptainName;
            }
        }

        private bool CanSaveTeam()
        {
            return !string.IsNullOrWhiteSpace(TeamName) &&
                   Players.Count > 0 &&
                   !string.IsNullOrWhiteSpace(SelectedCaptainName) &&
                   Players.Any(p => p.Name == SelectedCaptainName);
        }

        private void SaveTeam()
        {
            var newTeam = new Team
            {
                Name = TeamName,
                Captain = SelectedCaptainName ?? "Капитан не выбран"
            };

            var tournaments = _storage.LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == _tournamentId);
            if (tournament != null)
            {
                tournament.Teams.Add(newTeam);
                _storage.UpdateTournament(tournament);
            }

            RequestClose?.Invoke(true);
        }
    }

    // Player с IsCaptain
    public class Player : BaseViewModel
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCaptain)); // Обновляем при смене имени
            }
        }

        public bool IsCaptain { get; set; } // Управляется из VM
    }
}