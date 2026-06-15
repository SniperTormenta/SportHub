using System;
using System.Collections.Generic;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.Services
{
    public class DynamicStorageService : IStorage
    {
        private readonly SqliteStorageService _sqlite;
        private readonly SqlServerStorageService _sqlServer;

        public DynamicStorageService(SqliteStorageService sqlite, SqlServerStorageService sqlServer)
        {
            _sqlite = sqlite;
            _sqlServer = sqlServer;
        }

        private IStorage Current => DatabaseConfig.UseSqlServer ? (IStorage)_sqlServer : _sqlite;

        public List<Tournament> LoadTournaments() => Current.LoadTournaments();
        public void SaveTournaments(List<Tournament> tournaments) => Current.SaveTournaments(tournaments);
        public void CreateTournament(Tournament tournament) => Current.CreateTournament(tournament);
        public void UpdateTournament(Tournament tournament) => Current.UpdateTournament(tournament);
        public void DeleteTournament(Guid tournamentId) => Current.DeleteTournament(tournamentId);

        public void AddTeam(Guid tournamentId, Team team) => Current.AddTeam(tournamentId, team);
        public void UpdateTeam(Guid tournamentId, Team team) => Current.UpdateTeam(tournamentId, team);
        public void DeleteTeam(Guid tournamentId, Guid teamId) => Current.DeleteTeam(tournamentId, teamId);

        public void AddPlayer(Guid teamId, Player player) => Current.AddPlayer(teamId, player);
        public void UpdatePlayer(Guid teamId, Player player) => Current.UpdatePlayer(teamId, player);
        public void DeletePlayer(Guid teamId, Guid playerId) => Current.DeletePlayer(teamId, playerId);

        public void SaveMatch(Guid tournamentId, Match match) => Current.SaveMatch(tournamentId, match);
        public void DeleteMatch(Guid matchId) => Current.DeleteMatch(matchId);

        public void SaveStandings(Guid tournamentId, IEnumerable<ResultRow> rows) => Current.SaveStandings(tournamentId, rows);
    }
}