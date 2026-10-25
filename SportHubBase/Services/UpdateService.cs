using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using AutoUpdaterDotNET;
using SportHubBase.Interfaces;
using SportHubBase.View;

namespace SportHubBase.Services
{
    /// <summary>
    /// Сервис управления автоматическим обновлением приложения SportHub на базе AutoUpdater.NET.
    /// Соответствует стандартам .NET Framework 4.8.1 и C# 7.3.
    /// </summary>
    public class UpdateService : IUpdateService
    {
        private const string DefaultUpdateXmlUrl = "https://raw.githubusercontent.com/SniperTormenta/SportHub/Mainw/update.xml";

        private bool _isManualCheck;
        private Window _ownerWindow;
        private bool _isInitialized;

        /// <summary>
        /// Возвращает адрес XML-манифеста обновления (из App.config или значение по умолчанию).
        /// </summary>
        public string UpdateXmlUrl
        {
            get
            {
                string configUrl = ConfigurationManager.AppSettings["UpdateXmlUrl"];
                if (!string.IsNullOrWhiteSpace(configUrl))
                {
                    return configUrl;
                }
                return DefaultUpdateXmlUrl;
            }
        }

        /// <summary>
        /// Инициализирует модуль обновления, криптографические протоколы и путь к файлу состояния в AppData.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            // 1. Включение TLS 1.2 и TLS 1.3 (12288) для совместимости с серверами GitHub
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | (SecurityProtocolType)12288;
            }
            catch
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }

            // 2. Хранение служебных данных библиотеки в JSON в папке AppData/SportHub
            string appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SportHub"
            );

            if (!Directory.Exists(appDataDirectory))
            {
                Directory.CreateDirectory(appDataDirectory);
            }

            string stateFilePath = Path.Combine(appDataDirectory, "update_state.json");
            AutoUpdater.PersistenceProvider = new JsonFilePersistenceProvider(stateFilePath);

            // 3. Базовая конфигурация AutoUpdater
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.ShowSkipButton = true;
            AutoUpdater.ShowRemindLaterButton = true;
            AutoUpdater.Synchronous = false;

            // 4. Подписка на события проверки и завершения
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;

            _isInitialized = true;
        }

        /// <summary>
        /// Запускает фоновую тихую проверку обновлений при старте программы.
        /// </summary>
        public void CheckForUpdatesInBackground()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            _isManualCheck = false;
            _ownerWindow = null;
            AutoUpdater.ReportErrors = false;

            StartUpdateCheckInternal();
        }

        /// <summary>
        /// Запускает интерактивную ручную проверку обновлений по запросу пользователя.
        /// </summary>
        /// <param name="owner">Родительское окно для модальных сообщений.</param>
        public void CheckForUpdatesManual(Window owner)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            _isManualCheck = true;
            _ownerWindow = owner;
            AutoUpdater.ReportErrors = true;

            StartUpdateCheckInternal();
        }

        private void StartUpdateCheckInternal()
        {
            try
            {
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                AutoUpdater.Start(UpdateXmlUrl, executingAssembly);
            }
            catch (Exception ex)
            {
                if (_isManualCheck)
                {
                    ShowErrorMessage(string.Format("Не удалось запустить проверку обновлений: {0}", ex.Message));
                }
            }
        }

        private void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args == null)
            {
                if (_isManualCheck)
                {
                    ShowErrorMessage("Сервер обновлений вернул пустой ответ или недоступен.");
                }
                return;
            }

            if (args.Error != null)
            {
                if (_isManualCheck)
                {
                    ShowErrorMessage(string.Format("Ошибка при проверке обновлений: {0}", args.Error.Message));
                }
                return;
            }

            if (args.IsUpdateAvailable)
            {
                // Найдена новая версия — отображаем WPF окно обновления
                Application.Current.Dispatcher.Invoke(new Action(delegate
                {
                    Window parent = _ownerWindow;
                    if (parent == null || !parent.IsLoaded)
                    {
                        parent = Application.Current.MainWindow;
                    }

                    UpdateAvailableWindow window = new UpdateAvailableWindow(args);
                    if (parent != null && parent.IsLoaded)
                    {
                        window.Owner = parent;
                    }

                    window.ShowDialog();
                }));
            }
            else
            {
                // Обновлений нет
                if (_isManualCheck)
                {
                    Application.Current.Dispatcher.Invoke(new Action(delegate
                    {
                        Window parent = _ownerWindow;
                        if (parent == null || !parent.IsLoaded)
                        {
                            parent = Application.Current.MainWindow;
                        }

                        string currentVer = args.InstalledVersion != null ? args.InstalledVersion.ToString() : "текущая";
                        MessageBox.Show(
                            parent,
                            string.Format("У вас установлена актуальная версия SportHub ({0}). Обновлений не требуется.", currentVer),
                            "Проверка обновлений",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }));
                }
            }
        }

        private void AutoUpdater_ApplicationExitEvent()
        {
            // Корректное завершение работы приложения перед заменой файлов
            Application.Current.Dispatcher.Invoke(new Action(delegate
            {
                Application.Current.Shutdown();
            }));
        }

        private void ShowErrorMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(new Action(delegate
            {
                Window parent = _ownerWindow;
                if (parent == null || !parent.IsLoaded)
                {
                    parent = Application.Current.MainWindow;
                }

                MessageBox.Show(
                    parent,
                    message,
                    "Обновление приложения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }));
        }
    }
}
