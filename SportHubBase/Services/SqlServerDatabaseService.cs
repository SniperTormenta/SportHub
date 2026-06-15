// Services/SqlServerDatabaseService.cs
using System;

namespace SportHubBase.Services
{
    /// Сервис для работы с подключением к существующей внешней базе данных MS SQL Server.
    public class SqlServerDatabaseService
    {
        private readonly string _connectionString;

        public SqlServerDatabaseService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Строка подключения к MS SQL Server не может быть пустой", nameof(connectionString));

            _connectionString = connectionString;
        }

        /// Возвращает строку подключения к внешней базе данных.
        public string GetConnectionString()
        {
            return _connectionString;
        }
    }
}