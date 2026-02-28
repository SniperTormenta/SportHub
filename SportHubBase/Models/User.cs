// Models/User.cs
using System;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    public enum UserRole
    {
        User,
        Admin
    }

    public class User
    {
        [JsonProperty("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("passwordHash")]
        public string PasswordHash { get; set; }

        [JsonProperty("role")]
        public UserRole Role { get; set; } = UserRole.User;

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
