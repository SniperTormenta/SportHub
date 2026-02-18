using System;
using SportHubBase.Interfaces;
using SportHubBase.ViewModels;

namespace SportHubBase.Models.Results
{
    /// <summary>
    /// Базовый класс для данных результатов турнира различных форматов.
    /// Позволяет использовать полиморфные шаблоны данных в UI (WPF).
    /// </summary>
    public abstract class ResultsData : BaseViewModel
    {
        private string _statusMessage;
        private DateTime _lastUpdate = DateTime.Now;

        /// <summary>
        /// Сообщение о статусе результатов (например, ошибки или "в разработке").
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Время последнего обновления данных.
        /// </summary>
        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set
            {
                if (_lastUpdate != value)
                {
                    _lastUpdate = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _tournamentName;

        /// <summary>
        /// Название турнира для заголовков и экспорта.
        /// </summary>
        public string TournamentName
        {
            get => _tournamentName;
            set
            {
                if (_tournamentName != value)
                {
                    _tournamentName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Виртуальный метод для экспорта данных конкретного типа результатов в Excel.
        /// </summary>
        public virtual void ExportToExcel(IExcelService service, string fileName)
        {
            // Базовая реализация может быть пустой или вызывать уведомление
        }
    }
}
