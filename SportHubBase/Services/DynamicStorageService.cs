using System;
using System.Collections.Generic;
using System.Linq;
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
                            _sqlite.CacheTournament(remote);
                            result.Add(remote);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Write("Загрузка удалённого турнира", ex);
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
                        _sqlite.CacheTournament(remote);
                        return remote;
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Write("Синхронизация турнира", ex);
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
                _sqlite.CacheTournament(tournament);
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
                if (!IsTournamentEditable(tournament))
                    throw new InvalidOperationException("Турнир доступен только для чтения. Проверьте соединение и права организатора.");
                _sqlServer.UpdateTournament(tournament);
                tournament.IsOfflineCache = false;
                try { _sqlite.CacheTournament(tournament); }
                catch (Exception ex)
                {
                    ErrorLog.Write("Обновление локального кэша", ex);
                    throw new InvalidOperationException("Изменения сохранены на сервере, но локальный кэш не обновлён. Выполните синхронизацию турнира.", ex);
                }
            }
            else
            {
                _sqlite.UpdateTournament(tournament);
            }
        }

        public void DeleteTournament(Guid tournamentId)
        {
            var tournament = _sqlite.GetTournamentById(tournamentId);
            if (tournament != null) DeleteTournament(tournament);
        }

        public void DeleteTournament(Tournament tournament)
        {
            var tournamentId = tournament.Id;
            if (!IsTournamentEditable(tournament)) throw new InvalidOperationException("Нет прав на удаление турнира.");
            _sqlite.CreateSafetyBackup();
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local != null && local.IsRemote)
            {
                try
                {
                    _sqlServer.DeleteTournament(tournamentId, local.ServerAddress, tournament.Revision);
                }
                catch (StorageConflictException) { throw; }
                catch (Exception ex)
                {
                    ErrorLog.Write("Удаление удалённого турнира", ex);
                    throw new InvalidOperationException("Удалить турнир на сервере не удалось. Локальная копия сохранена. Для удаления только локальной копии используйте отключение турнира.", ex);
                }
            }
            _sqlite.DeleteTournament(tournamentId, local != null && local.IsRemote ? (long?)null : tournament.Revision);
            _sessionUnlockedIds.Remove(tournamentId);
        }

        public void CreateSafetyBackup() => _sqlite.CreateSafetyBackup();

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

            _sqlite.CacheTournament(remote);
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
                    _sqlite.CacheTournament(remote);
                    syncedTournament = remote;
                    return true;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write("Синхронизация турнира", ex);
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
                errorMessage = "Для старого турнира не назначен организатор. Учётные данные должен назначить администратор сервера.";
                return false;
            }

            _sessionUnlockedIds.Add(tournament.Id);
            tournament.IsOrganizerRemembered = rememberMe;
            _sqlite.SetOrganizerRemembered(tournament.Id, rememberMe);
            _sqlite.CacheTournament(tournament);
            return true;
        }

        public void LockTournament(Tournament tournament)
        {
            if (tournament == null) return;
            _sessionUnlockedIds.Remove(tournament.Id);
            tournament.IsOrganizerRemembered = false;
            _sqlite.SetOrganizerRemembered(tournament.Id, false);
        }

        private void Change(Guid tournamentId, Action<Tournament> change)
        {
            var tournament = GetTournamentById(tournamentId) ?? throw new InvalidOperationException("Турнир не найден.");
            if (!IsTournamentEditable(tournament)) throw new InvalidOperationException("Турнир доступен только для чтения. Проверьте соединение и права организатора.");
            change(tournament);
            UpdateTournament(tournament);
        }

        private Guid FindTeamTournament(Guid teamId)
        {
            var tournament = _sqlite.LoadTournaments().FirstOrDefault(t => t.Teams.Any(team => team.Id == teamId));
            return tournament?.Id ?? throw new InvalidOperationException("Команда не найдена.");
        }

        public void AddTeam(Guid tournamentId, Team team) => Change(tournamentId, t => t.Teams.Add(team));
        public void UpdateTeam(Guid tournamentId, Team team) => Change(tournamentId, t =>
        {
            var index = t.Teams.FindIndex(x => x.Id == team.Id);
            if (index < 0) throw new InvalidOperationException("Команда не найдена.");
            t.Teams[index] = team;
        });
        public void DeleteTeam(Guid tournamentId, Guid teamId) => Change(tournamentId, t =>
        {
            t.Teams.RemoveAll(x => x.Id == teamId);
            t.Matches.RemoveAll(x => x.Team1Id == teamId || x.Team2Id == teamId);
        });
        public void AddPlayer(Guid teamId, Player player) => Change(FindTeamTournament(teamId), t => t.Teams.Single(x => x.Id == teamId).Players.Add(player));
        public void UpdatePlayer(Guid teamId, Player player) => Change(FindTeamTournament(teamId), t =>
        {
            var players = t.Teams.Single(x => x.Id == teamId).Players;
            var index = players.FindIndex(x => x.Id == player.Id);
            if (index < 0) throw new InvalidOperationException("Игрок не найден.");
            players[index] = player;
        });
        public void DeletePlayer(Guid teamId, Guid playerId) => Change(FindTeamTournament(teamId), t => t.Teams.Single(x => x.Id == teamId).Players.RemoveAll(x => x.Id == playerId));
        public void SaveMatch(Guid tournamentId, Match match) => Change(tournamentId, t =>
        {
            var index = t.Matches.FindIndex(x => x.Id == match.Id);
            if (index < 0) t.Matches.Add(match); else t.Matches[index] = match;
        });
        public void DeleteMatch(Guid matchId)
        {
            var tournament = _sqlite.LoadTournaments().FirstOrDefault(t => t.Matches.Any(m => m.Id == matchId));
            if (tournament != null) Change(tournament.Id, t => t.Matches.RemoveAll(m => m.Id == matchId));
        }
        public void SaveStandings(Guid tournamentId, IEnumerable<ResultRow> rows)
        {
            var list = rows.ToList();
            var local = _sqlite.GetTournamentById(tournamentId);
            if (local != null && local.IsRemote) _sqlServer.SaveStandings(tournamentId, list, local.ServerAddress);
            _sqlite.SaveStandings(tournamentId, list);
        }
    }
}
