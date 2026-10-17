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

        [JsonProperty("initialSeed")]
        public int InitialSeed { get; set; }

        [JsonProperty("captain")]
        public string Captain { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("logoUrl")]
        public string LogoUrl { get; set; }

        [JsonProperty("players")]
        public List<Player> Players { get; set; } = new List<Player>();

        [JsonProperty("ownerUserId")]
        public Guid? OwnerUserId { get; set; }

        [JsonProperty("memberUserIds")]
        public List<Guid> MemberUserIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Возвращает true, если это "техническая" команда для пропуска раунда (BYE).
        /// </summary>
        [JsonIgnore]
        public bool IsBye => string.IsNullOrEmpty(Name) || Name.Equals("BYE", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Возвращает true, если у команды загружен логотип.
        /// </summary>
        [JsonIgnore]
        public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl);

        /// <summary>
        /// Первая буква названия команды для заглушки, если нет логотипа.
        /// </summary>
        [JsonIgnore]
        public string Initial => !string.IsNullOrWhiteSpace(Name) ? Name.Trim().Substring(0, 1).ToUpperInvariant() : "T";

        public override string ToString() => Name;
    }
}