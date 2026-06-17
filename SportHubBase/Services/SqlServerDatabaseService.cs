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

            // СЦЕНАРИЙ 2: Пользователь ввёл полную техническую строку подключения целиком
            if (input.Contains("Server=") || input.Contains("Data Source="))
            {
                return input;
            }

            // СЦЕНАРИЙ 3: Пользователь ввёл локальный адрес вручную
            // (localhost, 127.0.0.1, точку, имя своего ПК или экземпляр типа .\SQLEXPRESS)
            // Для таких подключений мы автоматически используем Windows-авторизацию (Integrated Security)
            if (input.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("127.0.0.1") ||
                input.Equals(".") ||
                input.Contains("\\") || // Ловит конструкции вида .\SQLEXPRESS или ИМЯ_ПК\SQLEXPRESS
                input.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format("Server={0};Database=SportHub;Integrated Security=True;TrustServerCertificate=True;", input);
            }

            // СЦЕНАРИЙ 4: Введён внешний IP-адрес или удаленный домен соревнований
            // Здесь автоматически применяется серверная SQL-авторизация (логин/пароль)
            // Замени sa и ТВОЙ_ПАРОЛЬ на реальные данные твоего внешнего сервера!
            return string.Format("Server={0};Database=SportHub;User Id=sa;Password=ТВОЙ_ПАРОЛЬ;TrustServerCertificate=True;", input);
        }
    }
}