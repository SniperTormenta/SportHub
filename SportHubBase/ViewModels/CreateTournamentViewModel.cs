// ViewModels/CreateTournamentViewModel.cs (для окна создания)
using System;
using System.Collections.Generic;
using System.Windows.Input;
using SportHubBase.Models;
using SportHubBase.Services;

namespace SportHubBase.ViewModels
{
    public class CreateTournamentViewModel : BaseViewModel
    {
        private readonly JsonStorageService _storage = new JsonStorageService();

        // Свойства из формы (биндим к UI)
        public string Name { get; set; }
        public List<Admin> Admins { get; set; } = new List<Admin> { new Admin { Name = "Фарватер", IsOwner = true } }; // Динамически
        public string LogoUrl { get; set; }
        public string Status { get; set; } = "Активный";
        public string SportType { get; set; } = "Другое";
        public string Type { get; set; } = "Круговой";
        public string EditMode { get; set; } = "Только администраторы турнира";
        public string AccessMode { get; set; } = "Доступно всем";
        public bool NoScore { get; set; }
        public string Description { get; set; }
        public string City { get; set; }
        public string Contacts { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;

        // Команда создания
        public ICommand CreateCommand { get; }

        public CreateTournamentViewModel()
        {
            CreateCommand = new RelayCommand(CreateTournament);
        }

        private void CreateTournament(object parameter)
        {
            var tournament = new Tournament
            {
                Name = Name,
                Admins = Admins,
                LogoUrl = LogoUrl,
                Status = Status,
                SportType = SportType,
                Type = Type,
                EditMode = EditMode,
                AccessMode = AccessMode,
                NoScore = NoScore,
                Description = Description,
                City = City,
                Contacts = Contacts,
                StartDate = StartDate
            };

            _storage.CreateTournament(tournament);
            // Закрыть окно или показать сообщение
        }


    }
}