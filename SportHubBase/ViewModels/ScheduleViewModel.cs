using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services.Scheduling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace SportHubBase.ViewModels
{
    public enum MatchFilterType
    {
        Все,
        Сыгранные,
        Не_сыгранные
    }

    public class RoundViewModel : BaseViewModel
    {
        private readonly ScheduleViewModel _parent;
        public int RoundNumber { get; }
        public string Name => $"Тур {RoundNumber}";
        
        public ObservableCollection<Match> Matches { get; } = new ObservableCollection<Match>();
        public ICollectionView FilteredMatches { get; }

        public bool IsGenerated => Matches.Count > 0;
        public bool IsNotGenerated => !IsGenerated;

        public ICommand GenerateRoundCommand { get; }

        public RoundViewModel(int roundNumber, ScheduleViewModel parent)
        {
            RoundNumber = roundNumber;
            _parent = parent;
            GenerateRoundCommand = new RelayCommand(_ => _parent.GenerateSpecificRound(roundNumber));
            
            FilteredMatches = CollectionViewSource.GetDefaultView(Matches);
            FilteredMatches.Filter = _parent.FilterMatch;
        }

        public void AddMatch(Match match)
        {
            Matches.Add(match);
            OnPropertyChanged(nameof(IsGenerated));
            OnPropertyChanged(nameof(IsNotGenerated));
        }

        public void ClearMatches()
        {
            Matches.Clear();
            OnPropertyChanged(nameof(IsGenerated));
            OnPropertyChanged(nameof(IsNotGenerated));
        }

        public void RefreshFilter()
        {
            FilteredMatches.Refresh();
        }
    }

    public class ScheduleViewModel : BaseViewModel
    {
        private Tournament _tournament;
        private readonly IStorage _storage;
        private readonly IScheduleStrategyFactory _scheduleFactory;
        private readonly IMatchService _matchService;
        private Dictionary<Guid, BracketMatch> _bracketMatchesMap = new Dictionary<Guid, BracketMatch>();

        private string _searchQuery;
        private MatchFilterType _filter;
        private string _scheduleMessage;

        public ObservableCollection<Match> Matches { get; } = new ObservableCollection<Match>();
        public ICollectionView FilteredMatches { get; }

        public ObservableCollection<RoundViewModel> Rounds { get; } = new ObservableCollection<RoundViewModel>();
        
        private RoundViewModel _selectedRound;
        public RoundViewModel SelectedRound
        {
            get => _selectedRound;
            set
            {
                if (_selectedRound != value)
                {
                    _selectedRound = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsGroupedView => _tournament?.Type == "Олимпийский";

        public bool IsSequentialSchedule => _scheduleFactory.GetStrategy(_tournament?.Type) is ISequentialScheduleStrategy;
        
        public int NextRoundNumber
        {
            get
            {
                if (_tournament?.Matches == null || _tournament.Matches.Count == 0) return 1;
                return _tournament.Matches.Max(m => m.Round) + 1;
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged();
                    FilteredMatches.Refresh();
                    foreach(var round in Rounds) round.RefreshFilter();
                }
            }
        }

        public MatchFilterType Filter
        {
            get => _filter;
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    OnPropertyChanged();
                    FilteredMatches.Refresh();
                    foreach(var round in Rounds) round.RefreshFilter();
                }
            }
        }

        public string ScheduleMessage
        {
            get => _scheduleMessage;
            private set
            {
                if (_scheduleMessage != value)
                {
                    _scheduleMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // Статистика
        public int TotalMatches => Matches.Count;
        public int Played => Matches.Count(m => m.Status == "Сыгран" || m.Status == "Техническое поражение");
        public int NotPlayed => Matches.Count(m => m.Status != "Сыгран" && m.Status != "Техническое поражение");
        public int TotalEncounters => TotalMatches; // Пока то же самое

        // Свойство для доступа из UI к списку фильтров
        public IEnumerable<MatchFilterType> FilterOptions => Enum.GetValues(typeof(MatchFilterType)).Cast<MatchFilterType>();

        // Команда для обновления (если нужно)
        public ICommand RefreshCommand { get; }


        
        // Команда открытия матча (делегируется TournamentViewModel или через событие, но здесь просто ICommand для биндинга)
        // В текущей архитектуре TournamentViewModel открывает окна, поэтому здесь мы можем просто пробрасывать вызов
        public Action<Match> OnOpenMatchRequest;
        public ICommand OpenMatchCommand { get; }

        public ScheduleViewModel(
            Tournament tournament, 
            IStorage storage, 
            IScheduleStrategyFactory scheduleFactory, 
            IMatchService matchService)
        {
            _tournament = tournament ?? throw new ArgumentNullException(nameof(tournament));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _scheduleFactory = scheduleFactory ?? throw new ArgumentNullException(nameof(scheduleFactory));
            _matchService = matchService ?? throw new ArgumentNullException(nameof(matchService));

            FilteredMatches = CollectionViewSource.GetDefaultView(Matches);
            FilteredMatches.Filter = FilterMatch;

            RefreshCommand = new RelayCommand(_ => LoadMatches());
            OpenMatchCommand = new RelayCommand(param => 
            {
                if (param is Match match) OnOpenMatchRequest?.Invoke(match);
            });

            LoadMatches();
        }

        public void LoadMatches()
        {
            Matches.Clear();
            Rounds.Clear();
            ScheduleMessage = string.Empty;

            if (_tournament.Teams == null || _tournament.Teams.Count < 2)
            {
                ScheduleMessage = "Недостаточно команд для расписания.";
                UpdateStats();
                return;
            }

            // Инициализация туров для Швейцарской системы
            if (IsSequentialSchedule)
            {
                // Используем TotalRounds из турнира. Если не задано (0), по умолчанию 7.
                int totalRoundsToCreate = _tournament.TotalRounds > 0 ? _tournament.TotalRounds : 7;
                for (int i = 1; i <= totalRoundsToCreate; i++)
                {
                    Rounds.Add(new RoundViewModel(i, this));
                }

                if (Rounds.Any())
                {
                    SelectedRound = Rounds.First();
                }
            }

            // Используем логику из TournamentViewModel для генерации/загрузки
            // Но лучше если мы просто берем из турнира, если генерация уже была?
            // Турнир уже содержит матчи в _tournament.Matches?
            
            // Всегда пробуем сгенерировать расписание для актуального списка команд
            IScheduleStrategy strategy = _scheduleFactory.GetStrategy(_tournament.Type);
            if (strategy == null)
            {
                ScheduleMessage = $"Формат \"{_tournament.Type}\" не поддерживается.";
                return;
            }
            if (!strategy.IsImplemented)
            {
                ScheduleMessage = $"Формат \"{strategy.Name}\" — расписание в разработке.";
                
                // Если стратегия не реализована, но матчи есть (загружены), покажем их
                if (_tournament.Matches != null)
                {
                     foreach (var match in _tournament.Matches) Matches.Add(match);
                }
                return;
            }

            IEnumerable<Match> generatedMatches;

            if (strategy is ISequentialScheduleStrategy seqStrategy)
            {
                // По-туровая стратегия не генерирует все раунды сразу, она просто берет историю
                generatedMatches = Enumerable.Empty<Match>();
                
                // Автоматическая генерация первого тура, если матчей еще нет
                if (_tournament.Matches == null || _tournament.Matches.Count == 0)
                {
                    try
                    {
                        var firstRoundMatches = seqStrategy.GenerateNextRound(_tournament, 1);
                        if (firstRoundMatches != null && firstRoundMatches.Any())
                        {
                            if (_tournament.Matches == null)
                                _tournament.Matches = new List<Match>();
                            
                            foreach (var match in firstRoundMatches)
                            {
                                _tournament.Matches.Add(match);
                            }
                            
                            _matchService.AssignAutoNumbers(_tournament.Matches);
                            _storage.UpdateTournament(_tournament);
                            
                            // Мы не вызываем LoadMatches рекурсивно, просто устанавливаем generatedMatches, 
                            // чтобы нижний код обработал их как сгенерированные
                            // Но поскольку SequentialScheduleStrategy распределяет по Rounds,
                            // нам достаточно добавить их в _tournament.Matches, и нижний код их подхватит.
                        }
                    }
                    catch (Exception ex)
                    {
                        ScheduleMessage = $"Ошибка при автогенерации 1-го тура: {ex.Message}";
                    }
                }
            }
            else
            {
                try
                {
                    generatedMatches = strategy.GenerateSchedule(_tournament.Teams.ToList()) ?? Enumerable.Empty<Match>();
                }
                catch (Exception)
                {
                    ScheduleMessage = "Произошла ошибка при генерации расписания.";
                    return;
                }
            }

            // Мерджим с существующими матчами (сохраняем результаты)
            var savedMatchesDict = new Dictionary<string, Match>();
            if (_tournament.Matches != null)
            {
                foreach (var savedMatch in _tournament.Matches)
                {
                    // Ключ не должен зависеть от раунда, так как при добавлении команд раунды смещаются.
                    // Команды упорядочиваем по алфавиту для уникальности пары.
                    var teams = new[] { savedMatch.Team1, savedMatch.Team2 }.OrderBy(t => t).ToArray();
                    string key = $"{teams[0]}|{teams[1]}";
                    
                    if (!savedMatchesDict.ContainsKey(key))
                        savedMatchesDict[key] = savedMatch;
                }
            }

            var finalMatches = new List<Match>();
            foreach (var generatedMatch in generatedMatches)
            {
                var teams = new[] { generatedMatch.Team1, generatedMatch.Team2 }.OrderBy(t => t).ToArray();
                string key = $"{teams[0]}|{teams[1]}";

                if (savedMatchesDict.TryGetValue(key, out Match savedMatch))
                {
                    // Переносим данные из сохранённого
                    generatedMatch.Id = savedMatch.Id;
                    generatedMatch.Status = savedMatch.Status;
                    generatedMatch.Team1QuickScore = savedMatch.Team1QuickScore;
                    generatedMatch.Team2QuickScore = savedMatch.Team2QuickScore;
                    generatedMatch.SetsScore = savedMatch.SetsScore;
                    generatedMatch.SetsBySet = savedMatch.SetsBySet;
                    generatedMatch.TotalScore = savedMatch.TotalScore; // Исправлено: TotalScore был decimal в модели? Нет string.
                    generatedMatch.Duration = savedMatch.Duration;
                    generatedMatch.Referee = savedMatch.Referee;
                    generatedMatch.Location = savedMatch.Location;
                    generatedMatch.Mvp = savedMatch.Mvp;
                    generatedMatch.MatchNumber = savedMatch.MatchNumber; // Сохраняем номер
                }
                finalMatches.Add(generatedMatch);
            }

            // Обновляем турнир
            _tournament.Matches = finalMatches;
            _storage.UpdateTournament(_tournament);
            
            // Автонумерация (для новых матчей)
            _matchService.AssignAutoNumbers(_tournament.Matches);

            // Разносим по раундам для Sequential
            if (IsSequentialSchedule)
            {
                foreach (var roundVM in Rounds)
                {
                    roundVM.ClearMatches();
                }

                foreach (var match in _tournament.Matches)
                {
                    var roundVM = Rounds.FirstOrDefault(r => r.RoundNumber == match.Round);
                    if (roundVM != null)
                    {
                        roundVM.AddMatch(match);
                        Matches.Add(match); // Добавляем и в общий плоский список на всякий случай
                    }
                }
            }
            else
            {
                // Заполняем ObservableCollection для других типов
                if (IsGroupedView)
            {
                _bracketMatchesMap.Clear();
                if (_tournament.Bracket != null)
                {
                    int number = 1;
                    foreach (var round in _tournament.Bracket.Rounds)
                    {
                        foreach (var bMatch in round.Matches)
                        {
                            if (bMatch.IsBye) continue;

                            var m = new Match
                            {
                                Id = bMatch.Id,
                                RoundName = round.Name,
                                Team1 = bMatch.DisplayTeam1,
                                Team2 = bMatch.DisplayTeam2,
                                Team1QuickScore = bMatch.Score1,
                                Team2QuickScore = bMatch.Score2,
                                Status = bMatch.IsCompleted ? "Сыгран" : "Не сыгран",
                                MatchNumber = number++
                            };
                            _bracketMatchesMap[m.Id] = bMatch;
                            Matches.Add(m);
                        }
                    }
                    if (_tournament.Bracket.BronzeMatch != null)
                    {
                        var bMatch = _tournament.Bracket.BronzeMatch;
                        if (!bMatch.IsBye)
                        {
                            var m = new Match
                            {
                                Id = bMatch.Id,
                                RoundName = "Матч за 3-е место",
                                Team1 = bMatch.DisplayTeam1,
                                Team2 = bMatch.DisplayTeam2,
                                Team1QuickScore = bMatch.Score1,
                                Team2QuickScore = bMatch.Score2,
                                Status = bMatch.IsCompleted ? "Сыгран" : "Не сыгран",
                                MatchNumber = number++
                            };
                            _bracketMatchesMap[m.Id] = bMatch;
                            Matches.Add(m);
                        }
                    }
                }

                FilteredMatches.GroupDescriptions.Clear();
                FilteredMatches.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Match.RoundName)));
            }
            else
            {
                FilteredMatches.GroupDescriptions.Clear();
                foreach (var match in _tournament.Matches)
                {
                    Matches.Add(match);
                }
            }
            } // Закрываем блок else для IsSequentialSchedule

            foreach (var match in Matches)
            {
                // Подписываемся на изменения
                match.PropertyChanged -= Match_PropertyChanged;
                match.PropertyChanged += Match_PropertyChanged;
            }

            if (Matches.Count == 0)
            {
                ScheduleMessage = "Не удалось сгенерировать расписание (возможно, мало команд).";
            }

            FilteredMatches.Refresh(); // Принудительно обновляем View, чтобы UI отобразил новые матчи
            OnPropertyChanged(nameof(IsGroupedView));
            UpdateStats();
        }

        private void Match_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
             if (e.PropertyName == nameof(Match.Status))
             {
                 UpdateStats();
                 FilteredMatches.Refresh(); // Обновить фильтр, если статус изменился
                 foreach(var r in Rounds) r.RefreshFilter();
             }
             // Если меняется номер матча - тоже обновить фильтрацию/поиск
             if (e.PropertyName == nameof(Match.MatchNumber))
             {
                 FilteredMatches.Refresh();
                 foreach(var r in Rounds) r.RefreshFilter();
             }
             
             if (IsGroupedView && (e.PropertyName == nameof(Match.Team1QuickScore) || e.PropertyName == nameof(Match.Team2QuickScore)))
             {
                 if (sender is Match m && _bracketMatchesMap.TryGetValue(m.Id, out var bMatch))
                 {
                     bMatch.Score1 = m.Team1QuickScore;
                     bMatch.Score2 = m.Team2QuickScore;
                 }
             }
        }

        public void UpdateStats()
        {
            OnPropertyChanged(nameof(TotalMatches));
            OnPropertyChanged(nameof(Played));
            OnPropertyChanged(nameof(NotPlayed));
            OnPropertyChanged(nameof(TotalEncounters));
        }

        public bool FilterMatch(object item)
        {
            if (!(item is Match match)) return false;

            // 1. Фильтр по типу
            bool typeMatch = true;
            switch (_filter)
            {
                case MatchFilterType.Сыгранные:
                    typeMatch = match.Status == "Сыгран" || match.Status == "Техническое поражение";
                    break;
                case MatchFilterType.Не_сыгранные:
                    typeMatch = match.Status != "Сыгран" && match.Status != "Техническое поражение";
                    break;
            }

            if (!typeMatch) return false;

            // 2. Поиск
            if (string.IsNullOrWhiteSpace(_searchQuery)) return true;

            string q = _searchQuery.Trim(); // Case insensitive is default usually? BaseViewModel doesn't specify.
            // Используем StringComparison.OrdinalIgnoreCase как в правилах

            if (match.Team1 != null && match.Team1.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (match.Team2 != null && match.Team2.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (match.MatchNumber.HasValue && match.MatchNumber.Value.ToString().Contains(q)) return true;

            return false;
        }
        public void UpdateTournamentReference(Tournament tournament)
        {
            _tournament = tournament ?? throw new ArgumentNullException(nameof(tournament));
            // Также обновляем matches, если они изменились в новом объекте
            // Но LoadMatches() все равно перезагрузит их
            OnPropertyChanged(nameof(IsSequentialSchedule));
            OnPropertyChanged(nameof(NextRoundNumber));
        }

        public void GenerateSpecificRound(int roundNumber)
        {
            try
            {
                var strategy = _scheduleFactory.GetStrategy(_tournament.Type) as ISequentialScheduleStrategy;
                if (strategy == null) return;

                // 1. Проверка: завершен ли предыдущий тур (если это не первый тур)
                if (roundNumber > 1)
                {
                    var previousRoundMatches = _tournament.Matches?.Where(m => m.Round == roundNumber - 1).ToList();
                    if (previousRoundMatches != null && previousRoundMatches.Any(m => m.Status != "Сыгран" && m.Status != "Техническое поражение"))
                    {
                        System.Windows.MessageBox.Show(
                            $"Невозможно сгенерировать тур {roundNumber}, пока не завершены все матчи тура {roundNumber - 1}.",
                            "Генерация пар",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                }
                
                // 2. Очистка старых матчей этого тура, если они были (перегенерация)
                if (_tournament.Matches != null)
                {
                    _tournament.Matches.RemoveAll(m => m.Round == roundNumber);
                }

                // 3. Генерация
                var newMatches = strategy.GenerateNextRound(_tournament, roundNumber);
                if (newMatches == null || !newMatches.Any())
                {
                    System.Windows.MessageBox.Show(
                        $"Невозможно сгенерировать тур {roundNumber}. Проверьте количество команд и историю встреч.",
                        "Генерация тура",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (_tournament.Matches == null)
                {
                    _tournament.Matches = new List<Match>();
                }

                foreach (var match in newMatches)
                {
                    _tournament.Matches.Add(match);
                }
                
                _matchService.AssignAutoNumbers(_tournament.Matches);
                _storage.UpdateTournament(_tournament);
                LoadMatches();

                System.Windows.MessageBox.Show(
                    $"Тур {roundNumber} успешно сформирован. Пары созданы.",
                    "Успех",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при генерации тура: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
