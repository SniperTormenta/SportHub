// Models/Invite.cs
using System;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    public class Invite
    {
        [JsonProperty("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("tournamentId")]
        public Guid TournamentId { get; set; }

        [JsonProperty("inviterId")]
        public Guid InviterId { get; set; }

        [JsonProperty("isUsed")]
        public bool IsUsed { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
