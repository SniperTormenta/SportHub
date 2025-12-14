// Models/Admin.cs
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    public class Admin
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("isOwner")]
        public bool IsOwner { get; set; }
    }
}