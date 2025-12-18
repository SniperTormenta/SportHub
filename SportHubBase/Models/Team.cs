// Models/Team.cs (для окна турнира)
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SportHubBase.Models
{
    public class Team
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("captain")]
        public string Captain { get; set; }

        [JsonProperty("logoUrl")]
        public string LogoUrl { get; set; }

        [JsonProperty("players")]
        public List<Player> Players { get; set; } = new List<Player>();
    }
}