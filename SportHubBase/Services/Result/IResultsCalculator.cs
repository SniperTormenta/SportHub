// Services/Results/IResultsCalculator.cs
using SportHubBase.Models;
using System.Collections.ObjectModel;

namespace SportHubBase.Services.Results
{
    /// Интерфейс калькулятора результатов и таблицы по виду спорта.
    /// Вызывается из TournamentViewModel для обновления ResultsTable и статистики.
    /// Часть инверсии зависимостей: разные виды спорта — разные реализации.
    public interface IResultsCalculator
    {
        /// Короткое имя калькулятора (для отладки/сообщений).
        string Name { get; }
        /// Основной метод: рассчитывает таблицу результатов и статистику на основе матчей.
        /// Турнир (для доступа к Type, Teams)
        /// Список матчей (сыгранные влияют на результаты)
        /// Коллекция для заполнения (очищается и перезаполняется внутри)
        /// Сообщение о статусе (e.g. "в разработке")
        void Calculate(Tournament tournament,
                       ObservableCollection<Match> schedule,
                       ObservableCollection<ResultRow> resultsTable,
                       out string resultsMessage);
    }
}