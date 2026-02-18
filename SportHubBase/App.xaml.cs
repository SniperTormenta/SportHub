using SimpleInjector;
using SportHubBase.Interfaces;
using SportHubBase.Services;
using SportHubBase.Services.Export;
using SportHubBase.Services.Results;
using SportHubBase.Services.Scheduling;
using SportHubBase.Services.Statistics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
            Container.Register<IResultsCalculatorFactory, ResultsCalculatorFactory>(Lifestyle.Singleton);
            Container.Register<IStatisticsCalculatorFactory, StatisticsCalculatorFactory>(Lifestyle.Singleton);
            Container.Register<IImageEncoderStrategyFactory, ImageEncoderStrategyFactory>(Lifestyle.Singleton);
            Container.Register<IExcelService, ExcelService>(Lifestyle.Singleton);

            // Верификация контейнера
            Container.Verify();
        }
    }
}
