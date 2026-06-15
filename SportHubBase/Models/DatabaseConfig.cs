// Models/DatabaseConfig.cs
namespace SportHubBase.Models
{
    public static class DatabaseConfig
    {
        // Флажок: используем внешний сервер?
        public static bool UseSqlServer { get; set; } = false;

        // Строка подключения (пользователь введет ее в интерфейсе)
        public static string SqlConnectionString { get; set; } = "";
    }
}