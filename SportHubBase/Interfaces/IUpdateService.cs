using System.Windows;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса управления автообновлением приложения.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Инициализирует проверку GitHub Releases. Пропуск хранится только до завершения приложения.
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
