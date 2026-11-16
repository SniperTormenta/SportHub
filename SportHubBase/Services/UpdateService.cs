using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.View;

namespace SportHubBase.Services
{
    /// <summary>Проверка опубликованного релиза GitHub. Скачивание открывается в браузере.</summary>
    public class UpdateService : IUpdateService
    {
        private static readonly HttpClient Client = CreateClient();
        private bool _checkInProgress;
        private bool _dismissedForSession;

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SportHub/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        public void Initialize()
        {
            // Старые настройки пропуска AutoUpdater.NET не используются.
            // Состояние предложения живёт только до завершения приложения.
        }

        public void CheckForUpdatesInBackground() => StartCheck(null, false);
        public void CheckForUpdatesManual(Window owner) => StartCheck(owner, true);

        private async void StartCheck(Window owner, bool manual)
        {
            if (_checkInProgress) return;
            if (_dismissedForSession)
            {
                if (manual) ShowMessage(owner, "Предложение обновления отложено до следующего запуска SportHub.");
                return;
            }
            _checkInProgress = true;
            try
            {
                string repository = ConfigurationManager.AppSettings["GitHubRepository"] ?? "SniperTormenta/SportHub";
                if (!UpdateRelease.IsValidRepository(repository)) throw new ArgumentException("Неверно задан репозиторий GitHub.");
                var release = await GetLatestRelease(repository);
                if (Application.Current == null || Application.Current.Dispatcher.HasShutdownStarted) return;
                var installed = Assembly.GetExecutingAssembly().GetName().Version;
                if (release == null || !release.IsNewerThan(installed))
                {
                    if (manual) ShowMessage(owner, release == null ? "Доступный релиз не найден. Проверьте, что репозиторий GitHub публичный и релиз опубликован." : "Установлена актуальная версия SportHub (" + installed + ").");
                    return;
                }
                var window = new UpdateAvailableWindow(release, installed);
                var parent = owner != null && owner.IsLoaded ? owner : Application.Current.MainWindow;
                if (parent != null && parent.IsLoaded) window.Owner = parent;
                window.ShowDialog();
                // Закрытие крестиком также откладывает предложение до следующего запуска.
                _dismissedForSession = true;
            }
            catch (Exception ex)
            {
                ErrorLog.Write("Проверка обновлений GitHub", ex);
                if (manual && Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                    ShowMessage(owner, "Не удалось проверить обновления на GitHub. Проверьте подключение к интернету и повторите попытку.");
            }
            finally { _checkInProgress = false; }
        }

        private static async Task<UpdateRelease> GetLatestRelease(string repository)
        {
            using (var response = await Client.GetAsync("https://api.github.com/repos/" + repository + "/releases/latest"))
            {
                if (response.StatusCode == HttpStatusCode.NotFound) return null;
                response.EnsureSuccessStatusCode();
                return UpdateRelease.FromGitHubJson(await response.Content.ReadAsStringAsync(), repository);
            }
        }

        private static void ShowMessage(Window owner, string message)
        {
            var parent = owner != null && owner.IsLoaded ? owner : Application.Current.MainWindow;
            if (parent != null && parent.IsLoaded) MessageBox.Show(parent, message, "Обновление SportHub", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show(message, "Обновление SportHub", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
