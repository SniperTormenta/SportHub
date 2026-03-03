using System;

namespace SportHubBase.Models
{
    public class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
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
