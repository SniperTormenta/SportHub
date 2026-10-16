// Interfaces/IStorage.cs
using SportHubBase.Models;
using System;
using System.Collections.Generic;

namespace SportHubBase.Interfaces
{
    /// <summary>
    /// Контракт хранилища данных. Реализуется JsonStorageService (JSON) или SqliteStorageService (SQLite).
    /// Все методы синхронные — JsonStorage/SQLite работают локально без I/O-задержек.
    /// </summary>
    public interface IStorage
    {
        // ─── Турниры ──────────────────────────────────────────────────────────────
        List<Tournament> LoadTournaments();
        Tournament GetTournamentById(Guid tournamentId);
        void SaveTournaments(List<Tournament> tournaments);
        void CreateTournament(Tournament tournament);
        void UpdateTournament(Tournament tournament);
        void DeleteTournament(Guid tournamentId);

        // ─── Удалённые турниры и права организатора ───────────────────────────────
        List<Tournament> GetRemoteTournamentsList(string serverAddress);
        Tournament ConnectRemoteTournament(Guid tournamentId, string serverAddress);
        bool TrySyncRemoteTournament(Guid tournamentId, out Tournament syncedTournament);
        bool IsTournamentEditable(Tournament tournament);
        bool UnlockTournament(Tournament tournament, string login, string password, bool rememberMe, out string errorMessage);
        void LockTournament(Tournament tournament);
        void DisconnectLocalCopy(Guid tournamentId);

        // ─── Команды ──────────────────────────────────────────────────────────────
        void AddTeam(Guid tournamentId, Team team);
        void UpdateTeam(Guid tournamentId, Team team);
        void DeleteTeam(Guid tournamentId, Guid teamId);

        // ─── Игроки ───────────────────────────────────────────────────────────────
        void AddPlayer(Guid teamId, Player player);
        void UpdatePlayer(Guid teamId, Player player);
        void DeletePlayer(Guid teamId, Guid playerId);

        // ─── Матчи ────────────────────────────────────────────────────────────────
        void SaveMatch(Guid tournamentId, Match match);
        void DeleteMatch(Guid matchId);

        // ─── Стендинги (кэш таблицы результатов) ─────────────────────────────────
        void SaveStandings(Guid tournamentId, IEnumerable<ResultRow> rows);
    }
}