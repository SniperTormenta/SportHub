// ViewModels/TournamentViewModel.cs

// Вынести текущий расчёт из UpdateResultsFromMatches() в VolleyballResultsCalculator.
// Создать фабрику и интерфейс.
// Изменить VM на использование калькулятора.
// Добавить заглушки для футбола/баскетбола (пока сообщение "в разработке").
// По мере необходимости реализовать другие калькуляторы.
using SportHubBase.Models;
using SportHubBase.Services;
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
        
        /// Сервис хранения. Пока прямое создание; для инверсии зависимостей — инжектировать.
        
        private readonly JsonStorageService _storage = new JsonStorageService();

        
        /// Текущий турнир (Model). Биндится к UI для заголовков/дат.
        
        public Tournament CurrentTournament { get; private set; }

        
        /// Коллекция команд для UI (ListView/DataGrid). Observable для добавления/удаления.
        
        public ObservableCollection<Team> Teams { get; } = new ObservableCollection<Team>();

        // Расписание
        
        /// Коллекция матчей (генерируется стратегией, обновляется из сохранённых).
        
        public ObservableCollection<Match> Schedule { get; } = new ObservableCollection<Match>();

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

        
        /// Генерация структуры таблицы результатов (для кругового формата).
        
        private void GenerateResults()
        {
            ResultsHeaderNumbers.Clear();
            ResultsTable.Clear();
            ResultsMessage = string.Empty;

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
            if (!string.Equals(CurrentTournament.Type, "Круговой", StringComparison.OrdinalIgnoreCase))
            {
                ResultsMessage = $"Формат \"{CurrentTournament.Type}\" — результаты в разработке.";
                return;
            }

            var sortedTeams = Teams.OrderBy(t => t.Name).ToList();
            int teamCount = sortedTeams.Count;
            for (int i = 1; i <= teamCount; i++)
            {
                ResultsHeaderNumbers.Add(i);
            }
            for (int i = 0; i < teamCount; i++)
            {
                var row = new ResultRow
                {
                    Index = i + 1,
                    TeamName = sortedTeams[i].Name
                };
                for (int j = 0; j < teamCount; j++)
                {
                    row.Cells.Add(i == j ? "SELF" : string.Empty);
                }
                ResultsTable.Add(row);
            }
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

            IScheduleStrategy strategy = ScheduleStrategyFactory.GetStrategy(CurrentTournament.Type);
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

        
        /// Уведомление об изменении статистических свойств.
        
        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(TotalMatchesPlayed));
            OnPropertyChanged(nameof(TotalSetsPlayed));
            OnPropertyChanged(nameof(TotalPointsScored));
            OnPropertyChanged(nameof(MostValuablePlayerName));
            OnPropertyChanged(nameof(MostValuablePlayerTeam));
            OnPropertyChanged(nameof(MostPopularTeamName));
            OnPropertyChanged(nameof(MostPopularTeamWins));
            OnPropertyChanged(nameof(MostPopularTeamMatchesPlayed));
            OnPropertyChanged(nameof(MostPopularTeamWinPercentage));
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

        
        /// Основной метод расчёта таблицы: статистика, итальянские очки, сортировка (очки → коэф. сетов → коэф. мячей → личные встречи), места.
        /// Длинный, но полный; работает только для кругового формата.
        
        private void UpdateResultsFromMatches()
        {
            if (CurrentTournament == null ||
                !string.Equals(CurrentTournament.Type, "Круговой", StringComparison.OrdinalIgnoreCase) ||
                Teams.Count == 0)
            {
                return;
            }

            GenerateResults();
            if (ResultsTable.Count == 0) return;

            var sortedTeams = Teams.OrderBy(t => t.Name).ToList();
            var nameToIndex = sortedTeams
                .Select((team, index) => new { team.Name, index })
                .ToDictionary(k => k.Name, v => v.index, StringComparer.OrdinalIgnoreCase);

            var teamStats = sortedTeams.ToDictionary(
                t => t.Name,
                t => new ResultRow
                {
                    TeamName = t.Name,
                    Wins = 0,
                    Losses = 0,
                    SetsWon = 0,
                    SetsLost = 0,
                    Points = 0,
                    PointsScored = 0,
                    PointsConceded = 0,
                    SetsRatio = 0,
                    PointsRatio = 0
                },
                StringComparer.OrdinalIgnoreCase);

            foreach (var match in Schedule)
            {
                if (match == null || string.IsNullOrWhiteSpace(match.Team1) || string.IsNullOrWhiteSpace(match.Team2))
                    continue;
                if (!teamStats.TryGetValue(match.Team1, out var team1Stats) ||
                    !teamStats.TryGetValue(match.Team2, out var team2Stats))
                    continue;

                if (string.IsNullOrWhiteSpace(match.Status) ||
                    !string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase))
                    continue;

                var setsScore = ParsePair(match.SetsScore);
                if (!setsScore.left.HasValue || !setsScore.right.HasValue)
                {
                    var quickScore = ParsePair(match.Team1QuickScore, match.Team2QuickScore);
                    if (quickScore.left.HasValue && quickScore.right.HasValue)
                    {
                        setsScore = quickScore;
                    }
                    else continue;
                }

                int team1Sets = setsScore.left.Value;
                int team2Sets = setsScore.right.Value;

                var totalScore = ParsePair(match.TotalScore);
                int team1Points = totalScore.left ?? 0;
                int team2Points = totalScore.right ?? 0;

                team1Stats.SetsWon += team1Sets;
                team1Stats.SetsLost += team2Sets;
                team2Stats.SetsWon += team2Sets;
                team2Stats.SetsLost += team1Sets;

                team1Stats.PointsScored += team1Points;
                team1Stats.PointsConceded += team2Points;
                team2Stats.PointsScored += team2Points;
                team2Stats.PointsConceded += team1Points;

                int team1PointsItalian = CalculateItalianPoints(team1Sets, team2Sets);
                int team2PointsItalian = CalculateItalianPoints(team2Sets, team1Sets);
                team1Stats.Points += team1PointsItalian;
                team2Stats.Points += team2PointsItalian;

                if (team1Sets > team2Sets)
                {
                    team1Stats.Wins++;
                    team2Stats.Losses++;
                }
                else if (team1Sets < team2Sets)
                {
                    team1Stats.Losses++;
                    team2Stats.Wins++;
                }

                if (nameToIndex.TryGetValue(match.Team1, out int t1) &&
                    nameToIndex.TryGetValue(match.Team2, out int t2))
                {
                    string team1Cell = team1Sets > team2Sets ? "1" : (team1Sets < team2Sets ? "0" : "½");
                    string team2Cell = team1Cell == "1" ? "0" : (team1Cell == "0" ? "1" : "½");

                    if (ResultsTable.ElementAtOrDefault(t1)?.Cells.Count > t2 &&
                        ResultsTable.ElementAtOrDefault(t2)?.Cells.Count > t1)
                    {
                        ResultsTable[t1].Cells[t2] = team1Cell;
                        ResultsTable[t2].Cells[t1] = team2Cell;
                    }
                }
            }

            foreach (var stats in teamStats.Values)
            {
                stats.SetsRatio = stats.SetsLost > 0 ? (double)stats.SetsWon / stats.SetsLost :
                                 stats.SetsWon > 0 ? double.MaxValue : 0;

                stats.PointsRatio = stats.PointsConceded > 0 ? (double)stats.PointsScored / stats.PointsConceded :
                                    stats.PointsScored > 0 ? double.MaxValue : 0;
            }

            var headToHeadResults = new Dictionary<string, Dictionary<string, int>>();
            foreach (var match in Schedule)
            {
                if (match == null || string.IsNullOrWhiteSpace(match.Team1) || string.IsNullOrWhiteSpace(match.Team2))
                    continue;
                if (string.IsNullOrWhiteSpace(match.Status) ||
                    !string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase))
                    continue;

                var setsScore = ParsePair(match.SetsScore);
                if (!setsScore.left.HasValue || !setsScore.right.HasValue)
                {
                    var quickScore = ParsePair(match.Team1QuickScore, match.Team2QuickScore);
                    if (quickScore.left.HasValue && quickScore.right.HasValue)
                        setsScore = quickScore;
                    else continue;
                }

                int team1Sets = setsScore.left.Value;
                int team2Sets = setsScore.right.Value;

                if (!headToHeadResults.ContainsKey(match.Team1))
                    headToHeadResults[match.Team1] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (!headToHeadResults.ContainsKey(match.Team2))
                    headToHeadResults[match.Team2] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                int result = team1Sets > team2Sets ? 1 : (team1Sets < team2Sets ? -1 : 0);
                headToHeadResults[match.Team1][match.Team2] = result;
                headToHeadResults[match.Team2][match.Team1] = -result;
            }

            var sortedByPlace = teamStats.Values
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.SetsRatio)
                .ThenByDescending(s => s.PointsRatio)
                .ToList();

            // Корректировка по личным встречам (группами равных)
            for (int i = 0; i < sortedByPlace.Count; i++)
            {
                var currentTeam = sortedByPlace[i];
                var equalTeams = sortedByPlace
                    .Where(t => t != currentTeam &&
                                t.Points == currentTeam.Points &&
                                Math.Abs(t.SetsRatio - currentTeam.SetsRatio) < 0.000001 &&
                                Math.Abs(t.PointsRatio - currentTeam.PointsRatio) < 0.000001)
                    .ToList();

                if (equalTeams.Count > 0)
                {
                    int groupStart = i;
                    int groupEnd = i;
                    while (groupEnd + 1 < sortedByPlace.Count &&
                           sortedByPlace[groupEnd + 1].Points == currentTeam.Points &&
                           Math.Abs(sortedByPlace[groupEnd + 1].SetsRatio - currentTeam.SetsRatio) < 0.000001 &&
                           Math.Abs(sortedByPlace[groupEnd + 1].PointsRatio - currentTeam.PointsRatio) < 0.000001)
                    {
                        groupEnd++;
                    }

                    if (groupEnd > groupStart)
                    {
                        var group = sortedByPlace.Skip(groupStart).Take(groupEnd - groupStart + 1).ToList();
                        group = group.OrderByDescending(t =>
                        {
                            int h2h = 0;
                            if (headToHeadResults.ContainsKey(t.TeamName))
                            {
                                foreach (var other in group.Where(ot => ot.TeamName != t.TeamName))
                                {
                                    if (headToHeadResults[t.TeamName].TryGetValue(other.TeamName, out int h2hResult))
                                        h2h += h2hResult;
                                }
                            }
                            return h2h;
                        }).ToList();

                        for (int j = 0; j < group.Count; j++)
                        {
                            sortedByPlace[groupStart + j] = group[j];
                        }
                    }
                }
            }

            int currentPlace = 1;
            for (int i = 0; i < sortedByPlace.Count; i++)
            {
                if (i > 0)
                {
                    var prev = sortedByPlace[i - 1];
                    var curr = sortedByPlace[i];
                    if (!(prev.Points == curr.Points &&
                          Math.Abs(prev.SetsRatio - curr.SetsRatio) < 0.000001 &&
                          Math.Abs(prev.PointsRatio - curr.PointsRatio) < 0.000001))
                    {
                        currentPlace = i + 1;
                    }
                }
                sortedByPlace[i].Place = currentPlace;
            }

            var oldTable = ResultsTable.ToList();
            ResultsTable.Clear();

            var newNameToIndex = sortedByPlace
                .Select((row, index) => new { row.TeamName, index })
                .ToDictionary(k => k.TeamName, v => v.index, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < sortedByPlace.Count; i++)
            {
                var stats = sortedByPlace[i];
                var oldRow = oldTable.FirstOrDefault(r => r.TeamName == stats.TeamName);
                var newRow = new ResultRow
                {
                    Index = i + 1,
                    TeamName = stats.TeamName,
                    Wins = stats.Wins,
                    Losses = stats.Losses,
                    SetsWon = stats.SetsWon,
                    SetsLost = stats.SetsLost,
                    Points = stats.Points,
                    Place = stats.Place,
                    SetsRatio = stats.SetsRatio,
                    PointsRatio = stats.PointsRatio,
                    PointsScored = stats.PointsScored,
                    PointsConceded = stats.PointsConceded
                };

                newRow.Cells = new ObservableCollection<string>();
                for (int j = 0; j < sortedByPlace.Count; j++)
                {
                    var opponentName = sortedByPlace[j].TeamName;
                    if (i == j)
                    {
                        newRow.Cells.Add("SELF");
                    }
                    else if (oldRow != null)
                    {
                        var oldOpponentIndex = oldTable.FindIndex(r => r.TeamName == opponentName);
                        newRow.Cells.Add(oldOpponentIndex >= 0 && oldRow.Cells.Count > oldOpponentIndex
                            ? oldRow.Cells[oldOpponentIndex]
                            : string.Empty);
                    }
                    else
                    {
                        newRow.Cells.Add(string.Empty);
                    }
                }
                ResultsTable.Add(newRow);
            }
        }

        
        /// Расчёт итальянских очков для волейбола.
        
        private int CalculateItalianPoints(int setsWon, int setsLost)
        {
            if (setsWon == 3 && (setsLost == 0 || setsLost == 1)) return 3;
            if (setsWon == 3 && setsLost == 2) return 2;
            if (setsWon == 2 && setsLost == 3) return 1;
            return 0;
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
    }
}