// ViewModels/CreateTournamentViewModel.cs (для окна создания)
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services;

namespace SportHubBase.ViewModels
{
    /// ViewModel для вспомогательного окна создания нового турнира.
    /// Наследует BaseViewModel для уведомлений и команд (MVVM).
    /// Свойства биндятся к элементам UI в CreateTournamentWindow.xaml (TextBox, ComboBox, CheckBox, DatePicker).
    /// Логика создания: маппинг свойств в Tournament (Model), сохранение через JsonStorageService.
    /// В архитектуре: Отдельный UI-уровень (вспомогательное окно); Type используется позже фабрикой для IScheduleStrategy.
    /// Улучшения: Инжектировать JsonStorageService через конструктор (IoC); добавить валидацию (e.g. Name не пустой); уведомлять о создании (событие или messenger); закрывать окно после успеха (через Action или событие).
    public class CreateTournamentViewModel : BaseViewModel
    {
        /// Сервис хранения (JsonStorage). Инжектируется через конструктор.
        private readonly IStorage _storage;

        /// Сервис для загрузки городов.
        private readonly CitiesService _citiesService;

        /// Все доступные города.
        private List<string> _allCities = new List<string>();

        /// Отфильтрованные города для отображения в ComboBox.
        private ObservableCollection<string> _filteredCities = new ObservableCollection<string>();

        // Свойства из формы (биндим к UI)
        /// Название турнира (биндинг TwoWay к TextBox).
        public string Name { get; set; }


        /// URL логотипа турнира (биндинг к TextBox; в UI — предпросмотр через Converter).
        public string LogoUrl { get; set; }

        /// Статус турнира (дефолт "Активный"; ComboBox в UI).
        public string Status { get; set; } = "Активный";

        /// Вид спорта (дефолт "Другое"; ComboBox).
        public string SportType { get; set; } = "Другое";

        /// Формат турнира (дефолт "Круговой"; ComboBox). Используется фабрикой ScheduleStrategyFactory для выбора IScheduleStrategy.
        public string Type { get; set; } = "Круговой";

        /// Режим редактирования (ComboBox).
        public string EditMode { get; set; } = "Только администраторы турнира";

        /// Режим доступа (ComboBox).
        public string AccessMode { get; set; } = "Доступно всем";
        public bool NoScore { get; set; }

        /// Описание турнира (MultiLine TextBox).
        public string Description { get; set; }

        private int _totalRounds = 7;
        public int TotalRounds
        {
            get => _totalRounds;
            set
            {
                if (_totalRounds != value)
                {
                    _totalRounds = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _expectedTeams = 16;
        public int ExpectedTeams
        {
            get => _expectedTeams;
            set
            {
                if (_expectedTeams != value)
                {
                    _expectedTeams = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RecommendedRoundsHint));
                }
            }
        }

        public string RecommendedRoundsHint
        {
            get
            {
                var (min, max, rec, _) = GetSwissRoundsLimits(ExpectedTeams);
                return $"Рекомендуется {rec} туров для {ExpectedTeams} команд.\nМинимум: {min} | Максимум: {max}";
            }
        }

        /// Город проведения.
        private string _city;
        public string City
        {
            get => _city;
            set
            {
                if (_city != value)
                {
                    _city = value;
                    OnPropertyChanged();
                    // Не фильтруем автоматически при установке города
                }
            }
        }

        /// Отфильтрованные города для ComboBox.
        public ObservableCollection<string> FilteredCities
        {
            get => _filteredCities;
            set
            {
                _filteredCities = value;
                OnPropertyChanged();
            }
        }

        /// Контакты (TextBox).
        public string Contacts { get; set; }

        /// Дата начала (DatePicker; дефолт сегодня).
        public DateTime StartDate { get; set; } = DateTime.Now;

        // Команда создания
        /// Команда для кнопки "Создать турнир" (биндинг в XAML).
        /// Всегда активна (canExecute null); выполняет CreateTournament.
        public ICommand CreateCommand { get; }

        /// Конструктор: инициализирует команду и загружает города.
        public CreateTournamentViewModel(IStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _citiesService = new CitiesService();

            CreateCommand = new RelayCommand(CreateTournament);
            ApplyRecommendedRoundsCommand = new RelayCommand(_ => 
            {
                var limits = GetSwissRoundsLimits(ExpectedTeams);
                TotalRounds = limits.recMin;
            });

            LoadCities();
        }

        public ICommand ApplyRecommendedRoundsCommand { get; private set; }

        private (int min, int max, int recMin, int recMax) GetSwissRoundsLimits(int teamCount)
        {
            int min;
            if (teamCount <= 6) min = 4;
            else if (teamCount <= 10) min = 5;
            else if (teamCount <= 16) min = 6;
            else if (teamCount <= 24) min = 7;
            else if (teamCount <= 32) min = 8;
            else min = (int)Math.Ceiling(Math.Log(teamCount, 2)) + 3; // Экстраполяция

            int rec = min + 1;
            int max = min + 2;

            return (min, max, rec, rec);
        }

        /// Метод выполнения команды: создаёт объект Tournament из свойств VM, сохраняет через сервис.
        /// Имя текущего пользователя для отображения в качестве владельца.
        public string OwnerName => CurrentSession.Username;

        /// Метод выполнения команды: создаёт объект Tournament из свойств VM, сохраняет через сервис.
        /// После сохранения — турнир появляется в списке (в главном окне, если реализован MainViewModel с LoadTournaments).
        /// Улучшение: Добавить MessageBox или событие OnTournamentCreated; валидацию (if string.IsNullOrEmpty(Name) return;); обработку ошибок.
        private void CreateTournament(object parameter)
        {
            var tournament = new Tournament
            {
                Name = Name,
                LogoUrl = LogoUrl,
                Status = Status,
                SportType = SportType,
                Type = Type, // Важно для стратегии в Scheduling
                EditMode = EditMode,
                AccessMode = AccessMode,
                NoScore = NoScore,
                Description = Description,
                City = City,
                Contacts = Contacts,
                StartDate = StartDate,
                // Ставим текущего пользователя владельцем (Id приводим к строке)
                OwnerId = CurrentSession.CurrentUser != null 
                    ? CurrentSession.CurrentUser.Id.ToString() 
                    : null
                // EndDate = null по умолчанию (идёт)
                // Teams/Matches = пустые списки
            };

            _storage.CreateTournament(tournament);

            // Закрыть окно или показать сообщение
            // Варианты: вызвать событие, передать Action в конструктор, или использовать DialogResult в окне
        }

        /// Загружает все города из JSON файла.
        private void LoadCities()
        {
            _allCities = _citiesService.LoadCities();
            // Показываем все города изначально
            FilteredCities = new ObservableCollection<string>(_allCities);
        }

        /// Фильтрует города по введённому тексту.
        public void FilterCitiesByText(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                // Если текст пустой, показываем все города
                FilteredCities = new ObservableCollection<string>(_allCities);
            }
            else
            {
                // Фильтруем города, которые содержат введенный текст
                var filtered = _allCities
                    .Where(c => c.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                FilteredCities = new ObservableCollection<string>(filtered);
            }
        }
    }
}