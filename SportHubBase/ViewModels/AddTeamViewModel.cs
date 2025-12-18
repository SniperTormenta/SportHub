using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SportHubBase.Models;
using SportHubBase.Services;

namespace SportHubBase.ViewModels
{
    public class AddTeamViewModel : BaseViewModel
    {
        private readonly JsonStorageService _storage = new JsonStorageService();
        private readonly Guid _tournamentId;
        private readonly Team _existingTeam;

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
        public ICommand DeleteTeamCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool> RequestClose;

        // Режим редактирования, если передана существующая команда
        public bool IsEditMode => _existingTeam != null;

        public AddTeamViewModel(Guid tournamentId, Team existingTeam = null)
        {
            _tournamentId = tournamentId;
            _existingTeam = existingTeam;

            AddPlayerCommand = new RelayCommand(_ => AddPlayer());
            RemovePlayerCommand = new RelayCommand(RemovePlayer, p => p is Player);
            SaveTeamCommand = new RelayCommand(_ => SaveTeam(), _ => CanSaveTeam());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));

            DeleteTeamCommand = new RelayCommand(_ => DeleteTeam(), _ => IsEditMode);

            // Если редактируем существующую команду — подставляем её данные
            if (_existingTeam != null)
            {
                TeamName = _existingTeam.Name;
                OnPropertyChanged(nameof(TeamName));

                // Загружаем сохранённый состав, если есть
                if (_existingTeam.Players != null && _existingTeam.Players.Any())
                {
                    foreach (var p in _existingTeam.Players)
                    {
                        Players.Add(p);
                    }

                    OnPropertyChanged(nameof(PlayersCount));

                    var captain = _existingTeam.Players.FirstOrDefault(p => p.IsCaptain)
                                  ?? _existingTeam.Players.FirstOrDefault();
                    if (captain != null)
                    {
                        SelectedCaptainName = captain.Name;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(_existingTeam.Captain))
                {
                    // Если старые данные без списка игроков — создаём капитана как единственного игрока
                    var captainPlayer = new Player
                    {
                        Name = _existingTeam.Captain,
                        IsCaptain = true
                    };
                    Players.Add(captainPlayer);
                    OnPropertyChanged(nameof(PlayersCount));

                    SelectedCaptainName = captainPlayer.Name;
                }
            }
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
                   !string.IsNullOrWhiteSpace(SelectedCaptainName);
        }

        private void SaveTeam()
        {
            var tournaments = _storage.LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == _tournamentId);
            if (tournament != null)
            {
                if (_existingTeam == null)
                {
                    // Создание новой команды
                    var newTeam = new Team
                    {
                        Name = TeamName,
                        Captain = SelectedCaptainName ?? "Капитан не выбран"
                    };

                    // Сохраняем состав игроков
                    newTeam.Players = Players
                        .Select(p => new Player
                        {
                            Name = p.Name,
                            Role = p.Role,
                            IsCaptain = p.Name == SelectedCaptainName
                        })
                        .ToList();

                    tournament.Teams.Add(newTeam);
                }
                else
                {
                    // Обновление существующей команды
                    var teamToUpdate = tournament.Teams
                        .FirstOrDefault(t => t.Name == _existingTeam.Name && t.Captain == _existingTeam.Captain);

                    if (teamToUpdate != null)
                    {
                        teamToUpdate.Name = TeamName;
                        teamToUpdate.Captain = SelectedCaptainName ?? "Капитан не выбран";

                        // Обновляем состав игроков
                        teamToUpdate.Players = Players
                            .Select(p => new Player
                            {
                                Name = p.Name,
                                Role = p.Role,
                                IsCaptain = p.Name == SelectedCaptainName
                            })
                            .ToList();
                    }
                }

                _storage.UpdateTournament(tournament);
            }

            RequestClose?.Invoke(true);
        }

        private void DeleteTeam()
        {
            if (!IsEditMode || _existingTeam == null)
                return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить эту команду?",
                "Удаление команды",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var tournaments = _storage.LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == _tournamentId);
            if (tournament != null)
            {
                var teamToRemove = tournament.Teams
                    .FirstOrDefault(t => t.Name == _existingTeam.Name && t.Captain == _existingTeam.Captain);

                if (teamToRemove != null)
                {
                    tournament.Teams.Remove(teamToRemove);
                    _storage.UpdateTournament(tournament);
                }
            }

            RequestClose?.Invoke(true);
        }
    }

}