// Interfaces/IStorage.cs
using SportHubBase.Models;
using System;
using System.Collections.Generic;

namespace SportHubBase.Interfaces
{
    public interface IStorage
    {
        List<Tournament> LoadTournaments();
        void SaveTournaments(List<Tournament> tournaments);
        void CreateTournament(Tournament tournament);
        void UpdateTournament(Tournament tournament);
        void DeleteTournament(Guid tournamentId);
        void AddTeam(Guid tournamentId, Team team);
        // Можно добавить другие методы по необходимости
    }
}