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

            IEnumerable<Match> matches;
            try
            {
                matches = strategy.GenerateSchedule(Teams.ToList()) ?? Enumerable.Empty<Match>();
            }
            catch (Exception)
            {
                ScheduleMessage = "Произошла ошибка при генерации расписания.";
                return;
            }

            foreach (var match in matches)
            {
                Schedule.Add(match);
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
                e.PropertyName == nameof(Match.Status))
            {
                var match = sender as Match;
                if (match != null && string.Equals(match.Status, "Не сыгран", StringComparison.OrdinalIgnoreCase))
                {
                    // Если появился счёт — меняем статус автоматически
                    var outcome = GetOutcome(match);
                    if (outcome.HasValue)
                    {
                        match.Status = "Сыгран";
                    }
                }

                UpdateResultsFromMatches();
            }
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

                    UpdateResultsFromMatches();
                }
            }
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