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
    /// <summary>
    /// Сервис для хранения и CRUD-операций с турнирами в JSON-файле.
    /// Изолированная логика persistence; инжектируется через IoC в ViewModels/Services.
    /// В MVVM: Вызывается из VM для загрузки/сохранения, без UI-зависимостей.
    /// Улучшение: Добавить обработку исключений (e.g. FileNotFound), возможно Directory.Create для папки; расширить методами для Matches/ResultRow.
    /// </summary>
    public class JsonStorageService : IStorage
    {
        /// <summary>
        /// Путь к файлу хранения (в базовой директории приложения).
        /// </summary>
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tournaments.json");

        /// <summary>
        /// Загружает список турниров из JSON. Если файл не существует — возвращает пустой список.
        /// </summary>
        public List<Tournament> LoadTournaments()
        {
            if (!File.Exists(_filePath))
            {
                return new List<Tournament>();
            }

            string json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<List<Tournament>>(json) ?? new List<Tournament>();
        }

        /// <summary>
        /// Сохраняет список турниров в JSON с отступами для читаемости.
        /// </summary>
        public void SaveTournaments(List<Tournament> tournaments)
        {
            string json = JsonConvert.SerializeObject(tournaments, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        /// <summary>
        /// Создаёт новый турнир: добавляет в список и сохраняет.
        /// </summary>
        public void CreateTournament(Tournament newTournament)
        {
            var tournaments = LoadTournaments();
            tournaments.Add(newTournament);
            SaveTournaments(tournaments);
        }

        /// <summary>
        /// Обновляет существующий турнир по ID.
        /// </summary>
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

        /// <summary>
        /// Добавляет команду в турнир по ID и сохраняет.
        /// </summary>
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

        /// <summary>
        /// Удаляет турнир по ID.
        /// </summary>
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