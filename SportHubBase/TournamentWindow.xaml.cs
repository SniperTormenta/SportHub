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
            var resultsFactory = App.Container.GetInstance<IResultsProviderFactory>();
            var statisticsFactory = App.Container.GetInstance<IStatisticsCalculatorFactory>();
            var imageEncoderFactory = App.Container.GetInstance<IImageEncoderStrategyFactory>();
            var matchService = App.Container.GetInstance<IMatchService>();
            var excelService = App.Container.GetInstance<IExcelService>();

            DataContext = new TournamentViewModel(tournamentId, storage, scheduleFactory, resultsFactory, statisticsFactory, imageEncoderFactory, matchService, excelService);
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

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.TabControl tabControl)
            {
                var selectedTab = tabControl.SelectedItem as System.Windows.Controls.TabItem;
                if (selectedTab != null && selectedTab.Header.ToString() == "Результаты")
                {
                    // Обновляем результаты при переключении на вкладку "Результаты"
                    var viewModel = DataContext as TournamentViewModel;
                    viewModel?.RefreshResults();
                }
            }
        }
    }
}