using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SportHubBase
{
    public partial class MainWindow : Window
    {
        private readonly IStorage _storage;
        private List<Tournament> _allTournaments = new List<Tournament>();
        public ObservableCollection<Tournament> Tournaments { get; set; } = new ObservableCollection<Tournament>();

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            DataContext = this;

            _storage = App.Container.GetInstance<IStorage>();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTournamentsFromDatabase();

            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            double targetWidth = screenWidth * 0.94;
            double targetHeight = screenHeight * 0.92;

            Width = Math.Max(targetWidth, MinWidth);
            Height = Math.Max(targetHeight, MinHeight);

            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;
        }

        private void LoadTournamentsFromDatabase()
        {
            try
            {
                var loaded = _storage.LoadTournaments();
                _allTournaments = loaded ?? new List<Tournament>();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки турниров: {0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (Tournaments == null) return;

            string search = SearchTextBox != null && SearchTextBox.Text != null
                ? SearchTextBox.Text.Trim()
                : string.Empty;

            string selectedSport = "Любой спорт";
            if (SportFilterComboBox != null && SportFilterComboBox.SelectedItem is ComboBoxItem sportItem && sportItem.Content != null)
            {
                selectedSport = sportItem.Content.ToString();
            }

            string selectedStorage = "Все турниры";
            if (StorageFilterComboBox != null && StorageFilterComboBox.SelectedItem is ComboBoxItem storageItem && storageItem.Content != null)
            {
                selectedStorage = storageItem.Content.ToString();
            }

            IEnumerable<Tournament> filtered = _allTournaments;

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(t =>
                    (t.Name != null && t.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.City != null && t.City.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.OwnerName != null && t.OwnerName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (!string.Equals(selectedSport, "Любой спорт", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(t => string.Equals(t.SportType, selectedSport, StringComparison.OrdinalIgnoreCase));
            }

            if (string.Equals(selectedStorage, "Локальные", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(t => !t.IsRemote);
            }
            else if (string.Equals(selectedStorage, "Удалённые (Сервер)", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(t => t.IsRemote);
            }

            Tournaments.Clear();
            foreach (var t in filtered)
            {
                Tournaments.Add(t);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SportFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTournamentsFromDatabase();
        }

        private void AddTurnament_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreateTournamentWindow();
            createWindow.Owner = this;
            createWindow.ShowDialog();
            LoadTournamentsFromDatabase();
        }

        private void ConnectRemoteButton_Click(object sender, RoutedEventArgs e)
        {
            var connectWindow = new ConnectRemoteTournamentWindow
            {
                Owner = this
            };
            if (connectWindow.ShowDialog() == true)
            {
                LoadTournamentsFromDatabase();
            }
        }

        private void TournamentItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Guid tournamentId)
            {
                var tournamentWindow = new TournamentWindow(tournamentId);
                tournamentWindow.Closed += (s, args) =>
                {
                    LoadTournamentsFromDatabase();
                };
                tournamentWindow.Show();
            }
        }
    }
}