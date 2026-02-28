using System;
using Microsoft.Data.Sqlite;
using SportHubBase.Interfaces;

namespace SportHubBase.Services
{
    public class AccountService : IAccountService
    {
        private readonly SqliteDatabaseService _dbService;

        public AccountService(SqliteDatabaseService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }

        public bool Register(string username, string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Имя пользователя не может быть пустым.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Пароль не может быть пустым.";
                return false;
            }

            try
            {
                using (var connection = new SqliteConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();

                    // Проверяем, существует ли пользователь
                    using (var checkCmd = connection.CreateCommand())
                    {
                        checkCmd.CommandText = "SELECT COUNT(1) FROM Accounts WHERE Username = @Username";
                        checkCmd.Parameters.AddWithValue("@Username", username);
                        long count = (long)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            errorMessage = "Пользователь с таким именем уже существует.";
                            return false;
                        }
                    }

                    // Вставляем нового пользователя
                    string hash = PasswordHasher.HashPassword(password);
                    using (var insertCmd = connection.CreateCommand())
                    {
                        insertCmd.CommandText = "INSERT INTO Accounts (Username, PasswordHash) VALUES (@Username, @PasswordHash)";
                        insertCmd.Parameters.AddWithValue("@Username", username);
                        insertCmd.Parameters.AddWithValue("@PasswordHash", hash);
                        insertCmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = string.Format("Ошибка при регистрации: {0}", ex.Message);
                return false;
            }
        }

        public bool Login(string username, string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Имя пользователя и пароль обязательны.";
                return false;
            }

            try
            {
                string storedHash = null;

                using (var connection = new SqliteConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PasswordHash FROM Accounts WHERE Username = @Username";
                        cmd.Parameters.AddWithValue("@Username", username);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                storedHash = reader["PasswordHash"]?.ToString();
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(storedHash))
                {
                    errorMessage = "Неверное имя пользователя или пароль.";
                    return false;
                }

                bool isMatch = PasswordHasher.VerifyPassword(password, storedHash);
                if (!isMatch)
                {
                    errorMessage = "Неверное имя пользователя или пароль.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = string.Format("Ошибка при входе: {0}", ex.Message);
                return false;
            }
        }
    }
}
