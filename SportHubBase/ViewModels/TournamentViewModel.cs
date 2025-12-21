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

            foreach (var match in Schedule)
            {
                if (match == null || string.IsNullOrWhiteSpace(match.Team1) || string.IsNullOrWhiteSpace(match.Team2))
                    continue;

                if (!nameToIndex.TryGetValue(match.Team1, out int t1) ||
                    !nameToIndex.TryGetValue(match.Team2, out int t2))
                {
                    continue;
                }

                var outcome = GetOutcome(match);
                if (!outcome.HasValue)
                    continue;

                string team1Cell;
                string team2Cell;

                if (outcome.Value == 1d)
                {
                    team1Cell = "1";
                    team2Cell = "0";
                }
                else if (outcome.Value == 0d)
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