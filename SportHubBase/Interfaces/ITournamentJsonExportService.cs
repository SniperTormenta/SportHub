using SportHubBase.Models;
using SportHubBase.Models.Export;
using SportHubBase.Services.Results.Data;
using SportHubBase.ViewModels;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Контракт сервиса экспорта всех данных конкретного турнира в JSON-формат (для сайта).
    /// </summary>
    public interface ITournamentJsonExportService
    {
        /// <summary>
        /// Формирует структурированную DTO-модель со всеми данными турнира.
        /// </summary>
        TournamentExportModel BuildExportModel(Tournament tournament, ResultsData resultsData, TournamentStatistics statistics);

        /// <summary>
        /// Сериализует все данные турнира в отформатированную JSON-строку.
        /// </summary>
        string ExportToJsonString(Tournament tournament, ResultsData resultsData, TournamentStatistics statistics);

        /// <summary>
        /// Сохраняет все данные турнира в указанный JSON-файл.
        /// </summary>
        void ExportToFile(Tournament tournament, ResultsData resultsData, TournamentStatistics statistics, string filePath);

        /// <summary>
        /// Открывает стандартный диалог сохранения файла и сохраняет JSON.
        /// Возвращает путь к сохранённому файлу или null, если пользователь отменил диалог.
        /// </summary>
        string ExportWithDialog(Tournament tournament, ResultsData resultsData, TournamentStatistics statistics);
    }
}
