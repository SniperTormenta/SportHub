using System;
using Microsoft.Data.Sqlite;
using SportHubBase.Interfaces;
using SportHubBase.Models;


namespace SportHubBase.Services
{
    public class SqliteAccountService : IAccountService // Было AccountService
    {
        private readonly SqliteDatabaseService _dbService;

        public SqliteAccountService(SqliteDatabaseService dbService) 
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }
        public bool Register(string username, string password, string email, string firstName, string phoneNumber, string city, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Имя пользователя и пароль обязательны.";
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
                        insertCmd.CommandText = "INSERT INTO Accounts (Username, PasswordHash, Email, FirstName, PhoneNumber, City) VALUES (@Username, @PasswordHash, @Email, @FirstName, @PhoneNumber, @City)";
                        insertCmd.Parameters.AddWithValue("@Username", username);
                        insertCmd.Parameters.AddWithValue("@PasswordHash", hash);
                        insertCmd.Parameters.AddWithValue("@Email", (object)email?.ToLower() ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@FirstName", (object)firstName ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@PhoneNumber", (object)phoneNumber ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@City", (object)city ?? DBNull.Value);
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

        /// <summary>
        /// Выполняет вход и при успехе возвращает заполненный UserAccount.
        /// </summary>
        public bool LoginAndGetAccount(string username, string password, out UserAccount account, out string errorMessage)
        {
            account = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Имя пользователя и пароль обязательны.";
                return false;
            }

            // 1. Получаем строку подключения
            string connectionString = _dbService.GetConnectionString();

            // 2. Если строка пустая (пользователю уже показан MessageBox), прерываем выполнение
            if (string.IsNullOrEmpty(connectionString))
            {
                errorMessage = "Адрес БД не указан.";
                return false;
            }

            try
            {
                int foundId = 0;
                string foundUsername = null;
                string foundFirstName = null;
                string foundLastName = null;
                string foundEmail = null;
                string foundPhone = null;
                string foundAvatar = null;
                string foundRole = null;
                string foundCity = null;
                string storedHash = null;

                // 3. Используем уже проверенную строку подключения
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Id, Username, PasswordHash, FirstName, LastName, Email, PhoneNumber, AvatarPath, Role, City FROM Accounts WHERE Username = @Username";
                        cmd.Parameters.AddWithValue("@Username", username);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                foundId = Convert.ToInt32(reader["Id"]);
                                foundUsername = reader["Username"]?.ToString();
                                storedHash = reader["PasswordHash"]?.ToString();
                                foundFirstName = reader["FirstName"]?.ToString();
                                foundLastName = reader["LastName"]?.ToString();
                                foundEmail = reader["Email"]?.ToString();
                                foundPhone = reader["PhoneNumber"]?.ToString();
                                foundAvatar = reader["AvatarPath"]?.ToString();
                                foundRole = reader["Role"]?.ToString();
                                foundCity = reader["City"]?.ToString();
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(storedHash))
                {
                    errorMessage = "Неверное имя пользователя или пароль.";
                    return false;
                }

                if (!PasswordHasher.VerifyPassword(password, storedHash))
                {
                    errorMessage = "Неверное имя пользователя или пароль.";
                    return false;
                }

                account = new UserAccount
                {
                    Id = foundId,
                    Username = foundUsername,
                    FirstName = foundFirstName,
                    LastName = foundLastName,
                    Email = foundEmail,
                    PhoneNumber = foundPhone,
                    City = foundCity,
                    AvatarPath = foundAvatar,
                    Role = foundRole
                };
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = string.Format("Ошибка при входе: {0}", ex.Message);
                return false;
            }
        }
        public bool UpdateAccountDetail(int userId, string columnName, string value)
        {
            try
            {
                using (var connection = new SqliteConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = string.Format("UPDATE Accounts SET {0} = @Value WHERE Id = @Id", columnName);
                        cmd.Parameters.AddWithValue("@Value", (object)value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public UserAccount GetAccount(int userId)
        {
            try
            {
                using (var connection = new SqliteConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Id, Username, FirstName, LastName, Email, PhoneNumber, AvatarPath, Role, City FROM Accounts WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("@Id", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new UserAccount
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Username = reader["Username"]?.ToString(),
                                    FirstName = reader["FirstName"]?.ToString(),
                                    LastName = reader["LastName"]?.ToString(),
                                    Email = reader["Email"]?.ToString(),
                                    PhoneNumber = reader["PhoneNumber"]?.ToString(),
                                    City = reader["City"]?.ToString(),
                                    AvatarPath = reader["AvatarPath"]?.ToString(),
                                    Role = reader["Role"]?.ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public bool ChangePassword(int userId, string oldPassword, string newPassword, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                errorMessage = "Пароли не могут быть пустыми.";
                return false;
            }

            try
            {
                string storedHash = null;

                using (var connection = new SqliteConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();

                    // Verify old password
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PasswordHash FROM Accounts WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("@Id", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                storedHash = reader["PasswordHash"]?.ToString();
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(storedHash) || !PasswordHasher.VerifyPassword(oldPassword, storedHash))
                    {
                        errorMessage = "Неверный текущий пароль.";
                        return false;
                    }

                    // Update with new password
                    string newHash = PasswordHasher.HashPassword(newPassword);
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE Accounts SET PasswordHash = @PasswordHash WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("@PasswordHash", newHash);
                        cmd.Parameters.AddWithValue("@Id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = string.Format("Ошибка при смене пароля: {0}", ex.Message);
                return false;
            }
        }

        public bool DeleteAccount(int userId, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (var connection = new SqliteConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM Accounts WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("@Id", userId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            return true;
                        }
                        else
                        {
                            errorMessage = "Пользователь не найден.";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = string.Format("Ошибка при удалении аккаунта: {0}", ex.Message);
                return false;
            }
        }
    }
}
