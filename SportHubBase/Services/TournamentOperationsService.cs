using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.Generic;

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

        public void ReplaceTeams(Tournament tournament, List<Team> teams)
        {
            if (teams == null || teams.Count == 0) throw new ArgumentException("Список импортируемых команд пуст.");
            (_storage as DynamicStorageService)?.CreateSafetyBackup();
            (_storage as SqliteStorageService)?.CreateSafetyBackup();
            var previousTeams = tournament.Teams;
            var previousMatches = tournament.Matches;
            var previousBracket = tournament.Bracket;
            tournament.Teams = teams;
            tournament.Matches = new List<Match>();
            tournament.Bracket = null;
            try { Save(tournament); }
            catch { tournament.Teams = previousTeams; tournament.Matches = previousMatches; tournament.Bracket = previousBracket; throw; }
        }
    }
}
