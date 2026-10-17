using System;
using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using SportHubBase.Models;

namespace SportHubBase.Services
{
    public class SqlServerDatabaseService
    {
        private readonly ConcurrentDictionary<string, bool> _initializedServers = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public string GetConnectionString(string serverAddress = null, int timeoutSeconds = 4)
        {
            string input = !string.IsNullOrWhiteSpace(serverAddress)
                ? serverAddress.Trim()
                : DatabaseConfig.SqlConnectionString?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                throw new InvalidOperationException("Не указан адрес SQL Server.");
            }

            // 1. Введена полная строка подключения
            if (input.IndexOf("Server=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                input.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (input.IndexOf("Connect Timeout=", StringComparison.OrdinalIgnoreCase) < 0 &&
                    input.IndexOf("Connection Timeout=", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return input.TrimEnd(';') + string.Format(";Connect Timeout={0};", timeoutSeconds);
                }
                return input;
            }

            // 2. Локальный SQL Server (Windows-авторизация)
            if (input.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("127.0.0.1") ||
                input.Equals(".") ||
                input.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                (input.Contains("\\") && !input.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase)))
            {
                return string.Format("Server={0};Database=SportHub;Integrated Security=True;TrustServerCertificate=True;Connect Timeout={1};", input, timeoutSeconds);
            }

            // 3. Внешний адрес (IP, домен, tcp:)
            string dbUser = "Seek";
            string dbPass = "SSSzxcgoule";

            return string.Format("Server={0};Database=SportHub;User Id={1};Password={2};TrustServerCertificate=True;Connect Timeout={3};", input, dbUser, dbPass, timeoutSeconds);
        }

        public SqlConnection OpenConnection(string serverAddress = null, int timeoutSeconds = 4)
        {
            string connStr = GetConnectionString(serverAddress, timeoutSeconds);
            var conn = new SqlConnection(connStr);
            conn.Open();

            if (!_initializedServers.ContainsKey(connStr))
            {
                EnsureSchema(conn);
                _initializedServers[connStr] = true;
            }

            return conn;
        }

        private void EnsureSchema(SqlConnection conn)
        {
            string sql = @"
                IF OBJECT_ID('dbo.Tournaments', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Tournaments (
                        Id NVARCHAR(64) NOT NULL PRIMARY KEY,
                        Name NVARCHAR(255) NOT NULL,
                        SportType NVARCHAR(100) NULL,
                        Type NVARCHAR(100) NULL,
                        TotalRounds INT NOT NULL DEFAULT 0,
                        Status NVARCHAR(100) NOT NULL DEFAULT N'Не начат',
                        EditMode NVARCHAR(100) NULL,
                        AccessMode NVARCHAR(100) NULL,
                        IsPublic BIT NOT NULL DEFAULT 1,
                        NoScore BIT NOT NULL DEFAULT 0,
                        Description NVARCHAR(MAX) NULL,
                        City NVARCHAR(150) NULL,
                        Contacts NVARCHAR(500) NULL,
                        LogoUrl NVARCHAR(500) NULL,
                        InviteCode NVARCHAR(100) NULL,
                        StartDate NVARCHAR(64) NOT NULL,
                        EndDate NVARCHAR(64) NULL,
                        OwnerId NVARCHAR(100) NULL,
                        AllowedUserIds NVARCHAR(MAX) NULL,
                        ScoringSystem NVARCHAR(100) NULL,
                        CustomWinPoints INT NOT NULL DEFAULT 3,
                        CustomDrawPoints INT NOT NULL DEFAULT 1,
                        CustomLossPoints INT NOT NULL DEFAULT 0,
                        TieBreakerRule NVARCHAR(100) NULL,
                        BracketJson NVARCHAR(MAX) NULL,
                        OrganizerLogin NVARCHAR(150) NULL,
                        OrganizerPasswordHash NVARCHAR(255) NULL,
                        IsDeleted BIT NOT NULL DEFAULT 0
                    );
                END

                IF COL_LENGTH('dbo.Tournaments', 'TotalRounds') IS NULL
                    ALTER TABLE dbo.Tournaments ADD TotalRounds INT NOT NULL DEFAULT 0;
                IF COL_LENGTH('dbo.Tournaments', 'TieBreakerRule') IS NULL
                    ALTER TABLE dbo.Tournaments ADD TieBreakerRule NVARCHAR(100) NULL;
                IF COL_LENGTH('dbo.Tournaments', 'OrganizerLogin') IS NULL
                    ALTER TABLE dbo.Tournaments ADD OrganizerLogin NVARCHAR(150) NULL;
                IF COL_LENGTH('dbo.Tournaments', 'OrganizerPasswordHash') IS NULL
                    ALTER TABLE dbo.Tournaments ADD OrganizerPasswordHash NVARCHAR(255) NULL;
                IF COL_LENGTH('dbo.Tournaments', 'IsDeleted') IS NULL
                    ALTER TABLE dbo.Tournaments ADD IsDeleted BIT NOT NULL DEFAULT 0;
                IF COL_LENGTH('dbo.Tournaments', 'CardBackgroundData') IS NULL
                    ALTER TABLE dbo.Tournaments ADD CardBackgroundData NVARCHAR(MAX) NULL;

                IF OBJECT_ID('dbo.Teams', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Teams (
                        Id NVARCHAR(64) NOT NULL PRIMARY KEY,
                        TournamentId NVARCHAR(64) NOT NULL,
                        Name NVARCHAR(255) NOT NULL,
                        Captain NVARCHAR(255) NULL,
                        City NVARCHAR(150) NULL,
                        LogoUrl NVARCHAR(MAX) NULL,
                        OwnerUserId NVARCHAR(100) NULL,
                        MemberUserIds NVARCHAR(MAX) NULL,
                        IsDeleted BIT NOT NULL DEFAULT 0
                    );
                END

                IF OBJECT_ID('dbo.Players', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Players (
                        Id NVARCHAR(64) NOT NULL PRIMARY KEY,
                        TeamId NVARCHAR(64) NOT NULL,
                        Name NVARCHAR(255) NOT NULL,
                        Role NVARCHAR(100) NOT NULL DEFAULT N'Обычный игрок',
                        IsCaptain BIT NOT NULL DEFAULT 0,
                        UserId NVARCHAR(100) NULL,
                        IsDeleted BIT NOT NULL DEFAULT 0
                    );
                END

                IF OBJECT_ID('dbo.Matches', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Matches (
                        Id NVARCHAR(64) NOT NULL PRIMARY KEY,
                        TournamentId NVARCHAR(64) NOT NULL,
                        Round INT NOT NULL DEFAULT 0,
                        RoundName NVARCHAR(150) NULL,
                        MatchNumber INT NULL,
                        Team1 NVARCHAR(255) NULL,
                        Team2 NVARCHAR(255) NULL,
                        Status NVARCHAR(100) NOT NULL DEFAULT N'Не сыгран',
                        Team1QuickScore NVARCHAR(50) NULL,
                        Team2QuickScore NVARCHAR(50) NULL,
                        SetsScore NVARCHAR(100) NULL,
                        SetsBySet NVARCHAR(255) NULL,
                        TotalScore NVARCHAR(100) NULL,
                        Duration NVARCHAR(100) NULL,
                        Referee NVARCHAR(255) NULL,
                        Location NVARCHAR(255) NULL,
                        Mvp NVARCHAR(255) NULL,
                        MatchDate NVARCHAR(64) NULL,
                        StartTime NVARCHAR(50) NULL,
                        WinnerId NVARCHAR(100) NULL,
                        CreatedByUserId NVARCHAR(100) NULL,
                        IsDeleted BIT NOT NULL DEFAULT 0
                    );
                END

                IF COL_LENGTH('dbo.Matches', 'MatchDate') IS NULL
                    ALTER TABLE dbo.Matches ADD MatchDate NVARCHAR(64) NULL;
                IF COL_LENGTH('dbo.Matches', 'StartTime') IS NULL
                    ALTER TABLE dbo.Matches ADD StartTime NVARCHAR(50) NULL;

                IF OBJECT_ID('dbo.TournamentStandings', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.TournamentStandings (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        TournamentId NVARCHAR(64) NOT NULL,
                        TeamId NVARCHAR(64) NOT NULL,
                        Place INT NOT NULL DEFAULT 0,
                        Wins INT NOT NULL DEFAULT 0,
                        Losses INT NOT NULL DEFAULT 0,
                        Draws INT NOT NULL DEFAULT 0,
                        SetsWon INT NOT NULL DEFAULT 0,
                        SetsLost INT NOT NULL DEFAULT 0,
                        PointsScored INT NOT NULL DEFAULT 0,
                        PointsConceded INT NOT NULL DEFAULT 0,
                        Points INT NOT NULL DEFAULT 0
                    );
                END";

            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Если у пользователя на сервере нет прав DDL (но таблицы уже созданы), не роняем подключение
            }
        }
    }
}