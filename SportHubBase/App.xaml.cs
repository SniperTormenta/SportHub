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

            // ── ВЫБОР ИНФРАСТРУКТУРЫ БД ─────────────────────────────────────────
            // В будущем ты можешь читать этот флаг и строку подключения из файла настроек
            // или даже сделать окно выбора перед запуском приложения.
            bool useSqlServer = false;
            string sqlConnectionString = "Server=ТВОЙ_IP_АДРЕС;Database=ТВОЯ_БД;User Id=ЛОГИН;Password=ПАРОЛЬ;TrustServerCertificate=True;";

            if (useSqlServer)
            {
                // Регистрируем MS SQL Server
                // Используем делегат, потому что конструктор требует строку подключения
                Container.Register(() => new SqlServerDatabaseService(sqlConnectionString), Lifestyle.Singleton);
                Container.Register<IStorage, SqlServerStorageService>(Lifestyle.Singleton);
            }
            else
            {
                // Регистрируем старую добрую локальную SQLite
                Container.Register<SqliteDatabaseService>(Lifestyle.Singleton);
                Container.Register<IStorage, SqliteStorageService>(Lifestyle.Singleton);
            }
            // ────────────────────────────────────────────────────────────────────

            // ── Прочие сервисы ──────────────────────────────────────────────────
            // Обрати внимание: этим сервисам ВООБЩЕ без разницы, что мы выбрали выше. 
            // Они просят IStorage, и контейнер даст им то, что активно!
            Container.Register<IMatchService, MatchService>(Lifestyle.Singleton);
            Container.Register<IAccountService, AccountService>(Lifestyle.Singleton);

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
