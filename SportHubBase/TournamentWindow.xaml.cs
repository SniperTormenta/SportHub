using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using SportHubBase.Models;

namespace SportHubBase
{
    public partial class TournamentWindow : Window
    {
        public Tournament CurrentTournament { get; private set; }

        public TournamentWindow(Guid tournamentId)
        {
            InitializeComponent();

            LoadTournament(tournamentId);
        }

        // Конструктор по умолчанию (для дизайнера и тестов)
        public TournamentWindow() : this(Guid.Empty) { }

        private void LoadTournament(Guid tournamentId)
        {
            if (tournamentId == Guid.Empty)
            {
                MessageBox.Show("ID турнира не передан.");
                return;
            }

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tournaments.json");

            if (!File.Exists(filePath))
            {
                MessageBox.Show("Файл с турнирами не найден.");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var tournaments = JsonConvert.DeserializeObject<List<Tournament>>(json);

                CurrentTournament = tournaments?.Find(t => t.Id == tournamentId);

                if (CurrentTournament != null)
                {
                    DataContext = CurrentTournament;
                    Title = CurrentTournament.Name;
                }
                else
                {
                    MessageBox.Show("Турнир не найден.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}