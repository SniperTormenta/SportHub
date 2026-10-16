using System;
using System.Collections.Generic;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.Services
{
    /// <summary>
    /// Координирует работу между локальной базой SQLite (локальные турниры + оффлайн-кэш)
    /// и удалёнными серверами MS SQL Server (для подключенных/опубликованных турниров).
    /// </summary>
    public class DynamicStorageService : IStorage
    {
        private readonly SqliteStorageService _sqlite;
        private readonly SqlServerStorageService _sqlServer;
        private readonly HashSet<Guid> _sessionUnlockedIds = new HashSet<Guid>();

        public DynamicStorageService(SqliteStorageService sqlite, SqlServerStorageService sqlServer)
        {
            _sqlite = sqlite ?? throw new ArgumentNullException(nameof(sqlite));
            _sqlServer = sqlServer ?? throw new ArgumentNullException(nameof(sqlServer));
        }

        public List<Tournament> LoadTournaments()
        {
            var localList = _sqlite.LoadTournaments();
            var result = new List<Tournament>(localList.Count);

            foreach (var t in localList)
            {
                if (t.IsRemote)
                {
                    try
                    {
                        var remote = _sqlServer.GetTournamentById(t.Id, t.ServerAddress);
                        if (remote != null)
                        {
                            remote.IsRemote = true;
                            remote.ServerAddress = t.ServerAddress;
                            remote.IsOrganizerRemembered = t.IsOrganizerRemembered;
                            remote.IsOfflineCache = false;
                            _sqlite.UpdateTournament(remote);
                            result.Add(remote);
                            continue;
                        }
                    }
                    catch
                    {
                        // Сервер недоступен — используем локальный оффлайн-кэш
                    }

                    t.IsOfflineCache = true;
                    result.Add(t);
                }
                else
                {
                    t.IsOfflineCache = false;
                    result.Add(t);
                }
            }

            return result;
        }

        public Tournament GetTournamentById(Guid tournamentId)
        {
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local == null) return null;

            if (local.IsRemote)
            {
                try
                {
                    var remote = _sqlServer.GetTournamentById(local.Id, local.ServerAddress);
                    if (remote != null)
                    {
                        remote.IsRemote = true;
                        remote.ServerAddress = local.ServerAddress;
                        remote.IsOrganizerRemembered = local.IsOrganizerRemembered;
                        remote.IsOfflineCache = false;
                        _sqlite.UpdateTournament(remote);
                        return remote;
                    }
                }
                catch
                {
                    // Оффлайн-режим: возвращаем кэшированную копию из SQLite
                }

                local.IsOfflineCache = true;
                return local;
            }

            local.IsOfflineCache = false;
            return local;
        }

        public void SaveTournaments(List<Tournament> tournaments)
        {
            if (tournaments == null) return;
            foreach (var t in tournaments)
            {
                UpdateTournament(t);
            }
        }

        public void CreateTournament(Tournament tournament)
        {
            if (tournament == null) throw new ArgumentNullException(nameof(tournament));
            if (tournament.Id == Guid.Empty) tournament.Id = Guid.NewGuid();

            if (tournament.IsRemote)
            {
                _sqlServer.CreateTournament(tournament);
                tournament.IsOrganizerRemembered = true;
                tournament.IsOfflineCache = false;
                _sessionUnlockedIds.Add(tournament.Id);
                _sqlite.CreateTournament(tournament);
            }
            else
            {
                tournament.IsOfflineCache = false;
                _sqlite.CreateTournament(tournament);
            }
        }

        public void UpdateTournament(Tournament tournament)
        {
            if (tournament == null) throw new ArgumentNullException(nameof(tournament));

            if (tournament.IsRemote)
            {
                _sqlServer.UpdateTournament(tournament);
                tournament.IsOfflineCache = false;
                _sqlite.UpdateTournament(tournament);
            }
            else
            {
                _sqlite.UpdateTournament(tournament);
            }
        }

        public void DeleteTournament(Guid tournamentId)
        {
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local != null && local.IsRemote)
            {
                try
                {
                    _sqlServer.DeleteTournament(tournamentId, local.ServerAddress);
                }
                catch
                {
                    // Если сервер недоступен, всё равно удаляем локально
                }
            }
            _sqlite.DeleteTournament(tournamentId);
            _sessionUnlockedIds.Remove(tournamentId);
        }

        public void DisconnectLocalCopy(Guid tournamentId)
        {
            _sqlite.DeleteTournament(tournamentId);
            _sessionUnlockedIds.Remove(tournamentId);
        }

        public List<Tournament> GetRemoteTournamentsList(string serverAddress)
        {
            return _sqlServer.GetRemoteTournamentsList(serverAddress);
        }

        public Tournament ConnectRemoteTournament(Guid tournamentId, string serverAddress)
        {
            var remote = _sqlServer.GetTournamentById(tournamentId, serverAddress);
            if (remote == null)
                throw new InvalidOperationException("Турнир не найден на указанном сервере.");

            var existingLocal = _sqlite.GetTournamentById(tournamentId);
            remote.IsRemote = true;
            remote.ServerAddress = serverAddress;
            remote.IsOfflineCache = false;
            remote.IsOrganizerRemembered = existingLocal != null && existingLocal.IsOrganizerRemembered;

            _sqlite.UpdateTournament(remote);
            return remote;
        }

        public bool TrySyncRemoteTournament(Guid tournamentId, out Tournament syncedTournament)
        {
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local == null)
            {
                syncedTournament = null;
                return false;
            }

            if (!local.IsRemote)
            {
                local.IsOfflineCache = false;
                syncedTournament = local;
                return true;
            }

            try
            {
                var remote = _sqlServer.GetTournamentById(tournamentId, local.ServerAddress);
                if (remote != null)
                {
                    remote.IsRemote = true;
                    remote.ServerAddress = local.ServerAddress;
                    remote.IsOrganizerRemembered = local.IsOrganizerRemembered;
                    remote.IsOfflineCache = false;
                    _sqlite.UpdateTournament(remote);
                    syncedTournament = remote;
                    return true;
                }
            }
            catch
            {
                // Не удалось достучаться до сервера
            }

            local.IsOfflineCache = true;
            syncedTournament = local;
            return false;
        }

        public bool IsTournamentEditable(Tournament tournament)
        {
            if (tournament == null) return false;
            if (!tournament.IsRemote) return true;
            if (tournament.IsOfflineCache) return false;

            return tournament.IsOrganizerRemembered || _sessionUnlockedIds.Contains(tournament.Id);
        }

        public bool UnlockTournament(Tournament tournament, string login, string password, bool rememberMe, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (tournament == null)
            {
                errorMessage = "Турнир не найден.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Введите логин и пароль организатора.";
                return false;
            }

            // Если у удалённого турнира заданы учётные данные организатора — проверяем их
            if (!string.IsNullOrEmpty(tournament.OrganizerPasswordHash))
            {
                bool loginMatches = string.Equals(tournament.OrganizerLogin?.Trim(), login.Trim(), StringComparison.OrdinalIgnoreCase);
                bool passwordMatches = PasswordHasher.VerifyPassword(password, tournament.OrganizerPasswordHash);

                if (!loginMatches || !passwordMatches)
                {
                    errorMessage = "Неверный логин или пароль организатора турнира.";
                    return false;
                }
            }
            else
            {
                // Если турнир был создан в старой схеме БД без пароля организатора — устанавливаем введённые данные
                tournament.OrganizerLogin = login.Trim();
                tournament.OrganizerPasswordHash = PasswordHasher.HashPassword(password);
                try
                {
                    _sqlServer.UpdateTournament(tournament);
                }
                catch (Exception ex)
                {
                    errorMessage = "Нет связи с сервером для сохранения прав организатора: " + ex.Message;
                    return false;
                }
            }

            _sessionUnlockedIds.Add(tournament.Id);
            tournament.IsOrganizerRemembered = rememberMe;
            _sqlite.SetOrganizerRemembered(tournament.Id, rememberMe);
            _sqlite.UpdateTournament(tournament);
            return true;
        }

        public void LockTournament(Tournament tournament)
        {
            if (tournament == null) return;
            _sessionUnlockedIds.Remove(tournament.Id);
            tournament.IsOrganizerRemembered = false;
            _sqlite.SetOrganizerRemembered(tournament.Id, false);
        }

        public void AddTeam(Guid tournamentId, Team team)
        {
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local != null && local.IsRemote)
            {
                _sqlServer.AddTeam(tournamentId, team, local.ServerAddress);
            }
            _sqlite.AddTeam(tournamentId, team);
        }

        public void UpdateTeam(Guid tournamentId, Team team)
        {
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local != null && local.IsRemote)
            {
                _sqlServer.UpdateTeam(tournamentId, team, local.ServerAddress);
            }
            _sqlite.UpdateTeam(tournamentId, team);
        }

        public void DeleteTeam(Guid tournamentId, Guid teamId)
        {
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local != null && local.IsRemote)
            {
                _sqlServer.DeleteTeam(tournamentId, teamId, local.ServerAddress);
            }
            _sqlite.DeleteTeam(tournamentId, teamId);
        }

        public void AddPlayer(Guid teamId, Player player)
        {
            _sqlite.AddPlayer(teamId, player);
        }

        public void UpdatePlayer(Guid teamId, Player player)
        {
            _sqlite.UpdatePlayer(teamId, player);
        }

        public void DeletePlayer(Guid teamId, Guid playerId)
        {
            _sqlite.DeletePlayer(teamId, playerId);
        }

        public void SaveMatch(Guid tournamentId, Match match)
        {
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local != null && local.IsRemote)
            {
                _sqlServer.SaveMatch(tournamentId, match, local.ServerAddress);
            }
            _sqlite.SaveMatch(tournamentId, match);
        }

        public void DeleteMatch(Guid matchId)
        {
            _sqlite.DeleteMatch(matchId);
        }

        public void SaveStandings(Guid tournamentId, IEnumerable<ResultRow> rows)
        {
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local != null && local.IsRemote)
            {
                try
                {
                    _sqlServer.SaveStandings(tournamentId, rows, local.ServerAddress);
                }
                catch
                {
                    // Игнорируем ошибку записи стендингов при оффлайне
                }
            }
            _sqlite.SaveStandings(tournamentId, rows);
        }
    }
}