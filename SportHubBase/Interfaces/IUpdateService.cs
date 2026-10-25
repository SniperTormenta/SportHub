using System.Windows;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса управления автообновлением приложения.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Инициализирует модуль обновления, криптографические протоколы (TLS 1.2/1.3) и провайдер хранения настроек.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Запускает тихую фоновую проверку обновлений (без блокировки UI и без окон при отсутствии новой версии).
        /// </summary>
        void CheckForUpdatesInBackground();

        /// <summary>
        /// Запускает ручную проверку обновлений с отображением диалоговых окон о результате.
        /// </summary>
        /// <param name="owner">Родительское окно для модальных диалогов.</param>
        void CheckForUpdatesManual(Window owner);
    }
}
