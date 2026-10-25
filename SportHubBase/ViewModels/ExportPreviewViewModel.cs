using System;
using SportHubBase.Interfaces;
using SportHubBase.Services.Results.Data;

namespace SportHubBase.ViewModels
{
    /// <summary>
    /// ViewModel предпросмотра и экспорта таблицы результатов.
    /// </summary>
    public class ExportPreviewViewModel : BaseViewModel
    {
        private readonly IImageEncoderStrategyFactory _encoderFactory;

        public string TournamentName { get; }
        public string TableTitle { get; }
        public RoundRobinResultsData ResultsData { get; }
        public IImageEncoderStrategyFactory EncoderFactory => _encoderFactory;
        public RelayCommand CloseCommand { get; }

        public event Action<object, bool> RequestClose;

        public ExportPreviewViewModel(
            string tournamentName,
            string tableTitle,
            RoundRobinResultsData resultsData,
            IImageEncoderStrategyFactory encoderFactory)
        {
            TournamentName = tournamentName ?? "Турнир";
            TableTitle = tableTitle ?? "Таблица результатов";
            ResultsData = resultsData ?? throw new ArgumentNullException(nameof(resultsData));
            _encoderFactory = encoderFactory ?? throw new ArgumentNullException(nameof(encoderFactory));
            CloseCommand = new RelayCommand(_ => Close(false));
        }

        public void Close(bool result)
        {
            RequestClose?.Invoke(this, result);
        }
    }
}
