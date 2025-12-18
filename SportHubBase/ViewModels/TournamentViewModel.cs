// ViewModels/TournamentViewModel.cs
using SportHubBase.Models;
using SportHubBase.Services;
using SportHubBase.View;
using System;
using System.Collections.ObjectModel;
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
    }
}