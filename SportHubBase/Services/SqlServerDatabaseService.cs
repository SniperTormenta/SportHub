// Services/SqlServerDatabaseService.cs
using SportHubBase.Models;

namespace SportHubBase.Services
{
    public class SqlServerDatabaseService
    {
        public string GetConnectionString()
        {
            // Берем то, что пользователь ввел в поле "Адрес сервера"
            string serverAddress = DatabaseConfig.SqlConnectionString;

            // Защита от дурака: если поле пустое, по умолчанию стучимся в localhost
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                serverAddress = "localhost";
            }

            // Формируем правильную строку подключения для .NET
            // Используем Integrated Security (Windows-авторизацию). 
            // Если у твоего сервера логин/пароль (sa), строку нужно будет изменить на:
            // return $"Server={serverAddress};Database=SportHub;User Id=ТВОЙ_ЛОГИН;Password=ТВОЙ_ПАРОЛЬ;TrustServerCertificate=True;";

            return $"Server={serverAddress};Database=SportHub;Integrated Security=True;TrustServerCertificate=True;";
        }
    }
}