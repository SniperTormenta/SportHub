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
        private readonly IResultsCalculatorFactory _resultsFactory;

        /// Фабрика калькуляторов статистики. Инжектируется через конструктор.
        private readonly IStatisticsCalculatorFactory _statisticsFactory;

        
        /// Текущий турнир (Model). Биндится к UI для заголовков/дат.
        
        public Tournament CurrentTournament { get; private set; }

        
        /// Коллекция команд для UI (ListView/DataGrid). Observable для добавления/удаления.
        
        public ObservableCollection<Team> Teams { get; } = new ObservableCollection<Team>();

        // Расписание
        
        /// Коллекция матчей (генерируется стратегией, обновляется из сохранённых).
        
        public ObservableCollection<Match> Schedule { get; } = new ObservableCollection<Match>();

        public TournamentStatistics Statistics { get; private set; } = new TournamentStatistics();

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

        // Результаты (шахматная таблица для кругового формата)
        
        /// Номера столбцов/строк таблицы (1..N).
        
        public ObservableCollection<int> ResultsHeaderNumbers { get; } = new ObservableCollection<int>();

        
        /// Строки таблицы результатов (с Cells для ячеек, статистикой).
        
        public ObservableCollection<ResultRow> ResultsTable { get; } = new ObservableCollection<ResultRow>();

        private string _resultsMessage;
        
        /// Сообщение о статусе таблицы (e.g. "в разработке").
        
        public string ResultsMessage
        {
            get => _resultsMessage;
            private set
            {
                if (_resultsMessage != value)
                {
                    _resultsMessage = value;
                    OnPropertyChanged();
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


        /// Флаг live-режима (для индикатора в UI).

        public bool IsLive => CurrentTournament?.IsLive ?? false;

        // Статистика
        
        /// Сыгранные матчи.
        
        public int TotalMatchesPlayed => Schedule.Count(m => !string.IsNullOrWhiteSpace(m.Status) &&
            string.Equals(m.Status, "Сыгран", StringComparison.OrdinalIgnoreCase));

        
        /// Всего сыгранных сетов (партий).
        
        public int TotalSetsPlayed
        {
            get
            {
                int total = 0;
                foreach (var match in Schedule)
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
                foreach (var match in Schedule)
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
                var mvpCounts = Schedule
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

                return Schedule
                    .Count(m => string.Equals(m.Mvp, mvp, StringComparison.OrdinalIgnoreCase));
            }
        }

        
        /// Лидер по победам.
        
        public string MostPopularTeamName
        {
            get
            {
                var wins = new Dictionary<string, int>();
                foreach (var match in Schedule)
                {
                    if (string.IsNullOrWhiteSpace(match.Status) ||
                        !string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase))
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
                foreach (var match in Schedule)
                {
                    if (string.IsNullOrWhiteSpace(match.Status) ||
                        !string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase))
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
                foreach (var match in Schedule)
                {
                    if (string.IsNullOrWhiteSpace(match.Status) ||
                        !string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase))
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


        /// Конструктор: загружает турнир по ID, инициализирует коллекции, команды, генерирует расписание/результаты, подписывается на изменения.

        /// <param name="tournamentId">ID турнира из списка.</param>
        /// <param name="storage">Сервис хранения.</param>
        /// <param name="scheduleFactory">Фабрика стратегий расписания.</param>
        /// <param name="resultsFactory">Фабрика калькуляторов результатов.</param>
        /// <param name="statisticsFactory">Фабрика калькуляторов статистики.</param>
        public TournamentViewModel(
            Guid tournamentId,
            IStorage storage,
            IScheduleStrategyFactory scheduleFactory,
            IResultsCalculatorFactory resultsFactory,
            IStatisticsCalculatorFactory statisticsFactory)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _scheduleFactory = scheduleFactory ?? throw new ArgumentNullException(nameof(scheduleFactory));
            _resultsFactory = resultsFactory ?? throw new ArgumentNullException(nameof(resultsFactory));
            _statisticsFactory = statisticsFactory ?? throw new ArgumentNullException(nameof(statisticsFactory));

            var tournaments = _storage.LoadTournaments();
            CurrentTournament = tournaments.Find(t => t.Id == tournamentId);
            if (CurrentTournament != null)
            {
                foreach (var team in CurrentTournament.Teams)
                {
                    Teams.Add(team);
                }
            }

            AddTeamCommand = new RelayCommand(OpenAddTeamWindow);
            EditTeamCommand = new RelayCommand(EditTeam, t => t is Team);
            OpenMatchCommand = new RelayCommand(OpenMatchCard, m => m is Match);

            GenerateSchedule();
            GenerateResults();

            Schedule.CollectionChanged += OnScheduleCollectionChanged;
            SubscribeToMatches(Schedule);

            UpdateResultsFromMatches();
            UpdateStatistics();
        }

        
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
                        Teams.Clear();
                        foreach (var team in updated.Teams)
                        {
                            Teams.Add(team);
                        }
                    }
                    OnPropertyChanged(nameof(TeamsCount));
                }
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
                        Teams.Clear();
                        foreach (var t in updated.Teams)
                        {
                            Teams.Add(t);
                        }
                    }
                    OnPropertyChanged(nameof(TeamsCount));
                }
            }
        }

        /// Инициализирует структуру таблицы результатов (очищает коллекции, проверяет базовые условия).
        /// Вызывается один раз при загрузке турнира или при изменении команд/формата.
        /// После инициализации сразу запускает полный расчёт через калькулятор.
        private void GenerateResults()
        {
            // Очищаем всё перед новым расчётом
            ResultsTable.Clear();
            ResultsHeaderNumbers.Clear();
            ResultsMessage = string.Empty;

            // Базовые проверки — если не прошли, калькулятор не вызываем
            if (CurrentTournament == null)
            {
                ResultsMessage = "Турнир не найден.";
                return;
            }

            if (Teams.Count == 0)
            {
                ResultsMessage = "Команды ещё не добавлены.";
                return;
            }

            // Для не-круговых форматов пока только сообщение (в будущем — другое представление)
            if (!string.Equals(CurrentTournament.Type, "Круговой", StringComparison.OrdinalIgnoreCase))
            {
                ResultsMessage = $"Таблица результатов для формата \"{CurrentTournament.Type}\" — в разработке.";
                return;
            }

            // Здесь больше НЕ строим вручную строки и ячейки!
            // Это теперь делает VolleyballResultsCalculator внутри Calculate()

            // Просто запускаем полный расчёт
            UpdateResultsFromMatches();
        }


        /// Генерация расписания с использованием паттерна "Стратегия".
        /// Merge с сохранёнными матчами (сохраняет введённые результаты).

        private void GenerateSchedule()
        {
            Schedule.Clear();
            ScheduleMessage = string.Empty;

            if (CurrentTournament == null)
            {
                ScheduleMessage = "Турнир не найден.";
                return;
            }
            if (Teams.Count < 2)
            {
                ScheduleMessage = "Недостаточно команд для генерации расписания.";
                return;
            }

            IScheduleStrategy strategy = _scheduleFactory.GetStrategy(CurrentTournament.Type);
            if (strategy == null)
            {
                ScheduleMessage = $"Формат \"{CurrentTournament.Type}\" не поддерживается.";
                return;
            }
            if (!strategy.IsImplemented)
            {
                ScheduleMessage = $"Формат \"{strategy.Name}\" — расписание в разработке.";
                return;
            }

            IEnumerable<Match> generatedMatches;
            try
            {
                generatedMatches = strategy.GenerateSchedule(Teams.ToList()) ?? Enumerable.Empty<Match>();
            }
            catch (Exception)
            {
                ScheduleMessage = "Произошла ошибка при генерации расписания.";
                return;
            }

            var savedMatchesDict = new Dictionary<string, Match>();
            if (CurrentTournament.Matches != null)
            {
                foreach (var savedMatch in CurrentTournament.Matches)
                {
                    string key = $"{savedMatch.Team1}|{savedMatch.Team2}|{savedMatch.Round}";
                    savedMatchesDict[key] = savedMatch;
                }
            }

            foreach (var generatedMatch in generatedMatches)
            {
                string key = $"{generatedMatch.Team1}|{generatedMatch.Team2}|{generatedMatch.Round}";

                if (savedMatchesDict.TryGetValue(key, out Match savedMatch))
                {
                    generatedMatch.Id = savedMatch.Id;
                    generatedMatch.Status = savedMatch.Status;
                    generatedMatch.Team1QuickScore = savedMatch.Team1QuickScore;
                    generatedMatch.Team2QuickScore = savedMatch.Team2QuickScore;
                    generatedMatch.SetsScore = savedMatch.SetsScore;
                    generatedMatch.SetsBySet = savedMatch.SetsBySet;
                    generatedMatch.TotalScore = savedMatch.TotalScore;
                    generatedMatch.Duration = savedMatch.Duration;
                    generatedMatch.Referee = savedMatch.Referee;
                    generatedMatch.Location = savedMatch.Location;
                    generatedMatch.Mvp = savedMatch.Mvp;
                }
                Schedule.Add(generatedMatch);
            }

            if (Schedule.Count == 0)
            {
                ScheduleMessage = "Не удалось сгенерировать расписание.";
            }
        }

        
        /// Подписка на изменения коллекции Schedule (для новых/удалённых матчей).
        
        private void OnScheduleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<Match>())
                {
                    item.PropertyChanged -= OnMatchPropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<Match>())
                {
                    item.PropertyChanged += OnMatchPropertyChanged;
                }
            }
            UpdateResultsFromMatches();
            UpdateStatistics();
        }

        
        /// Подписка на PropertyChanged отдельных матчей.
        
        private void SubscribeToMatches(IEnumerable<Match> matches)
        {
            foreach (var match in matches)
            {
                match.PropertyChanged -= OnMatchPropertyChanged;
                match.PropertyChanged += OnMatchPropertyChanged;
            }
        }

        
        /// Обработчик изменений в Match: авто-статус "Сыгран", сохранение, обновление результатов/статистики.
        
        private void OnMatchPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Match.SetsScore) ||
                e.PropertyName == nameof(Match.Team1QuickScore) ||
                e.PropertyName == nameof(Match.Team2QuickScore) ||
                e.PropertyName == nameof(Match.Status) ||
                e.PropertyName == nameof(Match.SetsBySet) ||
                e.PropertyName == nameof(Match.TotalScore) ||
                e.PropertyName == nameof(Match.Mvp))
            {
                var match = sender as Match;
                if (match != null)
                {
                    if (string.Equals(match.Status, "Не сыгран", StringComparison.OrdinalIgnoreCase))
                    {
                        var outcome = GetOutcome(match);
                        if (outcome.HasValue)
                        {
                            match.Status = "Сыгран";
                        }
                    }
                    SaveMatchToTournament(match);
                }
                UpdateResultsFromMatches();
                UpdateStatistics();
            }
        }
        
        /// Открывает карточку матча (MatchDetailsWindow).
        /// После закрытия — сохраняет и обновляет.
        
        private void OpenMatchCard(object parameter)
        {
            var match = parameter as Match;
            if (match == null) return;

            var currentWindow = Application.Current.Windows
                .OfType<TournamentWindow>()
                .FirstOrDefault(w => w.IsActive);
            if (currentWindow != null && CurrentTournament != null)
            {
                var detailsWindow = new MatchDetailsWindow(currentWindow, match, CurrentTournament.Id);
                if (detailsWindow.ShowDialog() == true)
                {
                    if (string.IsNullOrWhiteSpace(match.Status))
                    {
                        match.Status = "Сыгран";
                    }
                    SaveMatchToTournament(match);
                    UpdateResultsFromMatches();
                    UpdateStatistics();
                }
            }
        }

        
        /// Сохранение изменений матча в Tournament и JSON.
        
        private void SaveMatchToTournament(Match match)
        {
            if (CurrentTournament == null || match == null) return;
            if (CurrentTournament.Matches == null)
                CurrentTournament.Matches = new List<Match>();

            var existingMatch = CurrentTournament.Matches.FirstOrDefault(m =>
                m.Id == match.Id ||
                (m.Team1 == match.Team1 && m.Team2 == match.Team2 && m.Round == match.Round));

            if (existingMatch != null)
            {
                existingMatch.Status = match.Status ?? existingMatch.Status;
                existingMatch.Team1QuickScore = match.Team1QuickScore ?? existingMatch.Team1QuickScore;
                existingMatch.Team2QuickScore = match.Team2QuickScore ?? existingMatch.Team2QuickScore;
                existingMatch.SetsScore = match.SetsScore ?? existingMatch.SetsScore;
                existingMatch.SetsBySet = match.SetsBySet ?? existingMatch.SetsBySet;
                existingMatch.TotalScore = match.TotalScore ?? existingMatch.TotalScore;
                existingMatch.Duration = match.Duration ?? existingMatch.Duration;
                existingMatch.Referee = match.Referee ?? existingMatch.Referee;
                existingMatch.Location = match.Location ?? existingMatch.Location;
                existingMatch.Mvp = match.Mvp ?? existingMatch.Mvp;
            }
            else
            {
                var newMatch = new Match
                {
                    Id = match.Id,
                    Round = match.Round,
                    Team1 = match.Team1,
                    Team2 = match.Team2,
                    Status = match.Status,
                    Team1QuickScore = match.Team1QuickScore,
                    Team2QuickScore = match.Team2QuickScore,
                    SetsScore = match.SetsScore,
                    SetsBySet = match.SetsBySet,
                    TotalScore = match.TotalScore,
                    Duration = match.Duration,
                    Referee = match.Referee,
                    Location = match.Location,
                    Mvp = match.Mvp
                };
                CurrentTournament.Matches.Add(newMatch);
            }

            _storage.UpdateTournament(CurrentTournament);
        }

        /// Полный пересчёт таблицы результатов и статистики с использованием калькулятора по виду спорта.
        /// Вызывается при любом изменении матчей, добавлении команд и т.д.
        private void UpdateResultsFromMatches()
        {
            if (CurrentTournament == null)
            {
                ResultsMessage = "Турнир не найден.";
                ResultsTable.Clear();
                ResultsHeaderNumbers.Clear();
                return;
            }

            var calculator = _resultsFactory.GetCalculator(CurrentTournament);

            if (calculator == null)
            {
                ResultsMessage = "Калькулятор результатов не найден для данного вида спорта.";
                ResultsTable.Clear();
                ResultsHeaderNumbers.Clear();
                return;
            }

            calculator.Calculate(CurrentTournament, Schedule, ResultsTable, out string message);
            ResultsMessage = message;

            // ← Ключевое исправление: обновляем заголовки ПОСЛЕ расчёта и только для кругового
            if (string.Equals(CurrentTournament.Type, "Круговой", StringComparison.OrdinalIgnoreCase))
            {
                ResultsHeaderNumbers.Clear();
                for (int i = 1; i <= ResultsTable.Count; i++)
                {
                    ResultsHeaderNumbers.Add(i);
                }
            }
            else
            {
                ResultsHeaderNumbers.Clear(); // Для других форматов — очищаем
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
                Statistics = calculator.Calculate(CurrentTournament, Schedule);
            }

            OnPropertyChanged(nameof(Statistics));
        }

    }
}