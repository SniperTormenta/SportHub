using System;
using System.Windows;
using SportHubBase.Models;

namespace SportHubBase.Services
{
    public class SqlServerDatabaseService
    {
        public string GetConnectionString()
        {
            try
            {
                string input = DatabaseConfig.SqlConnectionString?.Trim();

                // 1. ПРОВЕРКА НА ПУСТОТУ: Если ничего не ввели — ругаемся и прерываем метод
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("Пожалуйста, введите адрес сервера.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null; // <-- КРИТИЧЕСКИ ВАЖНО: останавливаем выполнение!
                }

                // --- Дальше мы уверены, что input не пустой, можно безопасно с ним работать ---

                // 2. СЦЕНАРИЙ: Введена полная готовая строка
                if (input.Contains("Server=") || input.Contains("Data Source="))
                {
                    return input;
                }

                // 3. СЦЕНАРИЙ: Строго локальный ПК (беспарольная Windows-авторизация)
                if (input.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("127.0.0.1") ||
                    input.Equals(".") ||
                    input.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                    (input.Contains("\\") && !input.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase)))
                {
                    return string.Format("Server={0};Database=SportHub;Integrated Security=True;TrustServerCertificate=True;", input);
                }

                // 4. СЦЕНАРИЙ: Любой другой адрес (внешний IP, домен, tcp:)
                string dbUser = "Seek";
                string dbPass = "SSSzxcgoule";

                return string.Format("Server={0};Database=SportHub;User Id={1};Password={2};TrustServerCertificate=True;", input, dbUser, dbPass);
            }
            catch (Exception ex)
            {
                // Глобальный перехват любых технических сбоев (чтобы приложение не вылетало)
                MessageBox.Show($"Произошла ошибка при обработке адреса сервера:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}