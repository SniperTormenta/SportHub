// Models/ResultRow.cs
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    /// <summary>
    /// Одна строка шахматной таблицы результатов.
    /// Содержит название команды, ячейки результатов и статистику.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ResultRow
    {
        public int Index { get; set; }

        public string TeamName { get; set; }

        /// <summary>
        /// Ячейки по соперникам (по столбцам). ObservableCollection, чтобы UI обновлялся при замене значений.
        /// </summary>
        public ObservableCollection<CellResult> Cells { get; set; } = new ObservableCollection<CellResult>();

        // Статистика команды
        public int Wins { get; set; } // В - выигранные матчи
        public int Losses { get; set; } // П - проигранные матчи
        public int SetsWon { get; set; } // ВП - выигранные партии/сеты
        public int SetsLost { get; set; } // ПП - проигранные партии/сеты
        public int Points { get; set; } // Очки по итальянской системе
        public int Place { get; set; } // Место в таблице

        // Коэффициенты для сортировки
        public double SetsRatio { get; set; } // Коэффициент по сетам (ВП/ПП)
        public double PointsRatio { get; set; } // Коэффициент по мячам (забито/пропущено)
        public int PointsScored { get; set; } // Всего забито мячей
        public int PointsConceded { get; set; } // Всего пропущено мячей
    }
}




