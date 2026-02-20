// Models/Team.cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SportHubBase.Models
{
    /// <summary>
    /// Модель команды.
    /// </summary>
    public class Team
    {
        [JsonProperty("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("captain")]
        public string Captain { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("logoUrl")]
        public string LogoUrl { get; set; }

        [JsonProperty("players")]
        public List<Player> Players { get; set; } = new List<Player>();

        /// <summary>
        /// Возвращает true, если это "техническая" команда для пропуска раунда (BYE).
        /// </summary>
        [JsonIgnore]
        public bool IsBye => string.IsNullOrEmpty(Name) || Name.Equals("BYE", StringComparison.OrdinalIgnoreCase);

        public override string ToString() => Name;
    }
}