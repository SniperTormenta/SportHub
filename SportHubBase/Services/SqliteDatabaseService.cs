// Services/SqliteDatabaseService.cs
using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace SportHubBase.Services
{
    /// <summary>
    /// Центральный сервис инициализации SQLite-базы данных.
    /// Создаёт все таблицы при первом запуске.
    /// Инжектируется в AccountService и SqliteStorageService.
    /// </summary>
    public class SqliteDatabaseService
    {
        private readonly string _connectionString;

        public SqliteDatabaseService()
        {
            string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = Path.Combine(baseFolder, "sporthub.db");
            _connectionString = string.Format("Data Source={0}", dbPath);

            InitializeDatabase();
        }

        /// <summary>
        /// Возвращает строку подключения к базе данных.
        /// </summary>
        public string GetConnectionString()
        {
            return _connectionString;
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Включаем внешние ключи
                ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");

                // -------------------------------------------------------
                // Таблица аккаунтов (для авторизации)
                // -------------------------------------------------------
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Accounts (
                        Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username    TEXT    NOT NULL UNIQUE,
                        PasswordHash TEXT   NOT NULL,
                        Email       TEXT,
                        Role        TEXT    NOT NULL DEFAULT 'User',
                        CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now'))
                    );");

                // -------------------------------------------------------
                // Таблица пользователей (полная модель User)
                // -------------------------------------------------------
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id          TEXT    PRIMARY KEY,
                        Username    TEXT    NOT NULL UNIQUE,
                        PasswordHash TEXT   NOT NULL,
                        Email       TEXT,
                        Role        TEXT    NOT NULL DEFAULT 'User',
                        CreatedAt   TEXT    NOT NULL
                    );");

                // -------------------------------------------------------
                // Таблица турниров
                // -------------------------------------------------------
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Tournaments (
                        Id              TEXT    PRIMARY KEY,
                        Name            TEXT    NOT NULL,
                        SportType       TEXT,
                        Type            TEXT,
                        Status          TEXT    NOT NULL DEFAULT 'Не начат',
                        EditMode        TEXT,
                        AccessMode      TEXT,
                        IsPublic        INTEGER NOT NULL DEFAULT 1,
                        NoScore         INTEGER NOT NULL DEFAULT 0,
                        Description     TEXT,
                        City            TEXT,
                        Contacts        TEXT,
                        LogoUrl         TEXT,
                        InviteCode      TEXT,
                        StartDate       TEXT    NOT NULL,
                        EndDate         TEXT,
                        OwnerId         TEXT,
                        AllowedUserIds  TEXT,
                        ScoringSystem   TEXT,
                        CustomWinPoints  INTEGER NOT NULL DEFAULT 3,
                        CustomDrawPoints INTEGER NOT NULL DEFAULT 1,
                        CustomLossPoints INTEGER NOT NULL DEFAULT 0,
                        BracketJson     TEXT
                    );");

                // -------------------------------------------------------
                // Таблица команд
                // -------------------------------------------------------
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Teams (
                        Id              TEXT    PRIMARY KEY,
                        TournamentId    TEXT    NOT NULL REFERENCES Tournaments(Id) ON DELETE CASCADE,
                        Name            TEXT    NOT NULL,
                        Captain         TEXT,
                        City            TEXT,
                        LogoUrl         TEXT,
                        OwnerUserId     TEXT,
                        MemberUserIds   TEXT
                    );");

                // -------------------------------------------------------
                // Таблица игроков
                // -------------------------------------------------------
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Players (
                        Id          TEXT    PRIMARY KEY,
                        TeamId      TEXT    NOT NULL REFERENCES Teams(Id) ON DELETE CASCADE,
                        Name        TEXT    NOT NULL,
                        Role        TEXT    NOT NULL DEFAULT 'Обычный игрок',
                        IsCaptain   INTEGER NOT NULL DEFAULT 0,
                        UserId      TEXT
                    );");

                // -------------------------------------------------------
                // Таблица матчей
                // -------------------------------------------------------
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Matches (
                        Id                  TEXT    PRIMARY KEY,
                        TournamentId        TEXT    NOT NULL REFERENCES Tournaments(Id) ON DELETE CASCADE,
                        Round               INTEGER NOT NULL DEFAULT 0,
                        RoundName           TEXT,
                        MatchNumber         INTEGER,
                        Team1               TEXT,
                        Team2               TEXT,
                        Status              TEXT    NOT NULL DEFAULT 'Не сыгран',
                        Team1QuickScore     TEXT,
                        Team2QuickScore     TEXT,
                        SetsScore           TEXT,
                        SetsBySet           TEXT,
                        TotalScore          TEXT,
                        Duration            TEXT,
                        Referee             TEXT,
                        Location            TEXT,
                        Mvp                 TEXT,
                        WinnerId            TEXT,
                        CreatedByUserId     TEXT
                    );");

                // -------------------------------------------------------
                // Таблица сетов матча (детальный счёт по партиям)
                // -------------------------------------------------------
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS MatchSets (
                        Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                        MatchId     TEXT    NOT NULL REFERENCES Matches(Id) ON DELETE CASCADE,
                        SetNumber   INTEGER NOT NULL,
                        Score1      INTEGER NOT NULL DEFAULT 0,
                        Score2      INTEGER NOT NULL DEFAULT 0
                    );");

                // -------------------------------------------------------
                // Таблица турнирных стендингов (кэш таблицы результатов)
                // -------------------------------------------------------
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS TournamentStandings (
                        Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                        TournamentId    TEXT    NOT NULL REFERENCES Tournaments(Id) ON DELETE CASCADE,
                        TeamId          TEXT    NOT NULL REFERENCES Teams(Id) ON DELETE CASCADE,
                        Place           INTEGER NOT NULL DEFAULT 0,
                        Wins            INTEGER NOT NULL DEFAULT 0,
                        Losses          INTEGER NOT NULL DEFAULT 0,
                        Draws           INTEGER NOT NULL DEFAULT 0,
                        SetsWon         INTEGER NOT NULL DEFAULT 0,
                        SetsLost        INTEGER NOT NULL DEFAULT 0,
                        PointsScored    INTEGER NOT NULL DEFAULT 0,
                        PointsConceded  INTEGER NOT NULL DEFAULT 0,
                        Points          INTEGER NOT NULL DEFAULT 0,
                        UNIQUE(TournamentId, TeamId)
                    );");

                // Индексы для ускорения запросов
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Teams_TournamentId ON Teams(TournamentId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Players_TeamId ON Players(TeamId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Matches_TournamentId ON Matches(TournamentId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_MatchSets_MatchId ON MatchSets(MatchId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Standings_TournamentId ON TournamentStandings(TournamentId);");
            }
        }

        private static void ExecuteNonQuery(SqliteConnection connection, string sql)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
