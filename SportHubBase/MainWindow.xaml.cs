using Newtonsoft.Json;
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace SportHubBase
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Tournament> Tournaments { get; set; } = new ObservableCollection<Tournament>();

        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tournaments.json");

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTournamentsFromJson();
        }

        private void LoadTournamentsFromJson()
        {
            Tournaments.Clear();

            if (!File.Exists(_jsonFilePath)) return;

            try
            {
                string json = File.ReadAllText(_jsonFilePath);
                var loaded = JsonConvert.DeserializeObject<List<Tournament>>(json);

                if (loaded != null)
                {
                    foreach (var t in loaded)
                    {
                        Tournaments.Add(t);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTurnament_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreateTournamentWindow();
            createWindow.ShowDialog(); // Чтобы после закрытия обновить список
            LoadTournamentsFromJson(); // Обновляем список
        }

        private void TournamentItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Guid tournamentId)
            {
                var tournamentWindow = new TournamentWindow(tournamentId);
                tournamentWindow.Show();
            }
        }
    }
}