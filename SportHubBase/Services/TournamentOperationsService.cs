using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SportHubBase.Services
{
    /// <summary>Операции турнира без зависимостей от окон и диалогов WPF.</summary>
    public sealed class TournamentOperationsService
    {
        private readonly IStorage _storage;
        public TournamentOperationsService(IStorage storage) { _storage = storage; }

        public void Save(Tournament tournament)
        {
            try { TournamentIntegrityService.Normalize(tournament); _storage.UpdateTournament(tournament); }
            catch (Exception ex) { ErrorLog.Write("Сохранение турнира", ex); throw; }
        }

        public void Finish(Tournament tournament, DateTime now)
        {
            var oldStatus = tournament.Status;
            var oldEnd = tournament.EndDate;
            tournament.Status = "Завершён";
            if (!tournament.EndDate.HasValue || tournament.EndDate.Value > now) tournament.EndDate = now;
            try { Save(tournament); }
            catch { tournament.Status = oldStatus; tournament.EndDate = oldEnd; throw; }
        }

        public void Delete(Tournament tournament)
        {
            if (_storage is DynamicStorageService dynamicStorage) dynamicStorage.DeleteTournament(tournament);
            else if (_storage is SqliteStorageService sqlite) sqlite.DeleteTournament(tournament.Id, tournament.Revision);
            else _storage.DeleteTournament(tournament.Id);
        }

        public void RemoveTeam(Tournament tournament, Guid teamId)
        {
            if (!_storage.IsTournamentEditable(tournament))
                throw new InvalidOperationException("Турнир доступен только для чтения.");
            var team = tournament.Teams.SingleOrDefault(t => t.Id == teamId)
                ?? throw new InvalidOperationException("Команда не найдена.");
            (_storage as DynamicStorageService)?.CreateSafetyBackup();
            (_storage as SqliteStorageService)?.CreateSafetyBackup();
            var oldTeams = tournament.Teams;
            var oldMatches = tournament.Matches;
            var oldBracket = tournament.Bracket;
            var oldPreserve = tournament.PreserveSchedule;
            // В старых матчах без ID используем только полное уникальное название.
            bool IsTeam(Guid? id, string name) => id.HasValue ? id == teamId
                : string.Equals(name?.Trim(), team.Name?.Trim(), StringComparison.OrdinalIgnoreCase);
            try
            {
                tournament.Matches = (oldMatches ?? new List<Match>())
                    .Where(m => !IsTeam(m.Team1Id, m.Team1) && !IsTeam(m.Team2Id, m.Team2)).ToList();
                tournament.Teams = oldTeams.Where(t => t.Id != teamId).ToList();
                tournament.PreserveSchedule = true;
                if (oldBracket != null)
                {
                    var bracket = JsonConvert.DeserializeObject<TournamentBracket>(JsonConvert.SerializeObject(oldBracket));
                    bracket.PreserveStructure = true;
                    int number = 1;
                    foreach (var round in bracket.Rounds)
                    {
                        foreach (var match in round.Matches.ToList())
                        {
                            if (!match.IsBye) match.MatchNumber = match.MatchNumber ?? number++;
                            if (match.Team1?.Id == teamId || match.Team2?.Id == teamId) round.Matches.Remove(match);
                        }
                    }
                    if (bracket.BronzeMatch != null)
                    {
                        bracket.BronzeMatch.MatchNumber = bracket.BronzeMatch.MatchNumber ?? number;
                        if (bracket.BronzeMatch.Team1?.Id == teamId || bracket.BronzeMatch.Team2?.Id == teamId)
                            bracket.BronzeMatch = null;
                    }
                    bracket.ReconnectReferences();
                    tournament.Bracket = bracket;
                }
                Save(tournament);
            }
            catch
            {
                tournament.Teams = oldTeams;
                tournament.Matches = oldMatches;
                tournament.Bracket = oldBracket;
                tournament.PreserveSchedule = oldPreserve;
                throw;
            }
        }

        public void ReplaceTeams(Tournament tournament, List<Team> teams)
        {
            if (teams == null || teams.Count == 0) throw new ArgumentException("Список импортируемых команд пуст.");
            (_storage as DynamicStorageService)?.CreateSafetyBackup();
            (_storage as SqliteStorageService)?.CreateSafetyBackup();
            var previousTeams = tournament.Teams;
            var previousMatches = tournament.Matches;
            var previousBracket = tournament.Bracket;
            var previousPreserve = tournament.PreserveSchedule;
            tournament.Teams = teams;
            tournament.Matches = new List<Match>();
            tournament.Bracket = null;
            tournament.PreserveSchedule = false;
            try { Save(tournament); }
            catch { tournament.Teams = previousTeams; tournament.Matches = previousMatches; tournament.Bracket = previousBracket; tournament.PreserveSchedule = previousPreserve; throw; }
        }
    }
}
