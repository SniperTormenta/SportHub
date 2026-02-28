using SimpleInjector;
using SportHubBase.Interfaces;
using SportHubBase.Services;
using SportHubBase.Services.Export;
using SportHubBase.Services.Results;
using SportHubBase.Services.Scheduling;
using SportHubBase.Services.Scoring;
using SportHubBase.Services.Statistics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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

            // Настройка контейнера зависимостей
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

            // Регистрация сервисов
            Container.Register<IStorage, JsonStorageService>(Lifestyle.Singleton);
            Container.Register<IMatchService, MatchService>(Lifestyle.Singleton);

            // Регистрация фабрик
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

            // Регистрация сервисов для аккаунтов
            Container.Register<SqliteDatabaseService>(Lifestyle.Singleton);
            Container.Register<IAccountService, AccountService>(Lifestyle.Singleton);
            Container.Register<AuthViewModel>(Lifestyle.Transient);

            // Верификация контейнера
            Container.Verify();
        }
    }
}
