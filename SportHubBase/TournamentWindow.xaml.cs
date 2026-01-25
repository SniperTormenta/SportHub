using System;
using System.Windows;
using SportHubBase.Interfaces;
using SportHubBase.ViewModels;

namespace SportHubBase
{
    public partial class TournamentWindow : Window
    {
        public TournamentWindow(Guid tournamentId)
        {
            if (tournamentId == Guid.Empty)
                throw new ArgumentException("Tournament ID cannot be empty", nameof(tournamentId));

            InitializeComponent();
            Loaded += TournamentWindow_Loaded;

            // Создание ViewModel через контейнер зависимостей
            var storage = App.Container.GetInstance<IStorage>();
            var scheduleFactory = App.Container.GetInstance<IScheduleStrategyFactory>();
            var resultsFactory = App.Container.GetInstance<IResultsCalculatorFactory>();
            var statisticsFactory = App.Container.GetInstance<IStatisticsCalculatorFactory>();

            DataContext = new TournamentViewModel(tournamentId, storage, scheduleFactory, resultsFactory, statisticsFactory);
        }

        private void TournamentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Получаем размеры рабочей области экрана (без панели задач)
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            // Задаём размер окна — например, 95% от экрана, чтобы оставались видимыми границы
            Width = screenWidth * 0.95;
            Height = screenHeight * 0.95;

            // Центрируем окно (хотя WindowStartupLocation="CenterScreen" уже это делает,
            // но на случай, если размер изменился)
            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;
        }
    }
}