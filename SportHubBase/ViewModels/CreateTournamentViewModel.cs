// ViewModels/CreateTournamentViewModel.cs (для окна создания)
using System;
using System.Collections.Generic;
using System.Windows.Input;
using SportHubBase.Interfaces;
using SportHubBase.Models;

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

        // Свойства из формы (биндим к UI)
        /// Название турнира (биндинг TwoWay к TextBox).
        public string Name { get; set; }

        /// Список администраторов (по умолчанию текущий пользователь как владелец; динамически редактируется в UI).
        /// В UI: ListView или коллекция с добавлением/удалением.
        public List<Admin> Admins { get; set; } = new List<Admin> { new Admin { Name = "Фарватер", IsOwner = true } };

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

        /// Город проведения (TextBox).
        public string City { get; set; }

        /// Контакты (TextBox).
        public string Contacts { get; set; }

        /// Дата начала (DatePicker; дефолт сегодня).
        public DateTime StartDate { get; set; } = DateTime.Now;

        // Команда создания
        /// Команда для кнопки "Создать турнир" (биндинг в XAML).
        /// Всегда активна (canExecute null); выполняет CreateTournament.
        public ICommand CreateCommand { get; }

        /// Конструктор: инициализирует команду.
        public CreateTournamentViewModel(IStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            CreateCommand = new RelayCommand(CreateTournament);
        }

        /// Метод выполнения команды: создаёт объект Tournament из свойств VM, сохраняет через сервис.
        /// После сохранения — турнир появляется в списке (в главном окне, если реализован MainViewModel с LoadTournaments).
        /// Улучшение: Добавить MessageBox или событие OnTournamentCreated; валидацию (if string.IsNullOrEmpty(Name) return;); обработку ошибок.
        private void CreateTournament(object parameter)
        {
            var tournament = new Tournament
            {
                Name = Name,
                Admins = Admins,
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
                StartDate = StartDate
                // EndDate = null по умолчанию (идёт)
                // Teams/Matches = пустые списки
            };

            _storage.CreateTournament(tournament);

            // Закрыть окно или показать сообщение
            // Варианты: вызвать событие, передать Action в конструктор, или использовать DialogResult в окне
        }
    }
}