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
            // Размеры рабочей области экрана (без панели задач Windows)
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            // Делаем окно примерно 92–95% от экрана — выглядит максимально большим, но остаются границы окна
            double targetWidth = screenWidth * 0.94;
            double targetHeight = screenHeight * 0.92;

            // Уважем минимальные размеры
            Width = Math.Max(targetWidth, MinWidth);
            Height = Math.Max(targetHeight, MinHeight);

            // Центрируем (на случай, если размер изменился)
            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;
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