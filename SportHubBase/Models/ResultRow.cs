// Models/ResultRow.cs
using System.Collections.ObjectModel;

namespace SportHubBase.Models
{
    /// <summary>
    /// Одна строка шахматной таблицы результатов.
    /// Пока содержит только название команды и пустые ячейки под результаты.
    /// </summary>
    public class ResultRow
    {
        public int Index { get; set; }

        public string TeamName { get; set; }

        /// <summary>
        /// Ячейки по соперникам (по столбцам). ObservableCollection, чтобы UI обновлялся при замене значений.
        /// </summary>
        public ObservableCollection<string> Cells { get; set; } = new ObservableCollection<string>();
    }
}




