// Models/Team.cs (для окна турнира)
using Newtonsoft.Json;

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
    }
}