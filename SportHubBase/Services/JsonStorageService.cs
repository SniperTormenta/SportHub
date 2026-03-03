// Services/JsonStorageService.cs
// Используется как fallback / для обратной совместимости.
// Основным хранилищем является SqliteStorageService.
using Newtonsoft.Json;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SportHubBase.Services
{
    /// <summary>
    /// Реализация IStorage через JSON-файл (fallback / совместимость).
    /// Обновление: добавлены методы для команд, игроков, матчей и стендингов.
    /// </summary>
    public class JsonStorageService : IStorage
    {
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tournaments.json");

        // ─── Турниры ──────────────────────────────────────────────────────────────

        public List<Tournament> LoadTournaments()
        {
            if (!File.Exists(_filePath))
                return new List<Tournament>();

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
            int index = tournaments.FindIndex(t => t.Id == updatedTournament.Id);
            if (index != -1)
            {
                tournaments[index] = updatedTournament;
                SaveTournaments(tournaments);
            }
        }

        public void DeleteTournament(Guid tournamentId)
        {
            var tournaments = LoadTournaments();
            int index = tournaments.FindIndex(t => t.Id == tournamentId);
            if (index != -1)
            {
                tournaments.RemoveAt(index);
                SaveTournaments(tournaments);
            }
        }

        // ─── Команды ──────────────────────────────────────────────────────────────

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

        public void UpdateTeam(Guid tournamentId, Team updatedTeam)
        {
            var tournaments = LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == tournamentId);
            if (tournament != null)
            {
                int idx = tournament.Teams.FindIndex(tm => tm.Id == updatedTeam.Id);
                if (idx != -1)
                {
                    tournament.Teams[idx] = updatedTeam;
                    SaveTournaments(tournaments);
                }
            }
        }

        public void DeleteTeam(Guid tournamentId, Guid teamId)
        {
            var tournaments = LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == tournamentId);
            if (tournament != null)
            {
                int idx = tournament.Teams.FindIndex(tm => tm.Id == teamId);
                if (idx != -1)
                {
                    tournament.Teams.RemoveAt(idx);
                    SaveTournaments(tournaments);
                }
            }
        }

        // ─── Игроки ───────────────────────────────────────────────────────────────
        // В JSON-модели игроки вложены в команды — ищем команду по всем турнирам.

        public void AddPlayer(Guid teamId, Player player)
        {
            var tournaments = LoadTournaments();
            foreach (var t in tournaments)
            {
                var team = t.Teams.Find(tm => tm.Id == teamId);
                if (team != null)
                {
                    if (player.Id == Guid.Empty)
                        player.Id = Guid.NewGuid();
                    team.Players.Add(player);
                    SaveTournaments(tournaments);
                    return;
                }
            }
        }

        public void UpdatePlayer(Guid teamId, Player updated)
        {
            var tournaments = LoadTournaments();
            foreach (var t in tournaments)
            {
                var team = t.Teams.Find(tm => tm.Id == teamId);
                if (team != null)
                {
                    int idx = team.Players.FindIndex(p => p.Id == updated.Id);
                    if (idx != -1)
                    {
                        team.Players[idx] = updated;
                        SaveTournaments(tournaments);
                        return;
                    }
                }
            }
        }

        public void DeletePlayer(Guid teamId, Guid playerId)
        {
            var tournaments = LoadTournaments();
            foreach (var t in tournaments)
            {
                var team = t.Teams.Find(tm => tm.Id == teamId);
                if (team != null)
                {
                    int idx = team.Players.FindIndex(p => p.Id == playerId);
                    if (idx != -1)
                    {
                        team.Players.RemoveAt(idx);
                        SaveTournaments(tournaments);
                        return;
                    }
                }
            }
        }

        // ─── Матчи ────────────────────────────────────────────────────────────────

        public void SaveMatch(Guid tournamentId, Match match)
        {
            var tournaments = LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == tournamentId);
            if (tournament != null)
            {
                int idx = tournament.Matches.FindIndex(m => m.Id == match.Id);
                if (idx != -1)
                    tournament.Matches[idx] = match;
                else
                    tournament.Matches.Add(match);

                SaveTournaments(tournaments);
            }
        }

        public void DeleteMatch(Guid matchId)
        {
            var tournaments = LoadTournaments();
            foreach (var t in tournaments)
            {
                int idx = t.Matches.FindIndex(m => m.Id == matchId);
                if (idx != -1)
                {
                    t.Matches.RemoveAt(idx);
                    SaveTournaments(tournaments);
                    return;
                }
            }
        }

        // ─── Стендинги ────────────────────────────────────────────────────────────
        // В JSON-хранилище стендинги не кэшируются — пересчитываются каждый раз.

        public void SaveStandings(Guid tournamentId, IEnumerable<ResultRow> rows)
        {
            // Стендинги в JSON не кэшируются — пересчитываются из матчей на лету.
        }
    }
}