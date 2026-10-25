using System;
using System.Windows;
using AutoUpdaterDotNET;

namespace SportHubBase.View
{
    /// <summary>
    /// Окно уведомления о доступности обновления приложения (WPF).
    /// </summary>
    public partial class UpdateAvailableWindow : Window
    {
        private readonly UpdateInfoEventArgs _args;

        /// <summary>
        /// Конструктор окна обновления.
        /// </summary>
        /// <param name="args">Аргументы информации об обновлении от AutoUpdater.NET.</param>
        public UpdateAvailableWindow(UpdateInfoEventArgs args)
        {
            InitializeComponent();

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _args = args;

            TxtVersionInfo.Text = string.Format(
                "Установленная версия: {0}   ➔   Новая версия: {1}",
                _args.InstalledVersion != null ? _args.InstalledVersion.ToString() : "—",
                _args.CurrentVersion != null ? _args.CurrentVersion : "—"
            );

            // Если обновление обязательное — скрываем кнопки пропуска и откладывания
            if (_args.Mandatory != null && _args.Mandatory.Value)
            {
                BtnSkip.Visibility = Visibility.Collapsed;
                BtnRemindLater.Visibility = Visibility.Collapsed;
            }

            // Загрузка чейнджлога, если указан URL
            if (!string.IsNullOrEmpty(_args.ChangelogURL))
            {
                try
                {
                    ChangelogBrowser.Navigate(_args.ChangelogURL);
                }
                catch
                {
                    TxtEmptyChangelog.Visibility = Visibility.Visible;
                }
            }
            else
            {
                TxtEmptyChangelog.Visibility = Visibility.Visible;
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();

            try
            {
                if (AutoUpdater.DownloadUpdate(_args))
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Не удалось запустить процесс обновления: {0}", ex.Message),
                    "Ошибка обновления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnRemindLater_Click(object sender, RoutedEventArgs e)
        {
            if (AutoUpdater.PersistenceProvider != null)
            {
                AutoUpdater.PersistenceProvider.SetRemindLater(DateTime.Now.AddDays(2));
            }

            DialogResult = false;
            Close();
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            if (AutoUpdater.PersistenceProvider != null && !string.IsNullOrEmpty(_args.CurrentVersion))
            {
                Version versionToSkip;
                if (Version.TryParse(_args.CurrentVersion, out versionToSkip))
                {
                    AutoUpdater.PersistenceProvider.SetSkippedVersion(versionToSkip);
                }
            }

            DialogResult = false;
            Close();
        }
    }
}
