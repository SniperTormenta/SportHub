using System;
using SportHubBase.Models;

namespace SportHubBase.Services
{
    public class SqlServerDatabaseService
    {
        public string GetConnectionString()
        {
            string input = DatabaseConfig.SqlConnectionString?.Trim();

            // СЦЕНАРИЙ 1: Поле пустое -> Локальный сервер по умолчанию (Windows Auth)
            if (string.IsNullOrWhiteSpace(input))
            {
                return "Server=localhost;Database=SportHub;Integrated Security=True;TrustServerCertificate=True;";
            }

            // СЦЕНАРИЙ 2: Введена полная готовая строка
            if (input.Contains("Server=") || input.Contains("Data Source="))
            {
                return input;
            }

            // СЦЕНАРИЙ 3: Строго локальный ПК 
            // Только для себя используем беспарольную Windows-авторизацию
            if (input.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("127.0.0.1") ||
                input.Equals(".") ||
                input.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                (input.Contains("\\") && !input.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))) // Ловит .\SQLEXPRESS, но пропускает tcp:...
            {
                return string.Format("Server={0};Database=SportHub;Integrated Security=True;TrustServerCertificate=True;", input);
            }

            // СЦЕНАРИЙ 4: Любой другой адрес (192.168.0.12, tcp:201-SRV и т.д.)
            // Сюда прописывай свои учетные данные от сервера!
            string dbUser = "Seek"; // например, "sa" или твой личный логин
            string dbPass = "SSSzxcgoule";

            return string.Format("Server={0};Database=SportHub;User Id={1};Password={2};TrustServerCertificate=True;", input, dbUser, dbPass);
        }
    }
}