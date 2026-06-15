using System;
using Microsoft.Data.SqlClient; // <-- Важное отличие
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.Services
{
    public class SqlServerAccountService : IAccountService
    {
        private readonly SqlServerDatabaseService _dbService;

        public SqlServerAccountService(SqlServerDatabaseService dbService)
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
                using (var connection = new SqlConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();

                    using (var checkCmd = connection.CreateCommand())
                    {
                        checkCmd.CommandText = "SELECT COUNT(1) FROM Accounts WHERE Username = @Username";
                        checkCmd.Parameters.AddWithValue("@Username", username);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            errorMessage = "Пользователь с таким именем уже существует.";
                            return false;
                        }
                    }

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

                using (var connection = new SqlConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PasswordHash FROM Accounts WHERE Username = @Username";
                        cmd.Parameters.AddWithValue("@Username", username);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) storedHash = reader["PasswordHash"]?.ToString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(storedHash) || !PasswordHasher.VerifyPassword(password, storedHash))
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

        public bool LoginAndGetAccount(string username, string password, out UserAccount account, out string errorMessage)
        {
            account = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Имя пользователя и пароль обязательны.";
                return false;
            }

            try
            {
                int foundId = 0;
                string foundUsername = null, foundFirstName = null, foundLastName = null;
                string foundEmail = null, foundPhone = null, foundAvatar = null;
                string foundRole = null, foundCity = null, storedHash = null;

                using (var connection = new SqlConnection(_dbService.GetConnectionString()))
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

                if (string.IsNullOrEmpty(storedHash) || !PasswordHasher.VerifyPassword(password, storedHash))
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
                using (var connection = new SqlConnection(_dbService.GetConnectionString()))
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
                using (var connection = new SqlConnection(_dbService.GetConnectionString()))
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

                using (var connection = new SqlConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PasswordHash FROM Accounts WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("@Id", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) storedHash = reader["PasswordHash"]?.ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(storedHash) || !PasswordHasher.VerifyPassword(oldPassword, storedHash))
                    {
                        errorMessage = "Неверный текущий пароль.";
                        return false;
                    }

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
                using (var connection = new SqlConnection(_dbService.GetConnectionString()))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM Accounts WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("@Id", userId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0) return true;

                        errorMessage = "Пользователь не найден.";
                        return false;
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