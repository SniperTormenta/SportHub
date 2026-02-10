// ViewModels/MatchDetailsViewModel.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SportHubBase.ViewModels
{
    public class MatchDetailsViewModel : BaseViewModel
    {
        private readonly Match _match;
        private readonly IStorage _storage;
        private readonly IMatchService _matchService;

        public ObservableCollection<SetScore> SetsList { get; } = new ObservableCollection<SetScore>();
        public ObservableCollection<string> PlayersList { get; } = new ObservableCollection<string>();

        // Вычисляемые свойства (только для чтения)
        public string SetsScoreLeft => CalculateSetsWon(true).ToString();
        public string SetsScoreRight => CalculateSetsWon(false).ToString();
        public string TotalScoreLeft => CalculateTotalScore(true).ToString();
        public string TotalScoreRight => CalculateTotalScore(false).ToString();
        public bool HasSets => SetsList.Count > 0;

        // Прямой доступ к данным матча
        public string Team1 => _match.Team1;
        public string Team2 => _match.Team2;

        public string Status
        {
            get => _match.Status;
            set
            {
                _match.Status = value;
                OnPropertyChanged();
            }
        }

        public string Mvp
        {
            get => _match.Mvp;
            set
            {
                _match.Mvp = value;
                OnPropertyChanged();
            }
        }

        public string Referee
        {
            get => _match.Referee;
            set
            {
                _match.Referee = value;
                OnPropertyChanged();
            }
        }

        public string Location
        {
            get => _match.Location;
            set
            {
                _match.Location = value;
                OnPropertyChanged();
            }
        }

        // Быстрый счет (синхронизируется с Match)
        public string Team1QuickScore
        {
            get => _match.Team1QuickScore;
            set
            {
                if (_match.Team1QuickScore != value)
                {
                    // Если есть сеты, быстрый счет должен быть синхронизирован с ними
                    // Поэтому при попытке изменить быстрый счет, если есть сеты, игнорируем изменение
                    // и синхронизируем обратно на основе сетов
                    if (SetsList.Count > 0)
                    {
                        SyncSetsToQuickScore();
                        OnPropertyChanged(); // Обновляем UI
                    }
                    else
                    {
                        // Если сетов нет, можно вводить быстрый счет вручную
                        _match.Team1QuickScore = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        public string Team2QuickScore
        {
            get => _match.Team2QuickScore;
            set
            {
                if (_match.Team2QuickScore != value)
                {
                    // Если есть сеты, быстрый счет должен быть синхронизирован с ними
                    // Поэтому при попытке изменить быстрый счет, если есть сеты, игнорируем изменение
                    // и синхронизируем обратно на основе сетов
                    if (SetsList.Count > 0)
                    {
                        SyncSetsToQuickScore();
                        OnPropertyChanged(); // Обновляем UI
                    }
                    else
                    {
                        // Если сетов нет, можно вводить быстрый счет вручную
                        _match.Team2QuickScore = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        // Заглушки для логотипов (потом можно доработать через поиск по имени команды)
        public string Team1Logo => "https://via.placeholder.com/80";
        public string Team2Logo => "https://via.placeholder.com/80";

        // Команды — используем RelayCommand из BaseViewModel
        public ICommand AddSetCommand { get; }
        public ICommand RemoveSetCommand { get; }
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

        public MatchDetailsViewModel(Match match, Guid tournamentId, IStorage storage, IMatchService matchService)
        {
            _match = match ?? throw new ArgumentNullException(nameof(match));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _matchService = matchService ?? throw new ArgumentNullException(nameof(matchService));

            AddSetCommand = new RelayCommand(_ => AddSet());
            RemoveSetCommand = new RelayCommand(RemoveSet, CanRemoveSet);
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            LoadSetsFromString();
            LoadPlayersFromTeams(tournamentId);

            // Подписка на изменения для пересчёта счёта
            SetsList.CollectionChanged += (s, e) => 
            {
                RaiseCalculatedProperties();
                SyncSetsToQuickScore();
                // Подписываемся на новые сеты
                if (e.NewItems != null)
                {
                    foreach (SetScore set in e.NewItems)
                    {
                        set.PropertyChanged += (sender, args) => 
                        {
                            RaiseCalculatedProperties();
                            SyncSetsToQuickScore();
                        };
                    }
                }
            };
            foreach (var set in SetsList)
                set.PropertyChanged += (s, e) => 
                {
                    RaiseCalculatedProperties();
                    SyncSetsToQuickScore();
                };

            // Подписка на изменения Match для синхронизации быстрого счета
            _match.PropertyChanged += OnMatchPropertyChanged;
        }

        private void LoadPlayersFromTeams(Guid tournamentId)
        {
            PlayersList.Clear();
            
            var tournaments = _storage.LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == tournamentId);
            
            if (tournament == null)
                return;

            // Находим команды по именам из матча
            var team1 = tournament.Teams.FirstOrDefault(t => 
                string.Equals(t.Name, _match.Team1, StringComparison.OrdinalIgnoreCase));
            var team2 = tournament.Teams.FirstOrDefault(t => 
                string.Equals(t.Name, _match.Team2, StringComparison.OrdinalIgnoreCase));

            // Добавляем игроков из обеих команд
            if (team1 != null && team1.Players != null)
            {
                foreach (var player in team1.Players)
                {
                    if (!string.IsNullOrWhiteSpace(player.Name) && !PlayersList.Contains(player.Name))
                    {
                        PlayersList.Add(player.Name);
                    }
                }
            }

            if (team2 != null && team2.Players != null)
            {
                foreach (var player in team2.Players)
                {
                    if (!string.IsNullOrWhiteSpace(player.Name) && !PlayersList.Contains(player.Name))
                    {
                        PlayersList.Add(player.Name);
                    }
                }
            }
        }

        private void LoadSetsFromString()
        {
            SetsList.Clear();

            if (string.IsNullOrWhiteSpace(_match.SetsBySet))
            {
                // Если сетов нет, но есть быстрый счет, оставляем его
                return;
            }

            var sets = _match.SetsBySet.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            int number = 1;

            foreach (var setStr in sets)
            {
                var parts = setStr.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out int s1) &&
                    int.TryParse(parts[1].Trim(), out int s2))
                {
                    var setScore = new SetScore { Number = number++ };
                    setScore.Score1 = s1;
                    setScore.Score2 = s2;
                    setScore.PropertyChanged += (s, e) => 
                    {
                        RaiseCalculatedProperties();
                        SyncSetsToQuickScore();
                    };
                    SetsList.Add(setScore);
                }
            }
            
            // Синхронизируем быстрый счет после загрузки сетов
            if (SetsList.Count > 0)
            {
                SyncSetsToQuickScore();
            }
        }

        private void SaveSetsToString()
        {
            if (SetsList.Count == 0)
            {
                _match.SetsBySet = string.Empty;
                _match.SetsScore = string.Empty;
                _match.TotalScore = string.Empty;
                return;
            }

            _match.SetsBySet = string.Join(";", SetsList.Select(s => $"{s.Score1}:{s.Score2}"));

            int won1 = CalculateSetsWon(true);
            int won2 = CalculateSetsWon(false);
            _match.SetsScore = $"{won1}:{won2}";

            int total1 = CalculateTotalScore(true);
            int total2 = CalculateTotalScore(false);
            _match.TotalScore = $"{total1}:{total2}";
        }

        private int CalculateSetsWon(bool forTeam1)
            => SetsList.Count(s => forTeam1 ? s.Score1 > s.Score2 : s.Score2 > s.Score1);

        private int CalculateTotalScore(bool forTeam1)
            => SetsList.Sum(s => forTeam1 ? s.Score1 : s.Score2);

        private void RaiseCalculatedProperties()
        {
            OnPropertyChanged(nameof(SetsScoreLeft));
            OnPropertyChanged(nameof(SetsScoreRight));
            OnPropertyChanged(nameof(TotalScoreLeft));
            OnPropertyChanged(nameof(TotalScoreRight));
            OnPropertyChanged(nameof(HasSets));
        }

        /// <summary>
        /// Синхронизирует быстрый счет со счетом по сетам (когда меняются сеты)
        /// </summary>
        private void SyncSetsToQuickScore()
        {
            if (SetsList.Count > 0)
            {
                int setsWon1 = CalculateSetsWon(true);
                int setsWon2 = CalculateSetsWon(false);
                
                // Всегда обновляем быстрый счет на основе сетов
                // Это обеспечивает синхронизацию между окнами
                string newScore1 = setsWon1.ToString();
                string newScore2 = setsWon2.ToString();
                
                if (_match.Team1QuickScore != newScore1 || _match.Team2QuickScore != newScore2)
                {
                    _match.Team1QuickScore = newScore1;
                    _match.Team2QuickScore = newScore2;
                    OnPropertyChanged(nameof(Team1QuickScore));
                    OnPropertyChanged(nameof(Team2QuickScore));
                }
            }
            else
            {
                // Если сетов нет, но быстрый счет есть, оставляем быстрый счет как есть
                // Это позволяет использовать быстрый счет без сетов
            }
        }


        /// <summary>
        /// Обработчик изменений свойств Match для синхронизации UI
        /// </summary>
        private void OnMatchPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Match.Team1QuickScore) || e.PropertyName == nameof(Match.Team2QuickScore))
            {
                // Если быстрый счет изменился извне (например, из TournamentWindow)
                // и есть сеты, синхронизируем быстрый счет обратно на основе сетов
                if (SetsList.Count > 0)
                {
                    SyncSetsToQuickScore();
                }
                else
                {
                    // Если сетов нет, просто обновляем UI
                    OnPropertyChanged(nameof(Team1QuickScore));
                    OnPropertyChanged(nameof(Team2QuickScore));
                }
            }
            else if (e.PropertyName == nameof(Match.Status))
            {
                OnPropertyChanged(nameof(Status));
            }
        }

        private void AddSet()
        {
            var newSet = new SetScore { Number = SetsList.Count + 1 };
            newSet.PropertyChanged += (s, e) => RaiseCalculatedProperties();
            SetsList.Add(newSet);
        }

        private void RemoveSet(object parameter)
        {
            if (parameter is SetScore set)
            {
                SetsList.Remove(set);

                for (int i = 0; i < SetsList.Count; i++)
                    SetsList[i].Number = i + 1;

                RaiseCalculatedProperties();
            }
        }

        private bool CanRemoveSet(object parameter) => parameter is SetScore;

        private void Save()
        {
            ErrorMessage = string.Empty;

            // Валидация номера матча
            if (MatchNumber.HasValue)
            {
                // Загружаем актуальный список матчей для проверки уникальности
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

            SaveSetsToString();
            
            // Синхронизируем быстрый счет со счетом по сетам перед сохранением
            if (SetsList.Count > 0)
            {
                SyncSetsToQuickScore();
            }

            if (SetsList.Any() ||
                !string.IsNullOrWhiteSpace(_match.Team1QuickScore) ||
                !string.IsNullOrWhiteSpace(_match.Team2QuickScore))
            {
                _match.Status = "Сыгран";
            }

            RequestClose?.Invoke();
        }

        private void Cancel()
        {
            RequestClose?.Invoke();
        }
    }

    // Класс одного сета (можно вынести в отдельный файл позже)
    public class SetScore : INotifyPropertyChanged
    {
        public int Number { get; set; }

        private int _score1;
        public int Score1
        {
            get => _score1;
            set { _score1 = value; OnPropertyChanged(); }
        }

        private int _score2;
        public int Score2
        {
            get => _score2;
            set { _score2 = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}