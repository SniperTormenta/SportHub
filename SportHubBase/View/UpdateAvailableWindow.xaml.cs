using System;
using System.Diagnostics;
using System.Windows;
using SportHubBase.Models;
using SportHubBase.Services;

namespace SportHubBase.View
{
    public partial class UpdateAvailableWindow : Window
    {
        private readonly UpdateRelease _release;

        public UpdateAvailableWindow(UpdateRelease release, Version installed)
        {
            _release = release ?? throw new ArgumentNullException(nameof(release));
            InitializeComponent();
            TxtVersionInfo.Text = "Установлена: " + installed + "   →   Новая версия: " + release.Version;
            TxtReleaseNotes.Text = string.IsNullOrWhiteSpace(release.Notes) ? "Описание изменений не предоставлено." : release.Notes;
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(_release.DownloadUrl.AbsoluteUri) { UseShellExecute = true });
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorLog.Write("Открытие скачивания GitHub", ex);
                MessageBox.Show(this, "Не удалось открыть браузер. Ссылка для скачивания:\n" + _release.DownloadUrl.AbsoluteUri,
                    "Обновление SportHub", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
