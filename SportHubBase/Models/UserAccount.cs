using System;

namespace SportHubBase.Models
{
    public class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string City { get; set; }
        public string AvatarPath { get; set; }
        public string Role { get; set; } = "User";
    }

    /// <summary>
    /// Статический класс для хранения данных о текущей сессии пользователя.
    /// Устанавливается в AuthViewModel после успешного логина.
    /// </summary>
    public static class CurrentSession
    {
        public static UserAccount CurrentUser { get; set; }
        public static string Username => CurrentUser?.Username ?? string.Empty;
        public static void Clear() { CurrentUser = null; }
    }
}
