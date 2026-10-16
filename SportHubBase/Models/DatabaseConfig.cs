// Models/DatabaseConfig.cs
namespace SportHubBase.Models
{
    public static class DatabaseConfig
    {
        // Флажок: используем внешний сервер?
        public static bool UseSqlServer { get; set; } = false;

        // Строка подключения
        public static string SqlConnectionString { get; set; } = "";

        // Последний использованный адрес SQL Server по умолчанию
        public static string SqlServerAddress { get; set; } = "localhost";
    }
}