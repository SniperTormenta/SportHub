// Services/SqliteStorageService.cs
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportHubBase.Services
{
    /// <summary>
    /// Реализация IStorage поверх SQLite.
    /// Турниры, команды, игроки и матчи хранятся в реляционных таблицах.
    /// Bracket (сетка Олимпика) сериализуется в JSON-колонку, т.к. структура рекурсивна.
    /// </summary>
    public class SqliteStorageService : IStorage
    {
        private readonly SqliteDatabaseService _db;

        public SqliteStorageService(SqliteDatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ТУРНИРЫ
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Загружает все турниры вместе с командами, игроками и матчами.
        /// </summary>
        public List<Tournament> LoadTournaments()
        {
            var tournaments = new List<Tournament>();

            using (var conn = OpenConnection())
            {
                // 1. Загружаем словарь accountId -> username за один запрос
                var accountNames = LoadAccountNames(conn);

                // 2. Загружаем турниры
                using (var cmd = conn.CreateCommand())
                {
                    string currentUserId = CurrentSession.CurrentUser?.Id.ToString();
                    
                    cmd.CommandText = @"
                        SELECT * FROM Tournaments 
                        WHERE IsPublic = 1 
                           OR OwnerId = @CurrentUserId
                           OR (AllowedUserIds IS NOT NULL AND 
                               (AllowedUserIds = @CurrentUserId 
                                OR AllowedUserIds LIKE @CurrentUserId || ',%' 
                                OR AllowedUserIds LIKE '%,' || @CurrentUserId || ',%' 
                                OR AllowedUserIds LIKE '%,' || @CurrentUserId))
                        ORDER BY StartDate DESC";
                    
                    cmd.Parameters.AddWithValue("@CurrentUserId", currentUserId ?? string.Empty);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var t = ReadTournamentRow(reader);
                            // Заполняем OwnerName из кэша аккаунтов
                            if (!string.IsNullOrEmpty(t.OwnerId))
                            {
                                string ownerName;
                                if (accountNames.TryGetValue(t.OwnerId, out ownerName))
                                    t.OwnerName = ownerName;
                            }
                            tournaments.Add(t);
                        }
                    }
                }

                // 3. Загружаем команды, игроков и матчи для каждого турнира
                foreach (var t in tournaments)
                {
                    t.Teams = LoadTeamsForTournament(conn, t.Id);
                    t.Matches = LoadMatchesForTournament(conn, t.Id);
                }
            }

            return tournaments;
        }

        /// <summary>
        /// Загружает словарь Id(строка) -> Username из таблицы Accounts.
        /// Используется для денормализации OwnerName в Tournament.
        /// </summary>
        private Dictionary<string, string> LoadAccountNames(SqliteConnection conn)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Username FROM Accounts";
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string id   = r["Id"].ToString();
                        string name = r["Username"].ToString();
                        if (!map.ContainsKey(id))
                            map[id] = name;
                    }
                }
            }
            return map;
        }

        /// <summary>
        /// Полностью пересохраняет список (используется при импорте / откате).
        /// </summary>
        public void SaveTournaments(List<Tournament> tournaments)
        {
            if (tournaments == null) return;
            foreach (var t in tournaments)
            {
                UpsertTournamentCore(t);
            }
        }

        public void CreateTournament(Tournament tournament)
        {
            if (tournament == null) throw new ArgumentNullException(nameof(tournament));
            if (tournament.Id == Guid.Empty)
                tournament.Id = Guid.NewGuid();

            UpsertTournamentCore(tournament);
        }

        public void UpdateTournament(Tournament tournament)
        {
            if (tournament == null) throw new ArgumentNullException(nameof(tournament));
            UpsertTournamentCore(tournament);
        }

        public void DeleteTournament(Guid tournamentId)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // Cascade удалит команды, игроков, матчи, стендинги
                cmd.CommandText = "DELETE FROM Tournaments WHERE Id = @Id";
                cmd.Parameters.AddWithValue("@Id", tournamentId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // КОМАНДЫ
        // ─────────────────────────────────────────────────────────────────────────

        public void AddTeam(Guid tournamentId, Team team)
        {
            if (team == null) throw new ArgumentNullException(nameof(team));
            if (team.Id == Guid.Empty) team.Id = Guid.NewGuid();

            using (var conn = OpenConnection())
            {
                UpsertTeam(conn, tournamentId, team);
            }
        }

        public void UpdateTeam(Guid tournamentId, Team team)
        {
            if (team == null) throw new ArgumentNullException(nameof(team));

            using (var conn = OpenConnection())
            {
                UpsertTeam(conn, tournamentId, team);
            }
        }

        public void DeleteTeam(Guid tournamentId, Guid teamId)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // Cascade удалит игроков и стендинги команды
                cmd.CommandText = "DELETE FROM Teams WHERE Id = @Id AND TournamentId = @TournamentId";
                cmd.Parameters.AddWithValue("@Id", teamId.ToString());
                cmd.Parameters.AddWithValue("@TournamentId", tournamentId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ИГРОКИ
        // ─────────────────────────────────────────────────────────────────────────

        public void AddPlayer(Guid teamId, Player player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Id == Guid.Empty) player.Id = Guid.NewGuid();

            using (var conn = OpenConnection())
            {
                UpsertPlayer(conn, teamId, player);
            }
        }

        public void UpdatePlayer(Guid teamId, Player player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            using (var conn = OpenConnection())
            {
                UpsertPlayer(conn, teamId, player);
            }
        }

        public void DeletePlayer(Guid teamId, Guid playerId)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Players WHERE Id = @Id AND TeamId = @TeamId";
                cmd.Parameters.AddWithValue("@Id", playerId.ToString());
                cmd.Parameters.AddWithValue("@TeamId", teamId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // МАТЧИ
        // ─────────────────────────────────────────────────────────────────────────

        public void SaveMatch(Guid tournamentId, Match match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (match.Id == Guid.Empty) match.Id = Guid.NewGuid();

            using (var conn = OpenConnection())
            {
                UpsertMatch(conn, tournamentId, match);
            }
        }

        public void DeleteMatch(Guid matchId)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // Cascade удалит MatchSets
                cmd.CommandText = "DELETE FROM Matches WHERE Id = @Id";
                cmd.Parameters.AddWithValue("@Id", matchId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // СТЕНДИНГИ
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Сохраняет (UPSERT) таблицу результатов для турнира.
        /// Строки привязываются по имени команды (TeamId ищем в Teams).
        /// </summary>
        public void SaveStandings(Guid tournamentId, IEnumerable<ResultRow> rows)
        {
            if (rows == null) return;

            using (var conn = OpenConnection())
            {
                // Загружаем словарь TeamName -> TeamId для данного турнира
                var teamIds = LoadTeamNameToIdMap(conn, tournamentId);

                foreach (var row in rows)
                {
                    if (!teamIds.TryGetValue(row.TeamName ?? string.Empty, out string teamId))
                        continue;

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO TournamentStandings
                                (TournamentId, TeamId, Place, Wins, Losses, Draws,
                                 SetsWon, SetsLost, PointsScored, PointsConceded, Points)
                            VALUES
                                (@TournamentId, @TeamId, @Place, @Wins, @Losses, @Draws,
                                 @SetsWon, @SetsLost, @PointsScored, @PointsConceded, @Points)
                            ON CONFLICT(TournamentId, TeamId) DO UPDATE SET
                                Place          = excluded.Place,
                                Wins           = excluded.Wins,
                                Losses         = excluded.Losses,
                                Draws          = excluded.Draws,
                                SetsWon        = excluded.SetsWon,
                                SetsLost       = excluded.SetsLost,
                                PointsScored   = excluded.PointsScored,
                                PointsConceded = excluded.PointsConceded,
                                Points         = excluded.Points";

                        cmd.Parameters.AddWithValue("@TournamentId", tournamentId.ToString());
                        cmd.Parameters.AddWithValue("@TeamId", teamId);
                        cmd.Parameters.AddWithValue("@Place", row.Place);
                        cmd.Parameters.AddWithValue("@Wins", row.Wins);
                        cmd.Parameters.AddWithValue("@Losses", row.Losses);
                        cmd.Parameters.AddWithValue("@Draws", 0);
                        cmd.Parameters.AddWithValue("@SetsWon", row.SetsWon);
                        cmd.Parameters.AddWithValue("@SetsLost", row.SetsLost);
                        cmd.Parameters.AddWithValue("@PointsScored", row.PointsScored);
                        cmd.Parameters.AddWithValue("@PointsConceded", row.PointsConceded);
                        cmd.Parameters.AddWithValue("@Points", row.Points);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ПРИВАТНЫЕ МЕТОДЫ — UPSERT / ЧТЕНИЕ
        // ─────────────────────────────────────────────────────────────────────────

        private void UpsertTournamentCore(Tournament t)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // UPSERT самого турнира
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"
                            INSERT INTO Tournaments
                                (Id, Name, SportType, Type, Status, EditMode, AccessMode,
                                 IsPublic, NoScore, Description, City, Contacts, LogoUrl, InviteCode,
                                 StartDate, EndDate, OwnerId, AllowedUserIds,
                                 ScoringSystem, CustomWinPoints, CustomDrawPoints, CustomLossPoints, BracketJson)
                            VALUES
                                (@Id, @Name, @SportType, @Type, @Status, @EditMode, @AccessMode,
                                 @IsPublic, @NoScore, @Description, @City, @Contacts, @LogoUrl, @InviteCode,
                                 @StartDate, @EndDate, @OwnerId, @AllowedUserIds,
                                 @ScoringSystem, @CustomWinPoints, @CustomDrawPoints, @CustomLossPoints, @BracketJson)
                            ON CONFLICT(Id) DO UPDATE SET
                                Name             = excluded.Name,
                                SportType        = excluded.SportType,
                                Type             = excluded.Type,
                                Status           = excluded.Status,
                                EditMode         = excluded.EditMode,
                                AccessMode       = excluded.AccessMode,
                                IsPublic         = excluded.IsPublic,
                                NoScore          = excluded.NoScore,
                                Description      = excluded.Description,
                                City             = excluded.City,
                                Contacts         = excluded.Contacts,
                                LogoUrl          = excluded.LogoUrl,
                                InviteCode       = excluded.InviteCode,
                                StartDate        = excluded.StartDate,
                                EndDate          = excluded.EndDate,
                                OwnerId          = excluded.OwnerId,
                                AllowedUserIds   = excluded.AllowedUserIds,
                                ScoringSystem    = excluded.ScoringSystem,
                                CustomWinPoints  = excluded.CustomWinPoints,
                                CustomDrawPoints = excluded.CustomDrawPoints,
                                CustomLossPoints = excluded.CustomLossPoints,
                                BracketJson      = excluded.BracketJson";

                        string bracketJson = t.Bracket != null
                            ? JsonConvert.SerializeObject(t.Bracket, new JsonSerializerSettings
                              {
                                  ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                              })
                            : null;

                        string allowedIds = t.AllowedUserIds != null && t.AllowedUserIds.Count > 0
                            ? string.Join(",", t.AllowedUserIds.Select(id => id.ToString()))
                            : null;

                        cmd.Parameters.AddWithValue("@Id", t.Id.ToString());
                        cmd.Parameters.AddWithValue("@Name", t.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@SportType", DbValue(t.SportType));
                        cmd.Parameters.AddWithValue("@Type", DbValue(t.Type));
                        cmd.Parameters.AddWithValue("@Status", t.Status ?? "Не начат");
                        cmd.Parameters.AddWithValue("@EditMode", DbValue(t.EditMode));
                        cmd.Parameters.AddWithValue("@AccessMode", DbValue(t.AccessMode));
                        cmd.Parameters.AddWithValue("@IsPublic", t.IsPublic ? 1 : 0);
                        cmd.Parameters.AddWithValue("@NoScore", t.NoScore ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Description", DbValue(t.Description));
                        cmd.Parameters.AddWithValue("@City", DbValue(t.City));
                        cmd.Parameters.AddWithValue("@Contacts", DbValue(t.Contacts));
                        cmd.Parameters.AddWithValue("@LogoUrl", DbValue(t.LogoUrl));
                        cmd.Parameters.AddWithValue("@InviteCode", DbValue(t.InviteCode));
                        cmd.Parameters.AddWithValue("@StartDate", t.StartDate.ToString("o"));
                        cmd.Parameters.AddWithValue("@EndDate", t.EndDate.HasValue ? (object)t.EndDate.Value.ToString("o") : DBNull.Value);
                        cmd.Parameters.AddWithValue("@OwnerId", DbValue(t.OwnerId));
                        cmd.Parameters.AddWithValue("@AllowedUserIds", DbValue(allowedIds));
                        cmd.Parameters.AddWithValue("@ScoringSystem", DbValue(t.ScoringSystem));
                        cmd.Parameters.AddWithValue("@CustomWinPoints", t.CustomWinPoints);
                        cmd.Parameters.AddWithValue("@CustomDrawPoints", t.CustomDrawPoints);
                        cmd.Parameters.AddWithValue("@CustomLossPoints", t.CustomLossPoints);
                        cmd.Parameters.AddWithValue("@BracketJson", DbValue(bracketJson));
                        cmd.ExecuteNonQuery();
                    }

                    // Сохраняем команды и игроков
                    foreach (var team in t.Teams ?? new List<Team>())
                    {
                        UpsertTeam(conn, t.Id, team, tx);
                    }

                    // Сохраняем матчи
                    foreach (var match in t.Matches ?? new List<Match>())
                    {
                        UpsertMatch(conn, t.Id, match, tx);
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private void UpsertTeam(SqliteConnection conn, Guid tournamentId, Team team,
                                 SqliteTransaction tx = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO Teams (Id, TournamentId, Name, Captain, City, LogoUrl, OwnerUserId, MemberUserIds)
                    VALUES (@Id, @TournamentId, @Name, @Captain, @City, @LogoUrl, @OwnerUserId, @MemberUserIds)
                    ON CONFLICT(Id) DO UPDATE SET
                        Name          = excluded.Name,
                        Captain       = excluded.Captain,
                        City          = excluded.City,
                        LogoUrl       = excluded.LogoUrl,
                        OwnerUserId   = excluded.OwnerUserId,
                        MemberUserIds = excluded.MemberUserIds";

                string memberIds = team.MemberUserIds != null && team.MemberUserIds.Count > 0
                    ? string.Join(",", team.MemberUserIds.Select(id => id.ToString()))
                    : null;

                cmd.Parameters.AddWithValue("@Id", team.Id.ToString());
                cmd.Parameters.AddWithValue("@TournamentId", tournamentId.ToString());
                cmd.Parameters.AddWithValue("@Name", team.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@Captain", DbValue(team.Captain));
                cmd.Parameters.AddWithValue("@City", DbValue(team.City));
                cmd.Parameters.AddWithValue("@LogoUrl", DbValue(team.LogoUrl));
                cmd.Parameters.AddWithValue("@OwnerUserId", team.OwnerUserId.HasValue ? (object)team.OwnerUserId.Value.ToString() : DBNull.Value);
                cmd.Parameters.AddWithValue("@MemberUserIds", DbValue(memberIds));
                cmd.ExecuteNonQuery();
            }

            // Сохраняем игроков команды
            foreach (var player in team.Players ?? new List<Player>())
            {
                if (player.Id == Guid.Empty) player.Id = Guid.NewGuid();
                UpsertPlayer(conn, team.Id, player, tx);
            }
        }

        private void UpsertPlayer(SqliteConnection conn, Guid teamId, Player player,
                                   SqliteTransaction tx = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO Players (Id, TeamId, Name, Role, IsCaptain, UserId)
                    VALUES (@Id, @TeamId, @Name, @Role, @IsCaptain, @UserId)
                    ON CONFLICT(Id) DO UPDATE SET
                        Name      = excluded.Name,
                        Role      = excluded.Role,
                        IsCaptain = excluded.IsCaptain,
                        UserId    = excluded.UserId";

                cmd.Parameters.AddWithValue("@Id", player.Id.ToString());
                cmd.Parameters.AddWithValue("@TeamId", teamId.ToString());
                cmd.Parameters.AddWithValue("@Name", player.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@Role", player.Role ?? "Обычный игрок");
                cmd.Parameters.AddWithValue("@IsCaptain", player.IsCaptain ? 1 : 0);
                cmd.Parameters.AddWithValue("@UserId", player.UserId.HasValue ? (object)player.UserId.Value.ToString() : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private void UpsertMatch(SqliteConnection conn, Guid tournamentId, Match match,
                                  SqliteTransaction tx = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO Matches
                        (Id, TournamentId, Round, RoundName, MatchNumber,
                         Team1, Team2, Status, Team1QuickScore, Team2QuickScore,
                         SetsScore, SetsBySet, TotalScore, Duration,
                         Referee, Location, Mvp, WinnerId, CreatedByUserId)
                    VALUES
                        (@Id, @TournamentId, @Round, @RoundName, @MatchNumber,
                         @Team1, @Team2, @Status, @Team1QuickScore, @Team2QuickScore,
                         @SetsScore, @SetsBySet, @TotalScore, @Duration,
                         @Referee, @Location, @Mvp, @WinnerId, @CreatedByUserId)
                    ON CONFLICT(Id) DO UPDATE SET
                        Round           = excluded.Round,
                        RoundName       = excluded.RoundName,
                        MatchNumber     = excluded.MatchNumber,
                        Team1           = excluded.Team1,
                        Team2           = excluded.Team2,
                        Status          = excluded.Status,
                        Team1QuickScore = excluded.Team1QuickScore,
                        Team2QuickScore = excluded.Team2QuickScore,
                        SetsScore       = excluded.SetsScore,
                        SetsBySet       = excluded.SetsBySet,
                        TotalScore      = excluded.TotalScore,
                        Duration        = excluded.Duration,
                        Referee         = excluded.Referee,
                        Location        = excluded.Location,
                        Mvp             = excluded.Mvp,
                        WinnerId        = excluded.WinnerId,
                        CreatedByUserId = excluded.CreatedByUserId";

                cmd.Parameters.AddWithValue("@Id", match.Id.ToString());
                cmd.Parameters.AddWithValue("@TournamentId", tournamentId.ToString());
                cmd.Parameters.AddWithValue("@Round", match.Round);
                cmd.Parameters.AddWithValue("@RoundName", DbValue(match.RoundName));
                cmd.Parameters.AddWithValue("@MatchNumber", match.MatchNumber.HasValue ? (object)match.MatchNumber.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Team1", DbValue(match.Team1));
                cmd.Parameters.AddWithValue("@Team2", DbValue(match.Team2));
                cmd.Parameters.AddWithValue("@Status", match.Status ?? "Не сыгран");
                cmd.Parameters.AddWithValue("@Team1QuickScore", DbValue(match.Team1QuickScore));
                cmd.Parameters.AddWithValue("@Team2QuickScore", DbValue(match.Team2QuickScore));
                cmd.Parameters.AddWithValue("@SetsScore", DbValue(match.SetsScore));
                cmd.Parameters.AddWithValue("@SetsBySet", DbValue(match.SetsBySet));
                cmd.Parameters.AddWithValue("@TotalScore", DbValue(match.TotalScore));
                cmd.Parameters.AddWithValue("@Duration", DbValue(match.Duration));
                cmd.Parameters.AddWithValue("@Referee", DbValue(match.Referee));
                cmd.Parameters.AddWithValue("@Location", DbValue(match.Location));
                cmd.Parameters.AddWithValue("@Mvp", DbValue(match.Mvp));
                cmd.Parameters.AddWithValue("@WinnerId", match.WinnerId.HasValue ? (object)match.WinnerId.Value.ToString() : DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedByUserId", match.CreatedByUserId.HasValue ? (object)match.CreatedByUserId.Value.ToString() : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        // ─── Чтение строк из Reader ──────────────────────────────────────────────

        private Tournament ReadTournamentRow(SqliteDataReader r)
        {
            var t = new Tournament
            {
                Id              = Guid.Parse(r["Id"].ToString()),
                Name            = r["Name"].ToString(),
                SportType       = ReadString(r, "SportType"),
                Type            = ReadString(r, "Type"),
                Status          = r["Status"].ToString(),
                EditMode        = ReadString(r, "EditMode"),
                AccessMode      = ReadString(r, "AccessMode"),
                IsPublic        = Convert.ToInt32(r["IsPublic"]) == 1,
                NoScore         = Convert.ToInt32(r["NoScore"]) == 1,
                Description     = ReadString(r, "Description"),
                City            = ReadString(r, "City"),
                Contacts        = ReadString(r, "Contacts"),
                LogoUrl         = ReadString(r, "LogoUrl"),
                InviteCode      = ReadString(r, "InviteCode"),
                ScoringSystem   = ReadString(r, "ScoringSystem"),
                CustomWinPoints  = Convert.ToInt32(r["CustomWinPoints"]),
                CustomDrawPoints = Convert.ToInt32(r["CustomDrawPoints"]),
                CustomLossPoints = Convert.ToInt32(r["CustomLossPoints"]),
            };

            // StartDate
            if (DateTime.TryParse(r["StartDate"].ToString(), out DateTime startDate))
                t.StartDate = startDate;

            // EndDate
            string endDateStr = ReadString(r, "EndDate");
            if (!string.IsNullOrEmpty(endDateStr) && DateTime.TryParse(endDateStr, out DateTime endDate))
                t.EndDate = endDate;

            // OwnerId
            t.OwnerId = ReadString(r, "OwnerId");

            // AllowedUserIds
            string allowedStr = ReadString(r, "AllowedUserIds");
            if (!string.IsNullOrEmpty(allowedStr))
            {
                t.AllowedUserIds = new List<Guid>();
                foreach (var part in allowedStr.Split(','))
                {
                    if (Guid.TryParse(part.Trim(), out Guid uid))
                        t.AllowedUserIds.Add(uid);
                }
            }

            // BracketJson
            string bracketJson = ReadString(r, "BracketJson");
            if (!string.IsNullOrEmpty(bracketJson))
            {
                try
                {
                    t.Bracket = JsonConvert.DeserializeObject<TournamentBracket>(bracketJson);
                    t.Bracket?.ReconnectReferences();
                }
                catch
                {
                    t.Bracket = null;
                }
            }

            return t;
        }

        private List<Team> LoadTeamsForTournament(SqliteConnection conn, Guid tournamentId)
        {
            var teams = new List<Team>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Teams WHERE TournamentId = @TId ORDER BY Name";
                cmd.Parameters.AddWithValue("@TId", tournamentId.ToString());
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var team = new Team
                        {
                            Id        = Guid.Parse(r["Id"].ToString()),
                            Name      = r["Name"].ToString(),
                            Captain   = ReadString(r, "Captain"),
                            City      = ReadString(r, "City"),
                            LogoUrl   = ReadString(r, "LogoUrl"),
                        };

                        string ownerStr = ReadString(r, "OwnerUserId");
                        if (!string.IsNullOrEmpty(ownerStr) && Guid.TryParse(ownerStr, out Guid ownerGuid))
                            team.OwnerUserId = ownerGuid;

                        string memberStr = ReadString(r, "MemberUserIds");
                        if (!string.IsNullOrEmpty(memberStr))
                        {
                            foreach (var part in memberStr.Split(','))
                            {
                                if (Guid.TryParse(part.Trim(), out Guid muid))
                                    team.MemberUserIds.Add(muid);
                            }
                        }

                        teams.Add(team);
                    }
                }
            }

            // Загружаем игроков для каждой команды
            foreach (var team in teams)
            {
                team.Players = LoadPlayersForTeam(conn, team.Id);
            }

            return teams;
        }

        private List<Player> LoadPlayersForTeam(SqliteConnection conn, Guid teamId)
        {
            var players = new List<Player>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Players WHERE TeamId = @TId ORDER BY Name";
                cmd.Parameters.AddWithValue("@TId", teamId.ToString());
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var player = new Player
                        {
                            Id        = Guid.Parse(r["Id"].ToString()),
                            Name      = r["Name"].ToString(),
                            Role      = ReadString(r, "Role") ?? "Обычный игрок",
                            IsCaptain = Convert.ToInt32(r["IsCaptain"]) == 1,
                        };

                        string userIdStr = ReadString(r, "UserId");
                        if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out Guid uid))
                            player.UserId = uid;

                        players.Add(player);
                    }
                }
            }

            return players;
        }

        private List<Match> LoadMatchesForTournament(SqliteConnection conn, Guid tournamentId)
        {
            var matches = new List<Match>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Matches WHERE TournamentId = @TId ORDER BY Round, MatchNumber";
                cmd.Parameters.AddWithValue("@TId", tournamentId.ToString());
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var match = new Match
                        {
                            Id             = Guid.Parse(r["Id"].ToString()),
                            Round          = Convert.ToInt32(r["Round"]),
                            Team1          = ReadString(r, "Team1"),
                            Team2          = ReadString(r, "Team2"),
                            Status         = r["Status"].ToString(),
                            Team1QuickScore = ReadString(r, "Team1QuickScore"),
                            Team2QuickScore = ReadString(r, "Team2QuickScore"),
                            SetsScore      = ReadString(r, "SetsScore"),
                            SetsBySet      = ReadString(r, "SetsBySet"),
                            TotalScore     = ReadString(r, "TotalScore"),
                            Duration       = ReadString(r, "Duration"),
                            Referee        = ReadString(r, "Referee"),
                            Location       = ReadString(r, "Location"),
                            Mvp            = ReadString(r, "Mvp"),
                        };

                        // MatchNumber
                        var matchNumObj = r["MatchNumber"];
                        if (matchNumObj != DBNull.Value)
                            match.MatchNumber = Convert.ToInt32(matchNumObj);

                        // WinnerId
                        string winnerStr = ReadString(r, "WinnerId");
                        if (!string.IsNullOrEmpty(winnerStr) && Guid.TryParse(winnerStr, out Guid winner))
                            match.WinnerId = winner;

                        // CreatedByUserId
                        string createdStr = ReadString(r, "CreatedByUserId");
                        if (!string.IsNullOrEmpty(createdStr) && Guid.TryParse(createdStr, out Guid creator))
                            match.CreatedByUserId = creator;

                        matches.Add(match);
                    }
                }
            }

            return matches;
        }

        private Dictionary<string, string> LoadTeamNameToIdMap(SqliteConnection conn, Guid tournamentId)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name FROM Teams WHERE TournamentId = @TId";
                cmd.Parameters.AddWithValue("@TId", tournamentId.ToString());
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string name = r["Name"].ToString();
                        string id   = r["Id"].ToString();
                        if (!map.ContainsKey(name))
                            map[name] = id;
                    }
                }
            }
            return map;
        }

        // ─── Вспомогательные методы ──────────────────────────────────────────────

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_db.GetConnectionString());
            conn.Open();
            // Включаем внешние ключи в каждой сессии (SQLite требует это поле из каждого соединения)
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }
            return conn;
        }

        private static object DbValue(string value)
        {
            return string.IsNullOrEmpty(value) ? (object)DBNull.Value : value;
        }

        private static string ReadString(SqliteDataReader r, string columnName)
        {
            object val = r[columnName];
            return val == DBNull.Value ? null : val?.ToString();
        }
    }
}
