using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SportHubBase
{
    public partial class MainWindow : Window
    {
        private readonly IStorage _storage;
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
            Tournaments.Clear();

            try
            {
                var loaded = _storage.LoadTournaments();
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
                MessageBox.Show(
                    string.Format("Ошибка загрузки турниров: {0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddTurnament_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreateTournamentWindow();
            createWindow.Owner = this;
            createWindow.ShowDialog();
            // После закрытия окна создания — перезагружаем список из БД
            LoadTournamentsFromDatabase();
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = App.Container.GetInstance<AccountViewModel>();
            var accountWindow = new View.AccountWindow { DataContext = viewModel, Owner = this };
            accountWindow.ShowDialog();
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