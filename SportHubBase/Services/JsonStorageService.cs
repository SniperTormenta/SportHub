// Services/JsonStorageService.cs
// Это базовый CRUD. Расширить по мере нужды (для матчей, результатов и т.д.).
using Newtonsoft.Json;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace SportHubBase.Services
{
    /// Сервис для хранения и CRUD-операций с турнирами в JSON-файле.
    /// Изолированная логика persistence; инжектируется через IoC в ViewModels/Services.
    /// В MVVM: Вызывается из VM для загрузки/сохранения, без UI-зависимостей.
    /// Улучшение: Добавить обработку исключений (e.g. FileNotFound), возможно Directory.Create для папки; расширить методами для Matches/ResultRow.
    public class JsonStorageService : IStorage
    {
        /// Путь к файлу хранения (в базовой директории приложения).
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tournaments.json");

        /// Загружает список турниров из JSON. Если файл не существует — возвращает пустой список.
        public List<Tournament> LoadTournaments()
        {
            if (!File.Exists(_filePath))
            {
                return new List<Tournament>();
            }

            string json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<List<Tournament>>(json) ?? new List<Tournament>();
        }

        /// Сохраняет список турниров в JSON с отступами для читаемости.
        public void SaveTournaments(List<Tournament> tournaments)
        {
            string json = JsonConvert.SerializeObject(tournaments, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        /// Создаёт новый турнир: добавляет в список и сохраняет.
        public void CreateTournament(Tournament newTournament)
        {
            var tournaments = LoadTournaments();
            tournaments.Add(newTournament);
            SaveTournaments(tournaments);
        }

        /// Обновляет существующий турнир по ID.
        public void UpdateTournament(Tournament updatedTournament)
        {
            var tournaments = LoadTournaments();
            var index = tournaments.FindIndex(t => t.Id == updatedTournament.Id);
            if (index != -1)
            {
                tournaments[index] = updatedTournament;
                SaveTournaments(tournaments);
            }
            // Улучшение: Если не найден — throw ArgumentException или лог.
        }

        /// Добавляет команду в турнир по ID и сохраняет.
        public void AddTeam(Guid tournamentId, Team newTeam)
        {
            var tournaments = LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == tournamentId);
            if (tournament != null)
            {
                tournament.Teams.Add(newTeam);
                SaveTournaments(tournaments);
            }
        }

        /// Удаляет турнир по ID.
        public void DeleteTournament(Guid tournamentId)
        {
            var tournaments = LoadTournaments();
            var index = tournaments.FindIndex(t => t.Id == tournamentId);
            if (index != -1)
            {
                tournaments.RemoveAt(index);
                SaveTournaments(tournaments);
            }
        }
    }
}