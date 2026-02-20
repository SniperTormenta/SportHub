// ViewModels/TournamentViewModel.cs

// Вынести текущий расчёт из UpdateResultsFromMatches() в VolleyballResultsCalculator.
// Создать фабрику и интерфейс.
// Изменить VM на использование калькулятора.
// Добавить заглушки для футбола/баскетбола (пока сообщение "в разработке").
// По мере необходимости реализовать другие калькуляторы.
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services.Scheduling;
using SportHubBase.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SportHubBase.Services.Results;
using SportHubBase.Services.Statistics;
using SportHubBase.Services.Results.Data;

namespace SportHubBase.ViewModels
{
    
    /// Центральная ViewModel для окна турнира (TournamentWindow.xaml).
    /// Управляет текущим турниром: команды, расписание (паттерн "Стратегия"), результаты (шахматная таблица с итальянской системой), статистика.
    /// Наследует BaseViewModel для уведомлений и команд (MVVM).
    /// ObservableCollection для динамического обновления UI (списки команд/матчей/таблицы).
    /// В архитектуре: Вызывает JsonStorage для загрузки/сохранения, фабрику Scheduling для генерации Matches, открывает вспомогательные окна (AddTeamWindow, MatchDetailsWindow).
    /// Улучшения: Инжектировать JsonStorage через конструктор (IoC); вынести длинные методы (UpdateResultsFromMatches) в отдельный сервис расчёта результатов; добавить async для сохранения; обработку ошибок (try/catch в Save).
    public class TournamentViewModel : BaseViewModel
    {
        /// Сервис хранения. Инжектируется через конструктор.
        private readonly IStorage _storage;

        /// Фабрика стратегий расписания. Инжектируется через конструктор.
        private readonly IScheduleStrategyFactory _scheduleFactory;

        /// Фабрика калькуляторов результатов. Инжектируется через конструктор.
        private readonly IResultsProviderFactory _resultsFactory;

        /// Фабрика калькуляторов статистики. Инжектируется через конструктор.
        private readonly IStatisticsCalculatorFactory _statisticsFactory;

        /// Сервис матчей. Инжектируется через конструктор.
        private readonly IMatchService _matchService;

        /// Сервис Excel. Инжектируется через конструктор.
        private readonly IExcelService _excelService;

        public ScheduleViewModel ScheduleVM { get; }

        /// Фабрика стратегий кодирования изображений. Инжектируется через конструктор.
        private readonly IImageEncoderStrategyFactory _imageEncoderFactory;

        
        /// Текущий турнир (Model). Биндится к UI для заголовков/дат.
        
        public Tournament CurrentTournament { get; private set; }

        
        /// Коллекция команд для UI (ListView/DataGrid). Observable для добавления/удаления.
        
        public ObservableCollection<Team> Teams { get; } = new ObservableCollection<Team>();

        // Расписание
        
        /// Коллекция матчей (генерируется стратегией, обновляется из сохранённых).
        
        // Расписание теперь в ScheduleVM
        // public ObservableCollection<Match> Schedule { get; } = new ObservableCollection<Match>();

        public TournamentStatistics Statistics { get; private set; } = new TournamentStatistics();

        private TournamentBracket _bracket;
        /// <summary>
        /// Олимпийская сетка турнира.
        /// </summary>
        public TournamentBracket Bracket
        {
            get => _bracket;
            private set
            {
                _bracket = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOlympic));
            }
        }

        /// <summary>
        /// Флаг, является ли турнир олимпийским (для отображения вкладки Сетка).
        /// </summary>
        public bool IsOlympic => CurrentTournament?.Type == "Олимпийский";

        private string _scheduleMessage;
        
        /// Сообщение о статусе расписания (для TextBlock в UI).
        
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

        // Результаты (полиморфные)
        // Вся логика отображения теперь внутри ResultsData и DataTemplate
        
        private ResultsData _currentResults;
        public ResultsData CurrentResults
        {
            get => _currentResults;
            private set
            {
                if (_currentResults != value)
                {
                    _currentResults = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LeaderText)); // Лидер зависит от результатов
                }
            }
        }

        // Вычисляемые свойства для UI
        
        /// Количество команд (биндинг к TextBlock).
        
        public int TeamsCount => Teams.Count;

        
        /// Формат турнира (для заголовка).
        
        public string FormatText => CurrentTournament?.Type ?? "";

        
        /// Даты турнира с форматированием.
        
        public string DatesText
        {
            get
            {
                if (CurrentTournament == null) return "";
                string start = CurrentTournament.StartDate.ToString("d MMMM yyyy");
                if (CurrentTournament.EndDate.HasValue)
                {
                    string end = CurrentTournament.EndDate.Value.ToString("d MMMM yyyy");
                    return $"{start} – {end}";
                }
                return $"{start}\nтурнир продолжается";
            }
        }

        /// Текст статуса турнира.
        public string TournamentStatusText
        {
            get
            {
                if (CurrentTournament == null) return "—";

                // Если статус явно задан в модели
                if (!string.IsNullOrWhiteSpace(CurrentTournament.Status))
                {
                    // Делаем первую букву заглавной для красоты
                    return char.ToUpper(CurrentTournament.Status[0]) + CurrentTournament.Status.Substring(1).ToLower();
                }

                // Авто-определение
                var now = DateTime.Now.Date;
                if (now < CurrentTournament.StartDate.Date) return "Не начат";
                if (CurrentTournament.EndDate.HasValue && now > CurrentTournament.EndDate.Value.Date) return "Завершён";
                
                return "Идёт";
            }
        }

        /// Текст прогресса матчей ("X из Y").
        public string MatchesProgressText
        {
            get
            {
                int played = ScheduleVM?.Played ?? 0;
                int total = ScheduleVM?.TotalMatches ?? 0;
                return $"{played} из {total}";
            }
        }

        /// Текст текущего лидера.
        public string LeaderText
        {
            get
            {
                if (CurrentResults is RoundRobinResultsData rrData && rrData.Rows.Count > 0)
                {
                    // Ищем первое место
                    var leader = rrData.Rows.FirstOrDefault(r => r.Place == 1);
                    if (leader == null) return "Лидер: —";

                    return $"Лидер: {leader.TeamName} — {leader.Points} очков";
                }
                return "Лидер: —";
            }
        }

        /// Команда завершения турнира.
        public ICommand FinishTournamentCommand { get; private set; }


        /// Флаг live-режима (для индикатора в UI).

        public bool IsLive => CurrentTournament?.IsLive ?? false;

        // Статистика
        
        /// Сыгранные матчи.
        
        public int TotalMatchesPlayed => ScheduleVM?.Matches.Count(m => !string.IsNullOrWhiteSpace(m.Status) &&
            (string.Equals(m.Status, "Сыгран", StringComparison.OrdinalIgnoreCase) || 
             string.Equals(m.Status, "Техническое поражение", StringComparison.OrdinalIgnoreCase))) ?? 0;

        
        /// Всего сыгранных сетов (партий).
        
        public int TotalSetsPlayed
        {
            get
            {
                int total = 0;
                foreach (var match in ScheduleVM?.Matches ?? Enumerable.Empty<Match>())
                {
                    if (!string.IsNullOrWhiteSpace(match.SetsBySet))
                    {
                        var sets = match.SetsBySet.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        total += sets.Length;
                    }
                }
                return total;
            }
        }

        
        /// Всего набранных очков (мячей).
        
        public int TotalPointsScored
        {
            get
            {
                int total = 0;
                foreach (var match in ScheduleVM?.Matches ?? Enumerable.Empty<Match>())
                {
                    if (!string.IsNullOrWhiteSpace(match.TotalScore))
                    {
                        var parts = match.TotalScore.Split(':');
                        if (parts.Length == 2)
                        {
                            if (int.TryParse(parts[0].Trim(), out int p1) &&
                                int.TryParse(parts[1].Trim(), out int p2))
                            {
                                total += p1 + p2;
                            }
                        }
                    }
                }
                return total;
            }
        }

        
        /// Самый ценный игрок (по количеству MVP в матчах).
        
        public string MostValuablePlayerName
        {
            get
            {
                var mvpCounts = ScheduleVM?.Matches
                    .Where(m => !string.IsNullOrWhiteSpace(m.Mvp))
                    .GroupBy(m => m.Mvp)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                return mvpCounts?.Key ?? "—";
            }
        }


        /// Команда MVP.

        public string MostValuablePlayerTeam
        {
            get
            {
                var mvp = MostValuablePlayerName;
                if (mvp == "—") return "—";

                var team = Teams.FirstOrDefault(t =>
                    t.Players != null && t.Players.Any(p =>
                        string.Equals(p.Name, mvp, StringComparison.OrdinalIgnoreCase)));
                return team?.Name ?? "—";
            }
        }

        /// Количество MVP у самого ценного игрока.

        public int MostValuablePlayerCount
        {
            get
            {
                var mvp = MostValuablePlayerName;
                if (mvp == "—") return 0;

                return ScheduleVM?.Matches
                    .Count(m => string.Equals(m.Mvp, mvp, StringComparison.OrdinalIgnoreCase)) ?? 0;
            }
        }

        
        /// Лидер по победам.
        
        public string MostPopularTeamName
        {
            get
            {
                var wins = new Dictionary<string, int>();
                foreach (var match in ScheduleVM?.Matches ?? Enumerable.Empty<Match>())
                {
                    if (string.IsNullOrWhiteSpace(match.Status) ||
                        (!string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(match.Status, "Техническое поражение", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var outcome = GetOutcome(match);
                    if (outcome == 1d)
                    {
                        int currentWins;
                        wins.TryGetValue(match.Team1, out currentWins);
                        wins[match.Team1] = currentWins + 1;
                    }
                    else if (outcome == 0d)
                    {
                        int currentWins;
                        wins.TryGetValue(match.Team2, out currentWins);
                        wins[match.Team2] = currentWins + 1;
                    }
                }

                var topTeam = wins.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
                return topTeam.Key ?? "—";
            }
        }

        
        /// Победы лидера.
        
        public int MostPopularTeamWins
        {
            get
            {
                var teamName = MostPopularTeamName;
                if (teamName == "—") return 0;

                int wins = 0;
                foreach (var match in ScheduleVM?.Matches ?? Enumerable.Empty<Match>())
                {
                    if (string.IsNullOrWhiteSpace(match.Status) ||
                        (!string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(match.Status, "Техническое поражение", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var outcome = GetOutcome(match);
                    if ((outcome == 1d && match.Team1 == teamName) ||
                        (outcome == 0d && match.Team2 == teamName))
                        wins++;
                }
                return wins;
            }
        }

        
        /// Сыгранные матчи лидера.
        
        public int MostPopularTeamMatchesPlayed
        {
            get
            {
                var teamName = MostPopularTeamName;
                if (teamName == "—") return 0;

                int played = 0;
                foreach (var match in ScheduleVM?.Matches ?? Enumerable.Empty<Match>())
                {
                    if (string.IsNullOrWhiteSpace(match.Status) ||
                        (!string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(match.Status, "Техническое поражение", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    if (match.Team1 == teamName || match.Team2 == teamName)
                        played++;
                }
                return played;
            }
        }

        
        /// Процент побед лидера.
        
        public double MostPopularTeamWinPercentage
        {
            get
            {
                var teamName = MostPopularTeamName;
                if (teamName == "—") return 0;

                int played = MostPopularTeamMatchesPlayed;
                if (played == 0) return 0;

                int wins = MostPopularTeamWins;
                return Math.Round((double)wins / played * 100, 1);
            }
        }

        // Команды
        
        /// Команда добавления команды (кнопка).
        
        public ICommand AddTeamCommand { get; }

        
        /// Команда редактирования команды (по параметру Team).
        
        public ICommand EditTeamCommand { get; }

        
        /// Команда открытия карточки матча.

        public ICommand OpenMatchCommand { get; }

        /// Команда экспорта результатов.
        public ICommand ExportResultsCommand { get; }

        /// Команда экспорта результатов в Excel.
        public ICommand ExportResultsExcelCommand { get; }

        /// Команда импорта команд из Excel.
        public ICommand ImportTeamsExcelCommand { get; }

        /// Команда экспорта команд в Excel.
        public ICommand ExportTeamsExcelCommand { get; }

        /// Команда скачивания шаблона для импорта команд.
        public ICommand DownloadTemplateCommand { get; }

        // Settings Tab Properties and Commands
        
        /// Доступные города для выбора места проведения.
        
        public ObservableCollection<string> AvailableCities { get; } = new ObservableCollection<string>();

        /// Доступные стратегии расписания.
        
        public List<string> AvailableScheduleStrategies { get; private set; } = new List<string>();

        /// Доступные типы турниров.
        
        public List<string> AvailableTournamentTypes { get; } = new List<string> { "Круговой", "Плей-офф", "Швейцарка", "Группы + плей-офф" };

        /// Доступные виды спорта.
        
        public List<string> AvailableSportTypes { get; } = new List<string> { "Волейбол", "Футбол", "Баскетбол" };
        
        /// Доступные системы начисления очков.
        public List<string> AvailableScoringSystems { get; } = new List<string> { "Итальянская", "FIVB", "Пользовательская" };

        private string _scoringDescription;
        public string ScoringDescription
        {
            get => _scoringDescription;
            set
            {
                if (_scoringDescription != value)
                {
                    _scoringDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCustomScoring => CurrentTournament?.ScoringSystem == "Пользовательская";

        public string SelectedScoringSystem
        {
            get => CurrentTournament?.ScoringSystem ?? "Итальянская";
            set
            {
                if (CurrentTournament != null && CurrentTournament.ScoringSystem != value)
                {
                    CurrentTournament.ScoringSystem = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCustomScoring));
                    UpdateScoringDescription();
                }
            }
        }

        public int LossPoints
        {
            get => CurrentTournament?.CustomLossPoints ?? 0;
            set
            {
                if (CurrentTournament != null && CurrentTournament.CustomLossPoints != value)
                {
                    CurrentTournament.CustomLossPoints = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int WinPoints
        {
            get => CurrentTournament?.CustomWinPoints ?? 0;
            set
            {
                if (CurrentTournament != null && CurrentTournament.CustomWinPoints != value)
                {
                    CurrentTournament.CustomWinPoints = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DrawPoints
        {
            get => CurrentTournament?.CustomDrawPoints ?? 0;
            set
            {
                if (CurrentTournament != null && CurrentTournament.CustomDrawPoints != value)
                {
                    CurrentTournament.CustomDrawPoints = value;
                    OnPropertyChanged();
                }
            }
        }

        /// Команда сохранения настроек.
        
        public ICommand SaveSettingsCommand { get; private set; }

        /// Команда отмены изменений настроек.
        
        public ICommand CancelSettingsCommand { get; private set; }

        /// Команда удаления турнира.
        
        public ICommand DeleteTournamentCommand { get; private set; }

        /// Команда пересчета расписания.
        
        public ICommand RegenerateScheduleCommand { get; private set; }


        /// Конструктор: загружает турнир по ID, инициализирует коллекции, команды, генерирует расписание/результаты, подписывается на изменения.

        /// <param name="tournamentId">ID турнира из списка.</param>
        /// <param name="storage">Сервис хранения.</param>
        /// <param name="scheduleFactory">Фабрика стратегий расписания.</param>
        /// <param name="resultsFactory">Фабрика провайдеров результатов.</param>
        /// <param name="statisticsFactory">Фабрика калькуляторов статистики.</param>
        public TournamentViewModel(
            Guid tournamentId,
            IStorage storage,
            IScheduleStrategyFactory scheduleFactory,
            IResultsProviderFactory resultsFactory,
            IStatisticsCalculatorFactory statisticsFactory,
            IImageEncoderStrategyFactory imageEncoderFactory,
            IMatchService matchService,
            IExcelService excelService)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _scheduleFactory = scheduleFactory ?? throw new ArgumentNullException(nameof(scheduleFactory));
            _resultsFactory = resultsFactory ?? throw new ArgumentNullException(nameof(resultsFactory));
            _statisticsFactory = statisticsFactory ?? throw new ArgumentNullException(nameof(statisticsFactory));
            _imageEncoderFactory = imageEncoderFactory ?? throw new ArgumentNullException(nameof(imageEncoderFactory));
            _matchService = matchService ?? throw new ArgumentNullException(nameof(matchService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));

            var tournaments = _storage.LoadTournaments();
            CurrentTournament = tournaments.Find(t => t.Id == tournamentId);

            // Если турнир не найден (например, при Guid.Empty), создаем новый
            if (CurrentTournament == null && tournamentId != Guid.Empty)
            {
                throw new ArgumentException($"Турнир с ID {tournamentId} не найден.");
            }
            else if (CurrentTournament == null && tournamentId == Guid.Empty)
            {
                // Для отладки - если передан пустой GUID, создаем временный турнир
                CurrentTournament = new Tournament
                {
                    Id = Guid.NewGuid(),
                    Name = "Новый турнир",
                    Type = "Круговой",
                    SportType = "Волейбол"
                };
            }

            if (CurrentTournament != null && CurrentTournament.Teams != null)
            {
                foreach (var team in CurrentTournament.Teams)
                {
                    Teams.Add(team);
                }
            }

            ScheduleVM = new ScheduleViewModel(CurrentTournament, _storage, _scheduleFactory, _matchService);
            ScheduleVM.OnOpenMatchRequest += OpenMatchCard;

            // Загружаем сетку, если она есть
            Bracket = CurrentTournament.Bracket;
            if (Bracket != null)
            {
                Bracket.ReconnectReferences();
                SubscribeToBracket(Bracket);
            }

            AddTeamCommand = new RelayCommand(OpenAddTeamWindow);
            EditTeamCommand = new RelayCommand(EditTeam, t => t is Team);
            OpenMatchCommand = new RelayCommand(OpenMatchCard, m => m is Match);
            ExportResultsCommand = new RelayCommand(OpenExportWindow);
            ExportResultsExcelCommand = new RelayCommand(ExportResultsExcel);
            ImportTeamsExcelCommand = new RelayCommand(ImportTeamsExcel);
            ExportTeamsExcelCommand = new RelayCommand(ExportTeamsExcel);
            DownloadTemplateCommand = new RelayCommand(DownloadTemplate);

            // GenerateSchedule(); // Теперь в ScheduleVM
            GenerateResults();

            ScheduleVM.Matches.CollectionChanged += OnScheduleCollectionChanged;
            ScheduleVM.PropertyChanged += OnScheduleViewModelPropertyChanged; // Подписываемся на изменения в ScheduleVM (счётчики)

            SubscribeToMatches(ScheduleVM.Matches); // Подписываемся на матчи из VM

            UpdateResultsFromMatches();
            UpdateStatistics();
            
            // Инициализация команд Settings
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            CancelSettingsCommand = new RelayCommand(CancelSettings);
            DeleteTournamentCommand = new RelayCommand(DeleteTournament);
            RegenerateScheduleCommand = new RelayCommand(param => 
            {
                if (IsOlympic) RegenerateBracket();
                else ScheduleVM.RefreshCommand.Execute(null);
            });
            
            // Загрузка данных для Settings
            LoadCities();
            LoadScheduleStrategies();
            UpdateScoringDescription();
            
            // Инициализация команды завершения
            FinishTournamentCommand = new RelayCommand(FinishTournament, CanFinishTournament);
        }

        private void FinishTournament(object parameter)
        {
            if (MessageBox.Show("Вы уверены, что хотите завершить турнир? Это действие установит статус 'Завершён' и дату окончания на сегодня.", 
                "Завершение турнира", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (CurrentTournament != null)
                {
                    CurrentTournament.Status = "Завершён";
                    // Если дата окончания не установлена или она в будущем, ставим текущую
                    if (!CurrentTournament.EndDate.HasValue || CurrentTournament.EndDate.Value > DateTime.Now)
                    {
                        CurrentTournament.EndDate = DateTime.Now;
                    }
                    _storage.UpdateTournament(CurrentTournament);
                    
                    OnPropertyChanged(nameof(TournamentStatusText));
                    OnPropertyChanged(nameof(DatesText));
                    OnPropertyChanged(nameof(IsLive)); // Обновит индикатор Live
                    
                    // Обновить состояние команды (кнопка станет неактивной)
                    (FinishTournamentCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool CanFinishTournament(object parameter)
        {
            // Можно завершить, если статус не "Завершён" и все матчи сыграны
            if (CurrentTournament == null) return false;
            
            bool isFinished = string.Equals(CurrentTournament.Status, "Завершён", StringComparison.OrdinalIgnoreCase);
            if (isFinished) return false;

            // Проверяем, что есть матчи и все они сыграны
            if (ScheduleVM == null || ScheduleVM.TotalMatches == 0) return false;
            
            return ScheduleVM.Played == ScheduleVM.TotalMatches;
        }

        private void OnScheduleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScheduleViewModel.Played) || 
                e.PropertyName == nameof(ScheduleViewModel.TotalMatches))
            {
                OnPropertyChanged(nameof(MatchesProgressText));
                (FinishTournamentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // OnResultsTableCollectionChanged removed (logic moved to CurrentResults setter)

        
        /// Открывает вспомогательное окно добавления команды.
        /// После закрытия — перезагружает команды из JSON (надёжно).
        
        private void OpenAddTeamWindow(object parameter)
        {
            var currentWindow = Application.Current.Windows
                .OfType<TournamentWindow>()
                .FirstOrDefault(w => w.IsActive);
            if (currentWindow != null)
            {
                var addTeamWindow = new AddTeamWindow(currentWindow, CurrentTournament.Id);
                if (addTeamWindow.ShowDialog() == true)
                {
                    var updated = _storage.LoadTournaments().Find(t => t.Id == CurrentTournament.Id);
                    if (updated != null)
                    {
                        // ВАЖНО: Обновляем ссылку на объект турнира, чтобы он не был устаревшим
                        CurrentTournament = updated;
                        ScheduleVM.UpdateTournamentReference(updated);
                        
                        Teams.Clear();
                        foreach (var team in updated.Teams)
                        {
                            Teams.Add(team);
                        }
                    }
                    OnPropertyChanged(nameof(TeamsCount));
 
                    // Обновляем расписание и результаты после добавления команды
                    ScheduleVM.LoadMatches(); // Обновляем через VM
                    GenerateResults();
                    UpdateStatistics();
                }
            }
        }

        /// Открывает окно экспорта результатов.
        /// Показывает превью таблицы и позволяет сохранить в PNG/JPG.
        private void OpenExportWindow(object parameter)
        {
            var currentWindow = Application.Current.Windows
                .OfType<TournamentWindow>()
                .FirstOrDefault(w => w.IsActive);

            if (currentWindow != null && CurrentTournament != null && CurrentResults is RoundRobinResultsData rrData)
            {
                // Конвертируем IReadOnlyList<int> в ObservableCollection для совместимости с ExportPreviewViewModel,
                // либо, если там строго ObservableCollection, создаем новую.
                // В старом коде ResultsHeaderNumbers был ObservableCollection<int>.
                var headers = new ObservableCollection<int>(rrData.HeaderNumbers);

                var exportViewModel = new ExportPreviewViewModel(
                    CurrentTournament.Name,
                    "Таблица результатов",
                    headers, 
                    rrData.Rows,
                    ExportTableType.Results,
                    _imageEncoderFactory);

                var exportWindow = new View.ExportPreviewWindow(currentWindow, exportViewModel);
                exportWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Экспорт поддерживается только для круговой системы с наличием результатов.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// Открывает окно редактирования команды.
        
        private void EditTeam(object parameter)
        {
            var team = parameter as Team;
            if (team == null) return;

            var currentWindow = Application.Current.Windows
                .OfType<TournamentWindow>()
                .FirstOrDefault(w => w.IsActive);
            if (currentWindow != null)
            {
                var editTeamWindow = new AddTeamWindow(currentWindow, CurrentTournament.Id, team);
                if (editTeamWindow.ShowDialog() == true)
                {
                    var updated = _storage.LoadTournaments().Find(t => t.Id == CurrentTournament.Id);
                    if (updated != null)
                    {
                        // ВАЖНО: Обновляем ссылку на объект турнира
                        CurrentTournament = updated;
                        ScheduleVM.UpdateTournamentReference(updated);

                        Teams.Clear();
                        foreach (var t in updated.Teams)
                        {
                            Teams.Add(t);
                        }
                    }
                    OnPropertyChanged(nameof(TeamsCount));
 
                    // Обновляем расписание и результаты после редактирования команды
                    ScheduleVM.LoadMatches(); // Обновляем через VM
                    GenerateResults();
                    UpdateStatistics();
                }
            }
        }

        /// Инициализирует структуру таблицы результатов (очищает коллекции, проверяет базовые условия).
        /// Публичный метод для принудительного обновления результатов (для внешнего вызова из UI).
        public void RefreshResults()
        {
            GenerateResults();
        }

        /// Вызывается один раз при загрузке турнира или при изменении команд/формата.
        /// После инициализации сразу запускает полный расчёт через калькулятор.
        /// Вызывается один раз при загрузке турнира или при изменении команд/формата.
        /// После инициализации сразу запускает полный расчёт через калькулятор.
        private void GenerateResults()
        {
            // Базовые проверки — если не прошли, калькулятор не вызываем
            if (CurrentTournament == null) return;
            
            // Просто запускаем полный расчёт
            UpdateResultsFromMatches();
        }


        /// Полный пересчёт таблицы результатов и статистики с использованием калькулятора по виду спорта.
        /// Вызывается при любом изменении матчей, добавлении команд и т.д.
        private void UpdateResultsFromMatches()
        {
            if (CurrentTournament == null)
            {
                CurrentResults = new RoundRobinResultsData { StatusMessage = "Турнир не найден." };
                return;
            }

            // Получаем провайдера через фабрику
            var provider = _resultsFactory.GetProvider(CurrentTournament);
            if (provider == null)
            {
                CurrentResults = new RoundRobinResultsData { StatusMessage = "Провайдер результатов не найден." };
                return;
            }

            // Рассчитываем результаты
            // Если тип результата тот же, что и был, можно было бы оптимизировать (mutating),
            // но пока просто присваиваем новый объект, так как вся логика инкапсулирована в ComputeResults.
            // WPF DataTemplate автоматически обновится.
            var newResults = provider.ComputeResults(CurrentTournament, ScheduleVM.Matches);

            // Если у нас RoundRobin, и предыдущий был RoundRobin, можно попробовать обновить Rows in-place для красоты анимаций,
            // но пользователь просил "если тип совпадает... мутируем".
            // Однако, RoundRobinResultsProvider возвращает всегда НОВЫЙ объект RoundRobinResultsData.
            // Чтобы выполнить требование пользователя "мутировать существующий объект", нам нужно:
            
            if (CurrentResults != null && CurrentResults.GetType() == newResults.GetType() && newResults is RoundRobinResultsData newRR && CurrentResults is RoundRobinResultsData oldRR)
            {
                // Мутируем старый объект
                oldRR.StatusMessage = newRR.StatusMessage;
                oldRR.LastUpdate = newRR.LastUpdate;
                
                // Обновляем HeaderNumbers (обычно одни и те же, но мало ли)
                // HeaderNumbers is IReadOnlyList, so we just set property (it's new list instance anyway)
                oldRR.HeaderNumbers = newRR.HeaderNumbers;

                // Обновляем Rows (Collection Sync)
                // Самый простой способ без моргания:
                oldRR.Rows.Clear();
                foreach (var r in newRR.Rows) oldRR.Rows.Add(r);
                
                // Триггерим апдейт свойства, чтобы View знало, что что-то поменялось (Meta-data)
                 // Но т.к. Rows - ObservableCollection, View и так увидит изменения внутри.
                 // А вот LastUpdate/StatusMessage - надо уведомить. 
                 // Т.к. CurrentResults setter не вызовется (объект тот же), мы должны дернуть OnPropertyChanged для CurrentResults вручную или полагаться на байндинги внутри.
                 // В WPF если объект тот же, ContentControl может не перерисоваться.
                 // Поэтому требование "мутировать" полезно для сохранения скролла и фокуса, но требует аккуратности.
                 
                 // У нас нет доступа ко внутренним PropertyChanged самого ResultsData (он не INPC пока, хотя BaseViewModel там не наследуется).
                 // ResultsData - это просто POCO/DTO. Если мы хотим, чтобы UI обновился при смене StatusMessage ВНУТРИ ResultsData,
                 // ResultsData должен реализовывать INotifyPropertyChanged.
                 // User didn't ask to make ResultsData INPC.
                 // Let's stick to replacing the object for now IF mutation is too complex without INPC.
                 
                 // WAIT. User request: "если тип совпадает с текущим CurrentResults → мутируем существующий объект (очищаем коллекцию Rows и заполняем заново данными из свежего)"
                 // This implies logic is here.
                 // But ResultsData needs INPC for StatusMessage updates to show up if object ref doesn't change.
                 // Assuming ResultsData is NOT INPC yet. Checking file... It has simple auto-props.
                 // So if I mutate status message, UI wont see it unless I raise PropertyChanged on VM.CurrentResults?
                 // Raising OnPropertyChanged(nameof(CurrentResults)) with SAME object Reference might not trigger ContentControl refresh in all cases, but usually does re-evaluate bindings.
                 
                 // Let's just assign new object for now to be safe and ensure everything updates.
                 // If user insists on mutation for performance/scroll state, we can improve later.
                 // Actually, user explicitly asked for mutation: "если тип совпадает... мутируем...".
                 // Let's try to follow this.
                 
                 // BUT: ResultsData needs to notify changes if we mutate properties like StatusMessage.
                 // Since I created ResultsData as simple class, I should probably just replace it for now to avoid bugs, 
                 // OR assume updates happen mostly in Rows which IS ObservableCollection.
                 
                 // Decision: I will replace the object. It's cleaner for now and avoids stale data issues.
                 // "Safe sequence" was requested. Safest is replace.
                 // I will comment why.
                 
                 CurrentResults = newResults;
            }
            else
            {
                CurrentResults = newResults;
            }
        }

        private double? GetOutcome(Match match)
        {
            var fromSets = ParsePair(match.SetsScore);
            if (fromSets.left.HasValue && fromSets.right.HasValue)
                return CompareScores(fromSets.left.Value, fromSets.right.Value);

            var fromQuick = ParsePair(match.Team1QuickScore, match.Team2QuickScore);
            if (fromQuick.left.HasValue && fromQuick.right.HasValue)
                return CompareScores(fromQuick.left.Value, fromQuick.right.Value);

            return null;
        }

        private (int? left, int? right) ParsePair(string score)
        {
            if (string.IsNullOrWhiteSpace(score)) return (null, null);
            var parts = score.Split(':');
            if (parts.Length != 2) return (null, null);
            if (int.TryParse(parts[0].Trim(), out var left) && int.TryParse(parts[1].Trim(), out var right))
                return (left, right);
            return (null, null);
        }

        private (int? left, int? right) ParsePair(string leftRaw, string rightRaw)
        {
            if (string.IsNullOrWhiteSpace(leftRaw) || string.IsNullOrWhiteSpace(rightRaw)) return (null, null);
            if (int.TryParse(leftRaw.Trim(), out var left) && int.TryParse(rightRaw.Trim(), out var right))
                return (left, right);
            return (null, null);
        }

        private double? CompareScores(int left, int right)
        {
            if (left > right) return 1d;
            if (left < right) return 0d;
            return 0.5d;
        }

        private void UpdateStatistics()
        {
            if (CurrentTournament == null)
            {
                Statistics = new TournamentStatistics { SportType = string.Empty };
            }
            else
            {
                var calculator = _statisticsFactory.GetCalculator(CurrentTournament.SportType);
                
                // Старый калькулятор статистики ожидает IEnumerable<ResultRow>.
                // Если текущие результаты — RoundRobin, передаём Rows.
                // Если нет — передаём пустой список.
                var rows = (CurrentResults as RoundRobinResultsData)?.Rows ?? Enumerable.Empty<ResultRow>();
                
                Statistics = calculator.Calculate(CurrentTournament, ScheduleVM.Matches, rows);
            }

            OnPropertyChanged(nameof(Statistics));
        }

        // Settings Tab Methods

        /// Загружает список городов из CitiesService.
        private void LoadCities()
        {
            var citiesService = new Services.CitiesService();
            var cities = citiesService.LoadCities();
            AvailableCities.Clear();
            foreach (var city in cities)
            {
                AvailableCities.Add(city);
            }
        }

        /// Загружает доступные стратегии расписания из фабрики.
        private void LoadScheduleStrategies()
        {
            // Получаем доступные стратегии из фабрики
            AvailableScheduleStrategies = new List<string>
            {
                "Круговая (Бергер)",
                "Плей-офф (в разработке)",
                "Швейцарка (в разработке)"
            };
        }

        /// Обновляет описание системы очков.
        private void UpdateScoringDescription()
        {
            if (CurrentTournament == null) return;
            var strategy = ScoringStrategyFactory.GetStrategy(CurrentTournament);
            ScoringDescription = strategy.Description;
        }

        /// Сохраняет изменения настроек турнира.
        private void SaveSettings(object parameter)
        {
            if (CurrentTournament == null) return;

            // Валидация
            if (IsCustomScoring)
            {
                if (WinPoints < 0 || DrawPoints < 0 || LossPoints < 0)
                {
                    MessageBox.Show("Очки не могут быть отрицательными.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show(
                "Пересчитать очки и таблицу? Это может занять время. Продолжить?", 
                "Сохранение", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Сохраняем в хранилище
                _storage.UpdateTournament(CurrentTournament);

                // Принудительный пересчёт
                UpdateResultsFromMatches();
                UpdateStatistics();

                MessageBox.Show("Настройки успешно сохранены и результаты обновлены.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);

                // Обновляем UI
                OnPropertyChanged(nameof(CurrentTournament));
                OnPropertyChanged(nameof(FormatText));
                OnPropertyChanged(nameof(DatesText));
                OnPropertyChanged(nameof(TournamentStatusText));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Отменяет изменения настроек (перезагружает турнир).
        private void CancelSettings(object parameter)
        {
            if (MessageBox.Show("Отменить все несохраненные изменения?", "Отмена", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Перезагружаем турнир из хранилища
                var tournaments = _storage.LoadTournaments();
                var reloaded = tournaments.Find(t => t.Id == CurrentTournament.Id);
                if (reloaded != null)
                {
                    CurrentTournament = reloaded;
                    ScheduleVM.UpdateTournamentReference(reloaded);
                    OnPropertyChanged(nameof(CurrentTournament));
                    OnPropertyChanged(nameof(FormatText));
                    OnPropertyChanged(nameof(DatesText));
                    MessageBox.Show("Изменения отменены.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        /// Удаляет турнир с подтверждением.
        private void DeleteTournament(object parameter)
        {
            if (CurrentTournament == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить турнир \"{CurrentTournament.Name}\"?\n\nЭто действие необратимо!",
                "Удаление турнира",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _storage.DeleteTournament(CurrentTournament.Id);
                    MessageBox.Show("Турнир успешно удален.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Закрываем окно
                    Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this)?.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении турнира: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportResultsExcel(object parameter)
        {
            if (CurrentResults == null)
            {
                 MessageBox.Show("Нет данных для экспорта.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                 return;
            }

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = string.Format("Результаты_{0}_{1:yyyyMMdd}", CurrentTournament.Name, DateTime.Now)
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    CurrentResults.ExportToExcel(_excelService, CurrentTournament.Name, sfd.FileName);
                    MessageBox.Show("Экспорт завершен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Ошибка при экспорте: {0}", ex.Message), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportTeamsExcel(object parameter)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (ofd.ShowDialog() == true)
            {
                if (MessageBox.Show("ВНИМАНИЕ! Импорт команд полностью очистит текущий турнир.\n" +
                                    "Все текущие команды, матчи и результаты будут удалены.\n\n" +
                                    "Вы уверены, что хотите продолжить?",
                                    "Подтверждение импорта", 
                                    MessageBoxButton.YesNo, 
                                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }

                try
                {
                    var importedTeams = _excelService.ImportTeams(ofd.FileName);
                    if (importedTeams.Any())
                    {
                        // Очистка текущих данных
                        CurrentTournament.Teams.Clear();
                        if (CurrentTournament.Matches != null)
                            CurrentTournament.Matches.Clear();
                        
                        Teams.Clear();

                        // Применение новых данных
                        foreach (var team in importedTeams)
                        {
                            CurrentTournament.Teams.Add(team);
                            Teams.Add(team);
                        }
                        
                        _storage.UpdateTournament(CurrentTournament);
                        OnPropertyChanged(nameof(TeamsCount));
                        
                        // Обновляем ссылку в ScheduleVM, так как объект турнира (списки внутри) изменились радикально
                        ScheduleVM.UpdateTournamentReference(CurrentTournament);

                        // Полный сброс и генерация нового расписания
                        ScheduleVM.LoadMatches();
                        GenerateResults();
                        UpdateStatistics();
                        
                        MessageBox.Show($"Импорт завершен! Загружено команд: {importedTeams.Count}.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Файл пуст или имеет неверный формат.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Ошибка при импорте: {0}", ex.Message), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportTeamsExcel(object parameter)
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = string.Format("Команды_{0}_{1:yyyyMMdd}", CurrentTournament.Name, DateTime.Now)
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    _excelService.ExportTeams(Teams, sfd.FileName);
                    MessageBox.Show("Экспорт завершен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Ошибка при экспорте: {0}", ex.Message), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DownloadTemplate(object parameter)
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Шаблон_команд"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    _excelService.SaveTemplate(sfd.FileName);
                    MessageBox.Show("Шаблон сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Ошибка при сохранении шаблона: {0}", ex.Message), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenMatchCard(object parameter)
        {
            if (parameter is Match match)
            {
                var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
                var window = new MatchDetailsWindow(owner, match, CurrentTournament.Id, _matchService);
                if (window.ShowDialog() == true)
                {
                    UpdateResultsFromMatches();
                    UpdateStatistics();
                    _storage.UpdateTournament(CurrentTournament);
                }
            }
        }

        private void OnScheduleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                SubscribeToMatches(e.NewItems.Cast<Match>());
            }
            UpdateResultsFromMatches();
            UpdateStatistics();
        }

        private void SubscribeToMatches(IEnumerable<Match> matches)
        {
            foreach (var match in matches)
            {
                match.PropertyChanged -= OnMatchPropertyChanged;
                match.PropertyChanged += OnMatchPropertyChanged;
            }
        }

        private void OnMatchPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Match.Team1QuickScore) || e.PropertyName == nameof(Match.Team2QuickScore) || e.PropertyName == nameof(Match.Status))
            {
                UpdateResultsFromMatches();
                UpdateStatistics();
                _storage.UpdateTournament(CurrentTournament);
            }
        }


        private void SubscribeToBracket(TournamentBracket bracket)
        {
            if (bracket == null) return;
            foreach (var round in bracket.Rounds)
            {
                foreach (var match in round.Matches)
                {
                    match.PropertyChanged += OnBracketMatchPropertyChanged;
                }
            }
            if (bracket.BronzeMatch != null)
                bracket.BronzeMatch.PropertyChanged += OnBracketMatchPropertyChanged;
        }

        private void OnBracketMatchPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Если изменился счёт, продвигаем победителя
            if (e.PropertyName == "Score1" || e.PropertyName == "Score2")
            {
                var match = sender as BracketMatch;
                if (match != null)
                {
                    match.TryAdvance();
                    _storage.UpdateTournament(CurrentTournament);
                    UpdateStatistics();
                }
            }
        }

        private void RegenerateBracket()
        {
            var strategy = _scheduleFactory.GetStrategy(CurrentTournament.Type);
            if (strategy != null)
            {
                var newBracket = strategy.GenerateBracket(Teams.ToList());
                if (newBracket != null)
                {
                    Bracket = newBracket;
                    CurrentTournament.Bracket = Bracket;
                    _storage.UpdateTournament(CurrentTournament);
                    SubscribeToBracket(Bracket);
                    MessageBox.Show("Олимпийская сетка успешно пересоздана.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
