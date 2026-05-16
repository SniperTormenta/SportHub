// Services/Results/Data/ResultsData.cs
using SportHubBase.Interfaces;
using System;

namespace SportHubBase.Services.Results.Data
{
    /// <summary>
    /// Базовый класс для всех типов данных результатов.
    /// Полиморфный объект, который ViewModel передаёт в View для отображения.
    /// </summary>
    public abstract class ResultsData
    {
        public string StatusMessage { get; set; }
        public string SportType { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.Now;

        /// <summary>
        /// Экспорт результатов в Excel.
        /// Базовая реализация ничего не делает (для типов, где экспорт не поддерживается).
        /// </summary>
        public virtual void ExportToExcel(IExcelService excelService, string tournamentName, string filePath)
        {
            // По умолчанию ничего не делаем.
            // Можно кинуть исключение "Не поддерживается" или просто игнорировать.
        }
    }
}
