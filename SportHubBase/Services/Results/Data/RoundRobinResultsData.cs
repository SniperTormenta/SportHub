// Services/Results/Data/RoundRobinResultsData.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportHubBase.Services.Results.Data
{
    /// <summary>
    /// Данные для кругового турнира (шахматная таблица).
    /// </summary>
    public class RoundRobinResultsData : ResultsData
    {
        public ObservableCollection<ResultRow> Rows { get; set; } = new ObservableCollection<ResultRow>();
        
        /// <summary>
        /// Номера столбцов (1..N) для заголовка таблицы.
        /// Read-only, так как задаются при создании.
        /// </summary>
        public IReadOnlyList<int> HeaderNumbers { get; set; }

        public override void ExportToExcel(IExcelService excelService, string tournamentName, string filePath)
        {
            if (excelService == null) return;
            
            var headers = new List<string> { "Место", "Команда", "В", "П", "ВП", "ПП", "Очки" };
            excelService.ExportResults(tournamentName, headers, Rows, filePath);
        }
    }
}
