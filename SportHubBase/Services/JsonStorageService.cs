// Services/JsonStorageService.cs
// Это базовый CRUD. Расширить по мере нужды (для матчей, результатов и т.д.).
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SportHubBase.Models;

namespace SportHubBase.Services
{
    public class JsonStorageService
    {
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tournaments.json");

        public List<Tournament> LoadTournaments()
        {
            if (!File.Exists(_filePath))
            {
                return new List<Tournament>();
            }

            string json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<List<Tournament>>(json) ?? new List<Tournament>();
        }

        public void SaveTournaments(List<Tournament> tournaments)
        {
            string json = JsonConvert.SerializeObject(tournaments, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public void CreateTournament(Tournament newTournament)
        {
            var tournaments = LoadTournaments();
            tournaments.Add(newTournament);
            SaveTournaments(tournaments);
        }

        public void UpdateTournament(Tournament updatedTournament)
        {
            var tournaments = LoadTournaments();
            var index = tournaments.FindIndex(t => t.Id == updatedTournament.Id);
            if (index != -1)
            {
                tournaments[index] = updatedTournament;
                SaveTournaments(tournaments);
            }
        }

        // Пример для добавления команды
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
    }
}