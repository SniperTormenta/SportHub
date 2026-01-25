using System;
using System.Collections.ObjectModel;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.ViewModels
{
    /// <summary>
    /// Тип экспортируемой таблицы (результаты, расписание и т.п.).
    /// Сейчас используется только для текстов, но позволяет масштабировать решение.
    /// </summary>
    public enum ExportTableType
    {
        Results,
        Schedule
    }

    /// <summary>
    /// ViewModel для окна предпросмотра экспорта таблицы.
    /// </summary>
    public class ExportPreviewViewModel : BaseViewModel
    {
        private readonly IImageEncoderStrategyFactory _encoderFactory;

        public string TournamentName { get; }
        public string TableTitle { get; }
        public ExportTableType TableType { get; }

        public ObservableCollection<int> HeaderNumbers { get; }
        public ObservableCollection<ResultRow> Rows { get; }

        /// <summary>
        /// Фабрика стратегий кодирования (PNG/JPG), доступна окну для сохранения.
        /// </summary>
        public IImageEncoderStrategyFactory EncoderFactory => _encoderFactory;

        public event Action<object, bool> RequestClose;

        public RelayCommand CloseCommand { get; }

        /// <summary>
        /// Публичный метод для закрытия окна с результатом.
        /// Используется окном для уведомления ViewModel о необходимости закрытия.
        /// </summary>
        public void Close(bool result)
        {
            RequestClose?.Invoke(this, result);
        }

        public ExportPreviewViewModel(
            string tournamentName,
            string tableTitle,
            ObservableCollection<int> headerNumbers,
            ObservableCollection<ResultRow> rows,
            ExportTableType tableType,
            IImageEncoderStrategyFactory encoderFactory)
        {
            TournamentName = tournamentName ?? "Турнир";
            TableTitle = tableTitle ?? "Таблица";
            TableType = tableType;
            HeaderNumbers = headerNumbers ?? new ObservableCollection<int>();
            Rows = rows ?? new ObservableCollection<ResultRow>();
            _encoderFactory = encoderFactory ?? throw new ArgumentNullException(nameof(encoderFactory));

            CloseCommand = new RelayCommand(_ => Close(false));
        }
    }
}

