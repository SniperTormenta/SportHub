using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.ViewModels
{
    public class AddTeamViewModel : BaseViewModel
    {
        private readonly IStorage _storage;
        private readonly IExcelService _excelService;
        private readonly Guid _tournamentId;
        private readonly Team _existingTeam;

        public string TeamName { get; set; }

        // Выбранный капитан из ComboBox
        private Player _selectedCaptain;
        public Player SelectedCaptain
        {
            get => _selectedCaptain;
            set
            {
                _selectedCaptain = value;
                UpdateCaptainInPlayers();
                OnPropertyChanged();
            }
        }

        // Для обратной совместимости - имя капитана
        public string SelectedCaptainName => SelectedCaptain?.Name;

        public ObservableCollection<Player> Players { get; } = new ObservableCollection<Player>();

        public int PlayersCount => Players.Count;

        public string PlayersCountText => $"Игроков: {Players.Count} ({(SelectedCaptain != null ? "1 капитан" : "0 капитанов")})";

        public ICommand AddPlayerCommand { get; }
        public ICommand RemovePlayerCommand { get; }
        public ICommand SaveTeamCommand { get; }
        public ICommand DeleteTeamCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ImportExcelCommand { get; }
        public ICommand DownloadTemplateCommand { get; }
        public ICommand MakeCaptainCommand { get; }

        public event Action<bool> RequestClose;

        // Режим редактирования, если передана существующая команда
        public bool IsEditMode => _existingTeam != null;

        public AddTeamViewModel(Guid tournamentId, IStorage storage, IExcelService excelService, Team existingTeam = null)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _tournamentId = tournamentId;
            _existingTeam = existingTeam;

            AddPlayerCommand = new RelayCommand(_ => AddPlayer());
            RemovePlayerCommand = new RelayCommand(RemovePlayer, p => p is Player);
            SaveTeamCommand = new RelayCommand(_ => SaveTeam(), _ => CanSaveTeam());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));

            DeleteTeamCommand = new RelayCommand(_ => DeleteTeam(), _ => IsEditMode);

            ImportExcelCommand = new RelayCommand(_ => ImportExcel());
            DownloadTemplateCommand = new RelayCommand(_ => DownloadTemplate());
            MakeCaptainCommand = new RelayCommand(MakeCaptain, p => p is Player);

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
                        SelectedCaptain = captain;
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

                    SelectedCaptain = captainPlayer;
                }
            }
        }

        private void AddPlayer()
        {
            Players.Add(new Player { Name = "" }); // Пустое имя — пользователь введёт
            OnPropertyChanged(nameof(PlayersCount));
            OnPropertyChanged(nameof(PlayersCountText));
        }

        private void RemovePlayer(object parameter)
        {
            if (parameter is Player player)
            {
                Players.Remove(player);
                OnPropertyChanged(nameof(PlayersCount));
                UpdateCaptainInPlayers(); // На случай удаления капитана
                OnPropertyChanged(nameof(PlayersCountText));
            }
        }

        private void MakeCaptain(object parameter)
        {
            if (parameter is Player newCaptain)
            {
                SelectedCaptain = newCaptain;
                OnPropertyChanged(nameof(PlayersCountText));
            }
        }

        // Обновляем IsCaptain у всех игроков
        private void UpdateCaptainInPlayers()
        {
            foreach (var player in Players)
            {
                player.IsCaptain = player == SelectedCaptain;
            }
        }

        private bool CanSaveTeam()
        {
            return !string.IsNullOrWhiteSpace(TeamName) &&
                   Players.Count > 0 &&
                   SelectedCaptain != null;
        }

        private void SaveTeam()
        {
            // Уникальность имён игроков. Проверяем дубли среди введенных
            var groups = Players.Where(p => !string.IsNullOrWhiteSpace(p.Name))
                                .GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                                .Where(g => g.Count() > 1);
            
            foreach (var group in groups)
            {
                var name = group.Key;
                var res = MessageBox.Show(
                    $"Игрок с именем \"{name}\" уже есть в команде. Добавить дубликат или отменить?", 
                    "Проверка уникальности", 
                    MessageBoxButton.OKCancel, 
                    MessageBoxImage.Warning);
                    
                if (res != MessageBoxResult.OK)
                {
                    return; // Отмена сохранения
                }
            }

            var tournaments = _storage.LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == _tournamentId);
            if (tournament != null)
            {
                if (_existingTeam == null)
                {
                    // Создание новой команды
                    var newTeam = new Team
                    {
                        Id = Guid.NewGuid(),
                        Name = TeamName,
                        Captain = SelectedCaptain?.Name ?? "Капитан не выбран"
                    };

                    // Сохраняем состав игроков
                    newTeam.Players = Players.ToList();

                    tournament.Teams.Add(newTeam);
                }
                else
                {
                    // Обновление существующей команды
                    // Ищем по ID, так как он уникален
                    var teamToUpdate = tournament.Teams
                        .FirstOrDefault(t => t.Id == _existingTeam.Id);

                    // Fallback для старых данных без ID (хотя после импорта/создания они должны быть)
                    if (teamToUpdate == null && _existingTeam.Id == Guid.Empty)
                    {
                         teamToUpdate = tournament.Teams
                            .FirstOrDefault(t => t.Name == _existingTeam.Name && t.Captain == _existingTeam.Captain);
                    }

                    if (teamToUpdate != null)
                    {
                        teamToUpdate.Name = TeamName;
                        teamToUpdate.Captain = SelectedCaptain?.Name ?? "Капитан не выбран";

                        // Обновляем состав игроков
                        teamToUpdate.Players = Players.ToList();
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
                var teamToRemove = tournament.Teams.FirstOrDefault(t => t.Id == _existingTeam.Id);

                // Fallback для старых данных
                if (teamToRemove == null && _existingTeam.Id == Guid.Empty)
                {
                    teamToRemove = tournament.Teams
                        .FirstOrDefault(t => t.Name == _existingTeam.Name && t.Captain == _existingTeam.Captain);
                }

                if (teamToRemove != null)
                {
                    tournament.Teams.Remove(teamToRemove);
                    _storage.UpdateTournament(tournament);
                }
            }

            RequestClose?.Invoke(true);
        }

        private void ImportExcel()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Выберите файл с составом"
            };

            if (dialog.ShowDialog() == true)
            {
                var importedPlayers = _excelService.ImportPlayers(dialog.FileName);
                if (importedPlayers.Count == 0)
                {
                    MessageBox.Show("Файл пуст или имеет неверный формат.", "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int addedCount = 0;
                int skippedCount = 0;
                
                // Проверки импорта на дублирования с ТЕКУЩИМ списком
                var duplicates = importedPlayers.Where(ip => Players.Any(p => string.Equals(p.Name, ip.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                bool addAllDuplicates = false;

                if (duplicates.Any())
                {
                    var res = MessageBox.Show($"Найдено {duplicates.Count} дубликатов при импорте. Добавить всех или пропустить дубли?", "Дубликаты в Excel", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    addAllDuplicates = (res == MessageBoxResult.Yes);
                }

                foreach (var player in importedPlayers)
                {
                    bool isDuplicate = Players.Any(p => string.Equals(p.Name, player.Name, StringComparison.OrdinalIgnoreCase));
                    if (isDuplicate && !addAllDuplicates)
                    {
                        skippedCount++;
                        continue;
                    }

                    Players.Add(player);
                    addedCount++;
                }

                OnPropertyChanged(nameof(PlayersCount));
                OnPropertyChanged(nameof(PlayersCountText));

                if (SelectedCaptain == null && Players.Any())
                {
                    SelectedCaptain = Players.First();
                }

                MessageBox.Show($"Импорт завершён.\nУспешно добавлено игроков: {addedCount}\nПропущено дубликатов: {skippedCount}", "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DownloadTemplate()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Сохранить шаблон импорта игроков",
                FileName = "PlayersTemplate.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                _excelService.SavePlayersTemplate(dialog.FileName);
                MessageBox.Show("Шаблон успешно сохранён.", "Шаблон", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

}