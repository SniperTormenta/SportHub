// App.xaml.cs
using SimpleInjector;
using SportHubBase.Interfaces;
using SportHubBase.Services;
using SportHubBase.Services.Export;
using SportHubBase.Services.Results;
using SportHubBase.Services.Scheduling;
using SportHubBase.Services.Scoring;
using SportHubBase.Services.Statistics;
using System.Windows;
using SportHubBase.View;
using SportHubBase.ViewModels;

namespace SportHubBase
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Container Container { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ConfigureContainer();
            ShowAuthWindow();
        }

        private void ShowAuthWindow()
        {
            var authViewModel = Container.GetInstance<AuthViewModel>();
            var authWindow = new AuthWindow { DataContext = authViewModel };

            authViewModel.LoginSuccess += () =>
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                authWindow.Close();
            };

            authWindow.Show();
        }

        private void ConfigureContainer()
        {
            Container = new Container();

            // ── Инфраструктура БД ───────────────────────────────────────────────
            // SqliteDatabaseService создаёт схему БД при первом запуске
            Container.Register<SqliteDatabaseService>(Lifestyle.Singleton);

            // ── Хранилище данных ────────────────────────────────────────────────
            // SqliteStorageService — основное хранилище (заменяет JsonStorageService)
            Container.Register<IStorage, SqliteStorageService>(Lifestyle.Singleton);

            // ── Прочие сервисы ──────────────────────────────────────────────────
            Container.Register<IMatchService, MatchService>(Lifestyle.Singleton);
            Container.Register<IAccountService, AccountService>(Lifestyle.Singleton);

            // ── Фабрики стратегий ───────────────────────────────────────────────
            Container.Register<IScheduleStrategyFactory, ScheduleStrategyFactory>(Lifestyle.Singleton);
            Container.Register<IResultsProviderFactory, ResultsProviderFactory>(Lifestyle.Singleton);
            Container.Register<IStatisticsCalculatorFactory, StatisticsCalculatorFactory>(Lifestyle.Singleton);
            Container.Register<IImageEncoderStrategyFactory, ImageEncoderStrategyFactory>(Lifestyle.Singleton);
            Container.Register<IExcelService, ExcelService>(Lifestyle.Singleton);

            Container.Collection.Register<ISportScoreStrategy>(new[]
            {
                typeof(DefaultScoreStrategy),
                typeof(VolleyballScoreStrategy)
            }, Lifestyle.Singleton);
            Container.Register<ISportScoreStrategyFactory, SportScoreStrategyFactory>(Lifestyle.Singleton);

            // ── ViewModels ──────────────────────────────────────────────────────
            Container.Register<AuthViewModel>(Lifestyle.Transient);
            Container.Register<AccountViewModel>(Lifestyle.Transient);

            Container.Verify();
        }
    }
}
