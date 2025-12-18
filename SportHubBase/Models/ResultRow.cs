// Models/ResultRow.cs
using System.Collections.Generic;

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
        /// Ячейки по соперникам (по столбцам). Сейчас заполняются пустыми строками.
        /// </summary>
        public IList<string> Cells { get; set; } = new List<string>();
    }
}


