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

            // ── БАЗЫ ДАННЫХ И ХРАНИЛИЩА (ДИНАМИЧЕСКИЙ ВЫБОР) ─────────────────────

            // 1. Регистрируем обе СУБД (конкретные классы)
            // (SqlServerDatabaseService теперь берет строку из DatabaseConfig, поэтому конструктор без параметров)
            Container.Register<SqliteDatabaseService>(Lifestyle.Singleton);
            Container.Register<SqlServerDatabaseService>(Lifestyle.Singleton);

            // 2. Регистрируем обе реализации хранилища данных
            Container.Register<SqliteStorageService>(Lifestyle.Singleton);
            Container.Register<SqlServerStorageService>(Lifestyle.Singleton);

            // 3. Регистрируем обе реализации сервисов аккаунтов
            Container.Register<SqliteAccountService>(Lifestyle.Singleton);
            Container.Register<SqlServerAccountService>(Lifestyle.Singleton);

            // 4. ВАЖНО: Привязываем интерфейсы к Роутерам!
            // Именно эти классы будут на лету решать, к какой базе обращаться,
            // опираясь на галочку DatabaseConfig.UseSqlServer
            Container.Register<IStorage, DynamicStorageService>(Lifestyle.Singleton);
            Container.Register<IAccountService, DynamicAccountService>(Lifestyle.Singleton);

            // ────────────────────────────────────────────────────────────────────

            // ── Прочие сервисы ──────────────────────────────────────────────────
            Container.Register<IMatchService, MatchService>(Lifestyle.Singleton);

            // ── Фабрики стратегий ───────────────────────────────────────────────
            Container.Register<SportHubBase.Services.Scheduling.Swiss.IPlayedMatchesService, SportHubBase.Services.Scheduling.Swiss.PlayedMatchesService>(Lifestyle.Singleton);
            Container.Register<SportHubBase.Services.Scheduling.Swiss.ITiebreakerCalculator, SportHubBase.Services.Scheduling.Swiss.TiebreakerCalculator>(Lifestyle.Singleton);
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
