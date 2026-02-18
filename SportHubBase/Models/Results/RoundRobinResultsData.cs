using System.Collections.Generic;
using System.Collections.ObjectModel;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.Models.Results
{
    /// <summary>
    /// Данные результатов для кругового турнира (шахматная таблица).
    /// </summary>
    public class RoundRobinResultsData : ResultsData
    {
        private IReadOnlyList<int> _headerNumbers;

        /// <summary>
        /// Номера столбцов/строк для шахматной таблицы (1..N).
        /// </summary>
        public IReadOnlyList<int> HeaderNumbers
        {
            get => _headerNumbers;
            set
            {
                if (_headerNumbers != value)
                {
                    _headerNumbers = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Строки таблицы результатов.
        /// </summary>
        public ObservableCollection<ResultRow> Rows { get; } = new ObservableCollection<ResultRow>();

        /// <summary>
        /// Экспорт круговой таблицы в Excel.
        /// </summary>
        public override void ExportToExcel(IExcelService service, string fileName)
        {
            if (service == null) return;
            var headers = new List<string> { "Место", "Команда", "В", "П", "ВП", "ПП", "Очки" };
            service.ExportResults(TournamentName, headers, Rows, fileName);
        }
    }
}
