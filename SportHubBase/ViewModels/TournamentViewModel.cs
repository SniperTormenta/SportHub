// ViewModels/TournamentViewModel.cs
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
    public class TournamentViewModel : BaseViewModel
    {
        private readonly JsonStorageService _storage = new JsonStorageService();

        public Tournament CurrentTournament { get; private set; }
        public ObservableCollection<Team> Teams { get; } = new ObservableCollection<Team>();

        // Расписание
        public ObservableCollection<Match> Schedule { get; } = new ObservableCollection<Match>();

        private string _scheduleMessage;
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
        public ObservableCollection<int> ResultsHeaderNumbers { get; } = new ObservableCollection<int>();
        public ObservableCollection<ResultRow> ResultsTable { get; } = new ObservableCollection<ResultRow>();

        private string _resultsMessage;
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
        public int TeamsCount => Teams.Count;
        public string FormatText => CurrentTournament?.Type ?? "";
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

                // Если нет конца — начало + текст на новой строке
                return $"{start}\nтурнир продолжается";
            }
        }

        public bool IsLive => CurrentTournament?.IsLive ?? false;

        // Статистика
        public int TotalMatchesPlayed => Schedule.Count(m => !string.IsNullOrWhiteSpace(m.Status) && 
            string.Equals(m.Status, "Сыгран", StringComparison.OrdinalIgnoreCase));
        
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

        public ICommand AddTeamCommand { get; }
        public ICommand EditTeamCommand { get; }
        public ICommand OpenMatchCommand { get; }

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
                    // Перезагружаем команды из JSON (самый надёжный способ)
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

        private void EditTeam(object parameter)
        {
            var team = parameter as Team;
            if (team == null)
                return;

            var currentWindow = Application.Current.Windows
                .OfType<TournamentWindow>()
                .FirstOrDefault(w => w.IsActive);

            if (currentWindow != null)
            {
                var editTeamWindow = new AddTeamWindow(currentWindow, CurrentTournament.Id, team);

                if (editTeamWindow.ShowDialog() == true)
                {
                    // Перезагружаем команды из JSON
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

        /// <summary>
        /// Генерация таблицы результатов (пока только структура, без очков).
        /// Для кругового турнира — шахматная таблица (команды по алфавиту).
        /// Для остальных форматов — заглушка.
        /// </summary>
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

            // Круговой формат: строим шахматную таблицу
            var sortedTeams = Teams
                .OrderBy(t => t.Name)
                .ToList();

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

                // Создаём ячейки под результаты
                for (int j = 0; j < teamCount; j++)
                {
                    if (i == j)
                    {
                        // Self ячейка (команда против себя) - помечаем специальным значением
                        row.Cells.Add("SELF");
                    }
                    else
                    {
                        // Обычная ячейка - пока пустая
                        row.Cells.Add(string.Empty);
                    }
                }

                ResultsTable.Add(row);
            }
        }

        /// <summary>
        /// Генерация расписания на основе выбранного формата турнира.
        /// Используется паттерн "Стратегия".
        /// Загружает сохраненные матчи и обновляет их данными.
        /// </summary>
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
                // Для швейцарского, олимпийского, поэтапного и других заглушек
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

            // Создаем словарь сохраненных матчей для быстрого поиска
            var savedMatchesDict = new Dictionary<string, Match>();
            if (CurrentTournament.Matches != null)
            {
                foreach (var savedMatch in CurrentTournament.Matches)
                {
                    // Используем ключ: Team1|Team2|Round
                    string key = $"{savedMatch.Team1}|{savedMatch.Team2}|{savedMatch.Round}";
                    savedMatchesDict[key] = savedMatch;
                }
            }

            // Объединяем сгенерированные матчи с сохраненными данными
            foreach (var generatedMatch in generatedMatches)
            {
                string key = $"{generatedMatch.Team1}|{generatedMatch.Team2}|{generatedMatch.Round}";
                
                if (savedMatchesDict.TryGetValue(key, out Match savedMatch))
                {
                    // Обновляем сгенерированный матч данными из сохраненного
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
                        // Если появился счёт — меняем статус автоматически
                        var outcome = GetOutcome(match);
                        if (outcome.HasValue)
                        {
                            match.Status = "Сыгран";
                        }
                    }

                    // Сохраняем изменения матча
                    SaveMatchToTournament(match);
                }

                UpdateResultsFromMatches();
                UpdateStatistics();
            }
        }
        
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

        private void OpenMatchCard(object parameter)
        {
            var match = parameter as Match;
            if (match == null)
                return;

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

                    // Сохраняем матч в турнир
                    SaveMatchToTournament(match);
                    UpdateResultsFromMatches();
                    UpdateStatistics();
                }
            }
        }

        private void SaveMatchToTournament(Match match)
        {
            if (CurrentTournament == null || match == null) return;

            if (CurrentTournament.Matches == null)
                CurrentTournament.Matches = new List<Match>();

            // Ищем существующий матч по Id или по командам и раунду
            var existingMatch = CurrentTournament.Matches.FirstOrDefault(m => 
                m.Id == match.Id || 
                (m.Team1 == match.Team1 && m.Team2 == match.Team2 && m.Round == match.Round));

            if (existingMatch != null)
            {
                // Обновляем существующий матч
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
                // Добавляем новый матч (создаем копию)
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

            // Сохраняем турнир
            _storage.UpdateTournament(CurrentTournament);
        }

        private void UpdateResultsFromMatches()
        {
            if (CurrentTournament == null ||
                !string.Equals(CurrentTournament.Type, "Круговой", StringComparison.OrdinalIgnoreCase) ||
                Teams.Count == 0)
            {
                return;
            }

            // Перестраиваем таблицу (алфавитный порядок) и очищаем значения
            GenerateResults();

            if (ResultsTable.Count == 0)
                return;

            var sortedTeams = Teams
                .OrderBy(t => t.Name)
                .ToList();

            var nameToIndex = sortedTeams
                .Select((team, index) => new { team.Name, index })
                .ToDictionary(k => k.Name, v => v.index, StringComparer.OrdinalIgnoreCase);

            // Словарь для хранения статистики команд
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

            // Обрабатываем все матчи и заполняем статистику
            foreach (var match in Schedule)
            {
                if (match == null || string.IsNullOrWhiteSpace(match.Team1) || string.IsNullOrWhiteSpace(match.Team2))
                    continue;

                if (!teamStats.TryGetValue(match.Team1, out var team1Stats) ||
                    !teamStats.TryGetValue(match.Team2, out var team2Stats))
                {
                    continue;
                }

                // Проверяем, сыгран ли матч
                if (string.IsNullOrWhiteSpace(match.Status) ||
                    !string.Equals(match.Status, "Сыгран", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Получаем счет по сетам
                var setsScore = ParsePair(match.SetsScore);
                if (!setsScore.left.HasValue || !setsScore.right.HasValue)
                {
                    // Пробуем быстрый счет
                    var quickScore = ParsePair(match.Team1QuickScore, match.Team2QuickScore);
                    if (quickScore.left.HasValue && quickScore.right.HasValue)
                    {
                        setsScore = quickScore;
                    }
                    else
                    {
                        continue; // Нет счета - пропускаем матч
                    }
                }

                int team1Sets = setsScore.left.Value;
                int team2Sets = setsScore.right.Value;

                // Получаем общий счет по мячам
                var totalScore = ParsePair(match.TotalScore);
                int team1Points = totalScore.left ?? 0;
                int team2Points = totalScore.right ?? 0;

                // Обновляем статистику сетов
                team1Stats.SetsWon += team1Sets;
                team1Stats.SetsLost += team2Sets;
                team2Stats.SetsWon += team2Sets;
                team2Stats.SetsLost += team1Sets;

                // Обновляем статистику мячей
                team1Stats.PointsScored += team1Points;
                team1Stats.PointsConceded += team2Points;
                team2Stats.PointsScored += team2Points;
                team2Stats.PointsConceded += team1Points;

                // Определяем исход матча и начисляем очки по итальянской системе
                int team1PointsItalian = CalculateItalianPoints(team1Sets, team2Sets);
                int team2PointsItalian = CalculateItalianPoints(team2Sets, team1Sets);

                team1Stats.Points += team1PointsItalian;
                team2Stats.Points += team2PointsItalian;

                // Обновляем победы/поражения
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
                // Ничья в волейболе невозможна, но на всякий случай

                // Заполняем ячейки в таблице
                if (nameToIndex.TryGetValue(match.Team1, out int t1) &&
                    nameToIndex.TryGetValue(match.Team2, out int t2))
                {
                    string team1Cell;
                    string team2Cell;

                    if (team1Sets > team2Sets)
                    {
                        team1Cell = "1";
                        team2Cell = "0";
                    }
                    else if (team1Sets < team2Sets)
                    {
                        team1Cell = "0";
                        team2Cell = "1";
                    }
                    else
                    {
                        team1Cell = "½";
                        team2Cell = "½";
                    }

                    if (ResultsTable.ElementAtOrDefault(t1)?.Cells.Count > t2 &&
                        ResultsTable.ElementAtOrDefault(t2)?.Cells.Count > t1)
                    {
                        ResultsTable[t1].Cells[t2] = team1Cell;
                        ResultsTable[t2].Cells[t1] = team2Cell;
                    }
                }
            }

            // Рассчитываем коэффициенты для всех команд
            foreach (var stats in teamStats.Values)
            {
                // Коэффициент по сетам
                if (stats.SetsLost > 0)
                {
                    stats.SetsRatio = (double)stats.SetsWon / stats.SetsLost;
                }
                else if (stats.SetsWon > 0)
                {
                    stats.SetsRatio = double.MaxValue; // Бесконечность (деление на 0)
                }
                else
                {
                    stats.SetsRatio = 0;
                }

                // Коэффициент по мячам
                if (stats.PointsConceded > 0)
                {
                    stats.PointsRatio = (double)stats.PointsScored / stats.PointsConceded;
                }
                else if (stats.PointsScored > 0)
                {
                    stats.PointsRatio = double.MaxValue; // Бесконечность
                }
                else
                {
                    stats.PointsRatio = 0;
                }
            }

            // Создаем словарь результатов личных встреч для определения победителя при равенстве
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
                    {
                        setsScore = quickScore;
                    }
                    else
                    {
                        continue;
                    }
                }

                int team1Sets = setsScore.left.Value;
                int team2Sets = setsScore.right.Value;

                if (!headToHeadResults.ContainsKey(match.Team1))
                    headToHeadResults[match.Team1] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (!headToHeadResults.ContainsKey(match.Team2))
                    headToHeadResults[match.Team2] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                // Сохраняем результат личной встречи (1 = победа, -1 = поражение, 0 = ничья)
                if (team1Sets > team2Sets)
                {
                    headToHeadResults[match.Team1][match.Team2] = 1;
                    headToHeadResults[match.Team2][match.Team1] = -1;
                }
                else if (team1Sets < team2Sets)
                {
                    headToHeadResults[match.Team1][match.Team2] = -1;
                    headToHeadResults[match.Team2][match.Team1] = 1;
                }
                else
                {
                    headToHeadResults[match.Team1][match.Team2] = 0;
                    headToHeadResults[match.Team2][match.Team1] = 0;
                }
            }

            // Сортируем команды по месту (по очкам, затем по коэффициентам, затем по личной встрече)
            // Используем итеративную сортировку для учета личной встречи
            var sortedByPlace = teamStats.Values.ToList();
            
            // Сортируем по основным критериям
            sortedByPlace = sortedByPlace
                .OrderByDescending(s => s.Points) // Сначала по очкам (больше = лучше)
                .ThenByDescending(s => s.SetsRatio) // Затем по коэффициенту сетов
                .ThenByDescending(s => s.PointsRatio) // Затем по коэффициенту мячей
                .ToList();

            // Дополнительная сортировка по личной встрече для команд с равными показателями
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
                    // Если есть равные команды, проверяем личную встречу
                    int currentH2H = 0;
                    if (headToHeadResults.ContainsKey(currentTeam.TeamName))
                    {
                        foreach (var equalTeam in equalTeams)
                        {
                            if (headToHeadResults[currentTeam.TeamName].TryGetValue(equalTeam.TeamName, out int h2hResult))
                            {
                                currentH2H += h2hResult;
                            }
                        }
                    }

                    // Пересортируем группу равных команд по личной встрече
                    var groupStart = i;
                    var groupEnd = i;
                    while (groupEnd + 1 < sortedByPlace.Count)
                    {
                        var nextTeam = sortedByPlace[groupEnd + 1];
                        if (nextTeam.Points == currentTeam.Points &&
                            Math.Abs(nextTeam.SetsRatio - currentTeam.SetsRatio) < 0.000001 &&
                            Math.Abs(nextTeam.PointsRatio - currentTeam.PointsRatio) < 0.000001)
                        {
                            groupEnd++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (groupEnd > groupStart)
                    {
                        // Сортируем группу по личной встрече
                        var group = sortedByPlace.Skip(groupStart).Take(groupEnd - groupStart + 1).ToList();
                        group = group.OrderByDescending(t =>
                        {
                            int h2h = 0;
                            if (headToHeadResults.ContainsKey(t.TeamName))
                            {
                                foreach (var other in group.Where(ot => ot.TeamName != t.TeamName))
                                {
                                    if (headToHeadResults[t.TeamName].TryGetValue(other.TeamName, out int h2hResult))
                                    {
                                        h2h += h2hResult;
                                    }
                                }
                            }
                            return h2h;
                        }).ToList();

                        // Заменяем группу в отсортированном списке
                        for (int j = 0; j < group.Count; j++)
                        {
                            sortedByPlace[groupStart + j] = group[j];
                        }
                    }
                }
            }

            // Определяем места с учетом равенства и выводим отладочную информацию
            int currentPlace = 1;
            for (int i = 0; i < sortedByPlace.Count; i++)
            {
                if (i > 0)
                {
                    var prev = sortedByPlace[i - 1];
                    var curr = sortedByPlace[i];

                    // Если очки, коэффициент сетов и коэффициент мячей равны
                    if (prev.Points == curr.Points &&
                        Math.Abs(prev.SetsRatio - curr.SetsRatio) < 0.000001 &&
                        Math.Abs(prev.PointsRatio - curr.PointsRatio) < 0.000001)
                    {
                        // Проверяем личную встречу
                        int h2hResult = 0;
                        if (headToHeadResults.ContainsKey(prev.TeamName) &&
                            headToHeadResults[prev.TeamName].TryGetValue(curr.TeamName, out h2hResult))
                        {
                            if (h2hResult == 0)
                            {
                                // Коэффициенты равны, личная встреча ничья - отладка
                                System.Diagnostics.Debug.WriteLine(
                                    $"Коэффициенты равны для команд {prev.TeamName} и {curr.TeamName}. " +
                                    $"Очки: {prev.Points}, Коэф. сетов: {prev.SetsRatio:F6}, Коэф. мячей: {prev.PointsRatio:F6}. " +
                                    $"Личная встреча: ничья. Победитель определяется по личной встрече.");
                            }
                        }
                        else
                        {
                            // Нет личной встречи - оставляем то же место
                        }
                    }
                    else
                    {
                        currentPlace = i + 1;
                    }
                }

                sortedByPlace[i].Place = currentPlace;
            }

            // Пересоздаем таблицу с правильным порядком команд и статистикой
            // Сохраняем старую таблицу для копирования ячеек результатов
            var oldTable = ResultsTable.ToList();
            ResultsTable.Clear();

            // Создаем новую таблицу с правильным порядком
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

                // Копируем ячейки из старой таблицы, но переставляем их в правильном порядке
                newRow.Cells = new ObservableCollection<string>();
                for (int j = 0; j < sortedByPlace.Count; j++)
                {
                    var opponentName = sortedByPlace[j].TeamName;
                    if (i == j)
                    {
                        newRow.Cells.Add("SELF");
                    }
                    else if (oldRow != null && oldRow.Cells.Count > 0)
                    {
                        // Находим индекс оппонента в старой таблице
                        var oldOpponentIndex = oldTable.FindIndex(r => r.TeamName == opponentName);
                        if (oldOpponentIndex >= 0 && oldRow.Cells.Count > oldOpponentIndex)
                        {
                            newRow.Cells.Add(oldRow.Cells[oldOpponentIndex]);
                        }
                        else
                        {
                            newRow.Cells.Add(string.Empty);
                        }
                    }
                    else
                    {
                        newRow.Cells.Add(string.Empty);
                    }
                }

                ResultsTable.Add(newRow);
            }
        }

        /// <summary>
        /// Рассчитывает очки по итальянской системе для волейбола.
        /// Победа 3:0 или 3:1 = 3 очка
        /// Победа 3:2 = 2 очка
        /// Поражение 2:3 = 1 очко
        /// Поражение 0:3 или 1:3 = 0 очков
        /// </summary>
        private int CalculateItalianPoints(int setsWon, int setsLost)
        {
            if (setsWon == 3 && setsLost == 0)
                return 3; // Победа 3:0
            if (setsWon == 3 && setsLost == 1)
                return 3; // Победа 3:1
            if (setsWon == 3 && setsLost == 2)
                return 2; // Победа 3:2
            if (setsWon == 2 && setsLost == 3)
                return 1; // Поражение 2:3
            if (setsWon == 1 && setsLost == 3)
                return 0; // Поражение 1:3
            if (setsWon == 0 && setsLost == 3)
                return 0; // Поражение 0:3

            // Для других случаев (нестандартные счета) возвращаем 0
            return 0;
        }

        private double? GetOutcome(Match match)
        {
            var fromSets = ParsePair(match.SetsScore);
            if (fromSets.left.HasValue && fromSets.right.HasValue)
            {
                return CompareScores(fromSets.left.Value, fromSets.right.Value);
            }

            var fromQuick = ParsePair(match.Team1QuickScore, match.Team2QuickScore);
            if (fromQuick.left.HasValue && fromQuick.right.HasValue)
            {
                return CompareScores(fromQuick.left.Value, fromQuick.right.Value);
            }

            return null;
        }

        private (int? left, int? right) ParsePair(string score)
        {
            if (string.IsNullOrWhiteSpace(score))
                return (null, null);

            var parts = score.Split(':');
            if (parts.Length != 2)
                return (null, null);

            if (int.TryParse(parts[0].Trim(), out var left) &&
                int.TryParse(parts[1].Trim(), out var right))
            {
                return (left, right);
            }

            return (null, null);
        }

        private (int? left, int? right) ParsePair(string leftRaw, string rightRaw)
        {
            if (string.IsNullOrWhiteSpace(leftRaw) || string.IsNullOrWhiteSpace(rightRaw))
                return (null, null);

            if (int.TryParse(leftRaw.Trim(), out var left) &&
                int.TryParse(rightRaw.Trim(), out var right))
            {
                return (left, right);
            }

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