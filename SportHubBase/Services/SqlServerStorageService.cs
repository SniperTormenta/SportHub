// Services/SqlServerStorageService.cs
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace SportHubBase.Services
{
    /// <summary>
    /// Реализация IStorage для работы с существующей внешней базой данных MS SQL Server.
    /// </summary>
    public class SqlServerStorageService : IStorage
    {
        private readonly SqlServerDatabaseService _db;

        public SqlServerStorageService(SqlServerDatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ТУРНИРЫ
        // ─────────────────────────────────────────────────────────────────────────

        public List<Tournament> LoadTournaments()
        {
            var tournaments = new List<Tournament>();

            using (var conn = OpenConnection())
            {
                var accountNames = LoadAccountNames(conn);

                using (var cmd = conn.CreateCommand())
                {
                    string currentUserId = CurrentSession.CurrentUser?.Id.ToString();

                    // Используем оператор "+" для конкатенации строк в T-SQL вместо "||" в SQLite
                    cmd.CommandText = @"
                        SELECT * FROM Tournaments 
                        WHERE IsPublic = 1 
                           OR OwnerId = @CurrentUserId
                           OR (AllowedUserIds IS NOT NULL AND 
                               (AllowedUserIds = @CurrentUserId 
                                OR AllowedUserIds LIKE @CurrentUserId + ',%' 
                                OR AllowedUserIds LIKE '%,' + @CurrentUserId + ',%' 
                                OR AllowedUserIds LIKE '%,' + @CurrentUserId))
                        ORDER BY StartDate DESC";

                    cmd.Parameters.AddWithValue("@CurrentUserId", (object)currentUserId ?? DBNull.Value);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var t = ReadTournamentRow(reader);
                            if (!string.IsNullOrEmpty(t.OwnerId) && accountNames.TryGetValue(t.OwnerId, out string ownerName))
                            {
                                t.OwnerName = ownerName;
                            }
                            tournaments.Add(t);
                        }
                    }
                }

                foreach (var t in tournaments)
                {
                    t.Teams = LoadTeamsForTournament(conn, t.Id);
                    t.Matches = LoadMatchesForTournament(conn, t.Id);
                }
            }

            return tournaments;
        }

        private Dictionary<string, string> LoadAccountNames(Microsoft.Data.SqlClient.SqlConnection conn)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Username FROM Accounts";
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string id = r["Id"].ToString();
                        string name = r["Username"].ToString();
                        if (!map.ContainsKey(id)) map[id] = name;
                    }
                }
            }
            return map;
        }

        public void SaveTournaments(List<Tournament> tournaments)
        {
            if (tournaments == null) return;
            foreach (var t in tournaments) UpsertTournamentCore(t);
        }

        public void CreateTournament(Tournament tournament)
        {
            if (tournament == null) throw new ArgumentNullException(nameof(tournament));
            if (tournament.Id == Guid.Empty) tournament.Id = Guid.NewGuid();
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
            using (var conn = OpenConnection()) UpsertTeam(conn, tournamentId, team);
        }

        public void UpdateTeam(Guid tournamentId, Team team)
        {
            if (team == null) throw new ArgumentNullException(nameof(team));
            using (var conn = OpenConnection()) UpsertTeam(conn, tournamentId, team);
        }

        public void DeleteTeam(Guid tournamentId, Guid teamId)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
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
            using (var conn = OpenConnection()) UpsertPlayer(conn, teamId, player);
        }

        public void UpdatePlayer(Guid teamId, Player player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            using (var conn = OpenConnection()) UpsertPlayer(conn, teamId, player);
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
            using (var conn = OpenConnection()) UpsertMatch(conn, tournamentId, match);
        }

        public void DeleteMatch(Guid matchId)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Matches WHERE Id = @Id";
                cmd.Parameters.AddWithValue("@Id", matchId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // СТЕНДИНГИ
        // ─────────────────────────────────────────────────────────────────────────

        public void SaveStandings(Guid tournamentId, IEnumerable<ResultRow> rows)
        {
            if (rows == null) return;

            using (var conn = OpenConnection())
            {
                var teamIds = LoadTeamNameToIdMap(conn, tournamentId);

                foreach (var row in rows)
                {
                    if (!teamIds.TryGetValue(row.TeamName ?? string.Empty, out string teamId)) continue;

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            IF EXISTS (SELECT 1 FROM TournamentStandings WHERE TournamentId = @TournamentId AND TeamId = @TeamId)
                            BEGIN
                                UPDATE TournamentStandings SET
                                    Place = @Place, Wins = @Wins, Losses = @Losses, Draws = @Draws,
                                    SetsWon = @SetsWon, SetsLost = @SetsLost, PointsScored = @PointsScored,
                                    PointsConceded = @PointsConceded, Points = @Points
                                WHERE TournamentId = @TournamentId AND TeamId = @TeamId
                            END
                            ELSE
                            BEGIN
                                INSERT INTO TournamentStandings
                                    (TournamentId, TeamId, Place, Wins, Losses, Draws, SetsWon, SetsLost, PointsScored, PointsConceded, Points)
                                VALUES
                                    (@TournamentId, @TeamId, @Place, @Wins, @Losses, @Draws, @SetsWon, @SetsLost, @PointsScored, @PointsConceded, @Points)
                            END";

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
        // ПРИВАТНЫЕ МЕТОДЫ ОПЕРАЦИЙ ДАННЫХ
        // ─────────────────────────────────────────────────────────────────────────

        private void UpsertTournamentCore(Tournament t)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"
                            IF EXISTS (SELECT 1 FROM Tournaments WHERE Id = @Id)
                            BEGIN
                                UPDATE Tournaments SET
                                    Name = @Name, SportType = @SportType, Type = @Type, Status = @Status,
                                    EditMode = @EditMode, AccessMode = @AccessMode, IsPublic = @IsPublic,
                                    NoScore = @NoScore, Description = @Description, City = @City, Contacts = @Contacts,
                                    LogoUrl = @LogoUrl, InviteCode = @InviteCode, StartDate = @StartDate, EndDate = @EndDate,
                                    OwnerId = @OwnerId, AllowedUserIds = @AllowedUserIds, ScoringSystem = @ScoringSystem,
                                    CustomWinPoints = @CustomWinPoints, CustomDrawPoints = @CustomDrawPoints, 
                                    CustomLossPoints = @CustomLossPoints, BracketJson = @BracketJson
                                WHERE Id = @Id
                            END
                            ELSE
                            BEGIN
                                INSERT INTO Tournaments
                                    (Id, Name, SportType, Type, Status, EditMode, AccessMode, IsPublic, NoScore, Description, City, Contacts, LogoUrl, InviteCode, StartDate, EndDate, OwnerId, AllowedUserIds, ScoringSystem, CustomWinPoints, CustomDrawPoints, CustomLossPoints, BracketJson)
                                VALUES
                                    (@Id, @Name, @SportType, @Type, @Status, @EditMode, @AccessMode, @IsPublic, @NoScore, @Description, @City, @Contacts, @LogoUrl, @InviteCode, @StartDate, @EndDate, @OwnerId, @AllowedUserIds, @ScoringSystem, @CustomWinPoints, @CustomDrawPoints, @CustomLossPoints, @BracketJson)
                            END";

                        string bracketJson = t.Bracket != null ? JsonConvert.SerializeObject(t.Bracket, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }) : null;
                        string allowedIds = t.AllowedUserIds != null && t.AllowedUserIds.Count > 0 ? string.Join(",", t.AllowedUserIds.Select(id => id.ToString())) : null;

                        cmd.Parameters.AddWithValue("@Id", t.Id.ToString());
                        cmd.Parameters.AddWithValue("@Name", t.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@SportType", DbValue(t.SportType));
                        cmd.Parameters.AddWithValue("@Type", DbValue(t.Type));
                        cmd.Parameters.AddWithValue("@Status", t.Status ?? "Не начат");
                        cmd.Parameters.AddWithValue("@EditMode", DbValue(t.EditMode));
                        cmd.Parameters.AddWithValue("@AccessMode", DbValue(t.AccessMode));
                        cmd.Parameters.AddWithValue("@IsPublic", t.IsPublic);
                        cmd.Parameters.AddWithValue("@NoScore", t.NoScore);
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

                    var validTeamIds = new List<string>();
                    foreach (var team in t.Teams ?? new List<Team>())
                    {
                        UpsertTeam(conn, t.Id, team, tx);
                        validTeamIds.Add($"'{team.Id}'");
                    }

                    using (var delCmd = conn.CreateCommand())
                    {
                        delCmd.Transaction = tx;
                        if (validTeamIds.Count > 0)
                            delCmd.CommandText = $"DELETE FROM Teams WHERE TournamentId = '{t.Id}' AND Id NOT IN ({string.Join(",", validTeamIds)})";
                        else
                            delCmd.CommandText = $"DELETE FROM Teams WHERE TournamentId = '{t.Id}'";
                        delCmd.ExecuteNonQuery();
                    }

                    var validMatchIds = new List<string>();
                    foreach (var match in t.Matches ?? new List<Match>())
                    {
                        UpsertMatch(conn, t.Id, match, tx);
                        validMatchIds.Add($"'{match.Id}'");
                    }

                    using (var delCmd = conn.CreateCommand())
                    {
                        delCmd.Transaction = tx;
                        if (validMatchIds.Count > 0)
                            delCmd.CommandText = $"DELETE FROM Matches WHERE TournamentId = '{t.Id}' AND Id NOT IN ({string.Join(",", validMatchIds)})";
                        else
                            delCmd.CommandText = $"DELETE FROM Matches WHERE TournamentId = '{t.Id}'";
                        delCmd.ExecuteNonQuery();
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

        private void UpsertTeam(Microsoft.Data.SqlClient.SqlConnection conn, Guid tournamentId, Team team, Microsoft.Data.SqlClient.SqlTransaction tx = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    IF EXISTS (SELECT 1 FROM Teams WHERE Id = @Id)
                    BEGIN
                        UPDATE Teams SET Name = @Name, Captain = @Captain, City = @City, LogoUrl = @LogoUrl, OwnerUserId = @OwnerUserId, MemberUserIds = @MemberUserIds WHERE Id = @Id
                    END
                    ELSE
                    BEGIN
                        INSERT INTO Teams (Id, TournamentId, Name, Captain, City, LogoUrl, OwnerUserId, MemberUserIds)
                        VALUES (@Id, @TournamentId, @Name, @Captain, @City, @LogoUrl, @OwnerUserId, @MemberUserIds)
                    END";

                string memberIds = team.MemberUserIds != null && team.MemberUserIds.Count > 0 ? string.Join(",", team.MemberUserIds.Select(id => id.ToString())) : null;
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

            var validPlayerIds = new List<string>();
            foreach (var player in team.Players ?? new List<Player>())
            {
                if (player.Id == Guid.Empty) player.Id = Guid.NewGuid();
                UpsertPlayer(conn, team.Id, player, tx);
                validPlayerIds.Add($"'{player.Id}'");
            }

            using (var delCmd = conn.CreateCommand())
            {
                delCmd.Transaction = tx;
                if (validPlayerIds.Count > 0)
                    delCmd.CommandText = $"DELETE FROM Players WHERE TeamId = '{team.Id}' AND Id NOT IN ({string.Join(",", validPlayerIds)})";
                else
                    delCmd.CommandText = $"DELETE FROM Players WHERE TeamId = '{team.Id}'";
                delCmd.ExecuteNonQuery();
            }
        }

        private void UpsertPlayer(Microsoft.Data.SqlClient.SqlConnection conn, Guid teamId, Player player, Microsoft.Data.SqlClient.SqlTransaction tx = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    IF EXISTS (SELECT 1 FROM Players WHERE Id = @Id)
                    BEGIN
                        UPDATE Players SET Name = @Name, Role = @Role, IsCaptain = @IsCaptain, UserId = @UserId WHERE Id = @Id
                    END
                    ELSE
                    BEGIN
                        INSERT INTO Players (Id, TeamId, Name, Role, IsCaptain, UserId)
                        VALUES (@Id, @TeamId, @Name, @Role, @IsCaptain, @UserId)
                    END";

                cmd.Parameters.AddWithValue("@Id", player.Id.ToString());
                cmd.Parameters.AddWithValue("@TeamId", teamId.ToString());
                cmd.Parameters.AddWithValue("@Name", player.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@Role", player.Role ?? "Обычный игрок");
                cmd.Parameters.AddWithValue("@IsCaptain", player.IsCaptain);
                cmd.Parameters.AddWithValue("@UserId", player.UserId.HasValue ? (object)player.UserId.Value.ToString() : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private void UpsertMatch(Microsoft.Data.SqlClient.SqlConnection conn, Guid tournamentId, Match match, Microsoft.Data.SqlClient. SqlTransaction tx = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    IF EXISTS (SELECT 1 FROM Matches WHERE Id = @Id)
                    BEGIN
                        UPDATE Matches SET Round = @Round, RoundName = @RoundName, MatchNumber = @MatchNumber, Team1 = @Team1, Team2 = @Team2, Status = @Status, Team1QuickScore = @Team1QuickScore, Team2QuickScore = @Team2QuickScore, SetsScore = @SetsScore, SetsBySet = @SetsBySet, TotalScore = @TotalScore, Duration = @Duration, Referee = @Referee, Location = @Location, Mvp = @Mvp, WinnerId = @WinnerId, CreatedByUserId = @CreatedByUserId WHERE Id = @Id
                    END
                    ELSE
                    BEGIN
                        INSERT INTO Matches (Id, TournamentId, Round, RoundName, MatchNumber, Team1, Team2, Status, Team1QuickScore, Team2QuickScore, SetsScore, SetsBySet, TotalScore, Duration, Referee, Location, Mvp, WinnerId, CreatedByUserId)
                        VALUES (@Id, @TournamentId, @Round, @RoundName, @MatchNumber, @Team1, @Team2, @Status, @Team1QuickScore, @Team2QuickScore, @SetsScore, @SetsBySet, @TotalScore, @Duration, @Referee, @Location, @Mvp, @WinnerId, @CreatedByUserId)
                    END";

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

        private Tournament ReadTournamentRow(Microsoft.Data.SqlClient.SqlDataReader r)
        {
            var t = new Tournament
            {
                Id = Guid.Parse(r["Id"].ToString()),
                Name = r["Name"].ToString(),
                SportType = ReadString(r, "SportType"),
                Type = ReadString(r, "Type"),
                Status = r["Status"].ToString(),
                EditMode = ReadString(r, "EditMode"),
                AccessMode = ReadString(r, "AccessMode"),
                IsPublic = Convert.ToBoolean(r["IsPublic"]),
                NoScore = Convert.ToBoolean(r["NoScore"]),
                Description = ReadString(r, "Description"),
                City = ReadString(r, "City"),
                Contacts = ReadString(r, "Contacts"),
                LogoUrl = ReadString(r, "LogoUrl"),
                InviteCode = ReadString(r, "InviteCode"),
                ScoringSystem = ReadString(r, "ScoringSystem"),
                CustomWinPoints = Convert.ToInt32(r["CustomWinPoints"]),
                CustomDrawPoints = Convert.ToInt32(r["CustomDrawPoints"]),
                CustomLossPoints = Convert.ToInt32(r["CustomLossPoints"]),
            };

            if (r["StartDate"] != DBNull.Value)
            {
                if (r["StartDate"] is DateTime dt) t.StartDate = dt;
                else if (DateTime.TryParse(r["StartDate"].ToString(), out DateTime parsedDt)) t.StartDate = parsedDt;
            }

            if (r["EndDate"] != DBNull.Value)
            {
                if (r["EndDate"] is DateTime dt) t.EndDate = dt;
                else if (DateTime.TryParse(r["EndDate"].ToString(), out DateTime parsedDt)) t.EndDate = parsedDt;
            }

            t.OwnerId = ReadString(r, "OwnerId");
            string allowedStr = ReadString(r, "AllowedUserIds");
            if (!string.IsNullOrEmpty(allowedStr))
            {
                t.AllowedUserIds = new List<Guid>();
                foreach (var part in allowedStr.Split(','))
                    if (Guid.TryParse(part.Trim(), out Guid uid)) t.AllowedUserIds.Add(uid);
            }

            string bracketJson = ReadString(r, "BracketJson");
            if (!string.IsNullOrEmpty(bracketJson))
            {
                try
                {
                    t.Bracket = JsonConvert.DeserializeObject<TournamentBracket>(bracketJson);
                    t.Bracket?.ReconnectReferences();
                }
                catch { t.Bracket = null; }
            }
            return t;
        }

        private List<Team> LoadTeamsForTournament(Microsoft.Data.SqlClient.SqlConnection conn, Guid tournamentId)
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
                            Id = Guid.Parse(r["Id"].ToString()),
                            Name = r["Name"].ToString(),
                            Captain = ReadString(r, "Captain"),
                            City = ReadString(r, "City"),
                            LogoUrl = ReadString(r, "LogoUrl"),
                        };
                        string ownerStr = ReadString(r, "OwnerUserId");
                        if (!string.IsNullOrEmpty(ownerStr) && Guid.TryParse(ownerStr, out Guid ownerGuid)) team.OwnerUserId = ownerGuid;

                        string memberStr = ReadString(r, "MemberUserIds");
                        if (!string.IsNullOrEmpty(memberStr))
                            foreach (var part in memberStr.Split(','))
                                if (Guid.TryParse(part.Trim(), out Guid muid)) team.MemberUserIds.Add(muid);
                        teams.Add(team);
                    }
                }
            }
            foreach (var team in teams) team.Players = LoadPlayersForTeam(conn, team.Id);
            return teams;
        }

        private List<Player> LoadPlayersForTeam(Microsoft.Data.SqlClient.SqlConnection conn, Guid teamId)
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
                            Id = Guid.Parse(r["Id"].ToString()),
                            Name = r["Name"].ToString(),
                            Role = ReadString(r, "Role") ?? "Обычный игрок",
                            IsCaptain = Convert.ToBoolean(r["IsCaptain"]),
                        };
                        string userIdStr = ReadString(r, "UserId");
                        if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out Guid uid)) player.UserId = uid;
                        players.Add(player);
                    }
                }
            }
            return players;
        }

        private List<Match> LoadMatchesForTournament(Microsoft.Data.SqlClient.SqlConnection conn, Guid tournamentId)
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
                            Id = Guid.Parse(r["Id"].ToString()),
                            Round = Convert.ToInt32(r["Round"]),
                            Team1 = ReadString(r, "Team1"),
                            Team2 = ReadString(r, "Team2"),
                            Status = r["Status"].ToString(),
                            Team1QuickScore = ReadString(r, "Team1QuickScore"),
                            Team2QuickScore = ReadString(r, "Team2QuickScore"),
                            SetsScore = ReadString(r, "SetsScore"),
                            SetsBySet = ReadString(r, "SetsBySet"),
                            TotalScore = ReadString(r, "TotalScore"),
                            Duration = ReadString(r, "Duration"),
                            Referee = ReadString(r, "Referee"),
                            Location = ReadString(r, "Location"),
                            Mvp = ReadString(r, "Mvp"),
                        };
                        if (r["MatchNumber"] != DBNull.Value) match.MatchNumber = Convert.ToInt32(r["MatchNumber"]);
                        string winnerStr = ReadString(r, "WinnerId");
                        if (!string.IsNullOrEmpty(winnerStr) && Guid.TryParse(winnerStr, out Guid winner)) match.WinnerId = winner;
                        string createdStr = ReadString(r, "CreatedByUserId");
                        if (!string.IsNullOrEmpty(createdStr) && Guid.TryParse(createdStr, out Guid creator)) match.CreatedByUserId = creator;
                        matches.Add(match);
                    }
                }
            }
            return matches;
        }

        private Dictionary<string, string> LoadTeamNameToIdMap(Microsoft.Data.SqlClient.SqlConnection conn, Guid tournamentId)
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
                        string id = r["Id"].ToString();
                        if (!map.ContainsKey(name)) map[name] = id;
                    }
                }
            }
            return map;
        }

        private Microsoft.Data.SqlClient.SqlConnection OpenConnection()
        {
            var conn = new Microsoft.Data.SqlClient.SqlConnection(_db.GetConnectionString());
            conn.Open();
            return conn;
        }

        private static object DbValue(string value) => string.IsNullOrEmpty(value) ? (object)DBNull.Value : value;
        private static string ReadString(Microsoft.Data.SqlClient.SqlDataReader r, string columnName) => r[columnName] == DBNull.Value ? null : r[columnName]?.ToString();
    }
}