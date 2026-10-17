using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SportHubBase.Converters;
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
    /// ViewModel для окна предпросмотра экспорта таблицы результатов турнира в стиле АЛВ.
    /// </summary>
    public class ExportPreviewViewModel : BaseViewModel
    {
        private readonly IImageEncoderStrategyFactory _encoderFactory;
        private readonly Tournament _tournament;
        private readonly IStorage _storage;

        public string TournamentName { get; }
        public string TableTitle { get; }
        public string SubtitleText { get; }
        public ExportTableType TableType { get; }

        public ObservableCollection<int> HeaderNumbers { get; }
        public ObservableCollection<ResultRow> Rows { get; }

        /// <summary>
        /// Фабрика стратегий кодирования (PNG/JPG), доступна окну для сохранения.
        /// </summary>
        public IImageEncoderStrategyFactory EncoderFactory => _encoderFactory;

        private string _cardBackgroundData;
        public string CardBackgroundData
        {
            get => _cardBackgroundData;
            set
            {
                if (_cardBackgroundData != value)
                {
                    _cardBackgroundData = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasCustomBackground));
                }
            }
        }

        public bool HasCustomBackground => !string.IsNullOrWhiteSpace(_cardBackgroundData);

        public ICommand SelectBackgroundCommand { get; }
        public ICommand ResetBackgroundCommand { get; }
        public RelayCommand CloseCommand { get; }

        public event Action<object, bool> RequestClose;

        /// <summary>
        /// Публичный метод для закрытия окна с результатом.
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
            IImageEncoderStrategyFactory encoderFactory,
            Tournament tournament = null,
            IStorage storage = null)
        {
            TournamentName = tournamentName ?? "Турнир";
            TableTitle = tableTitle ?? "Таблица результатов";
            TableType = tableType;
            HeaderNumbers = headerNumbers ?? new ObservableCollection<int>();
            Rows = rows ?? new ObservableCollection<ResultRow>();
            _encoderFactory = encoderFactory ?? throw new ArgumentNullException(nameof(encoderFactory));
            _tournament = tournament;
            _storage = storage;

            if (_tournament != null)
            {
                _cardBackgroundData = _tournament.CardBackgroundData;
                if (!string.IsNullOrWhiteSpace(_tournament.City))
                {
                    string city = _tournament.City.Trim();
                    string cityPrefix = city.StartsWith("г.", StringComparison.OrdinalIgnoreCase) ? city : "г. " + city;
                    SubtitleText = cityPrefix + " / " + TableTitle;
                }
                else
                {
                    SubtitleText = TableTitle;
                }
            }
            else
            {
                SubtitleText = TableTitle;
            }

            SelectBackgroundCommand = new RelayCommand(_ => SelectBackground());
            ResetBackgroundCommand = new RelayCommand(_ => ResetBackground());
            CloseCommand = new RelayCommand(_ => Close(false));
        }

        private void SelectBackground()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите изображение заднего фона карточки",
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string encoded = ImageStorageHelper.EncodeImageFileToBase64(dialog.FileName, 1600, 1000, true);
                    CardBackgroundData = encoded;
                    if (_tournament != null)
                    {
                        _tournament.CardBackgroundData = encoded;
                        if (_storage != null && _storage.IsTournamentEditable(_tournament))
                        {
                            _storage.UpdateTournament(_tournament);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Не удалось загрузить фоновое изображение:\n" + ex.Message,
                        "Ошибка загрузки фона",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ResetBackground()
        {
            CardBackgroundData = string.Empty;
            if (_tournament != null)
            {
                _tournament.CardBackgroundData = string.Empty;
                if (_storage != null && _storage.IsTournamentEditable(_tournament))
                {
                    _storage.UpdateTournament(_tournament);
                }
            }
        }
    }
}
