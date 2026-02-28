using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace SportHubBase.Services
{
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

        public string GetConnectionString()
        {
            return _connectionString;
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Accounts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL
                    )";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createTableQuery;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
