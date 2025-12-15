// Models/Tournament.cs
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    public class Tournament
    {
        [JsonProperty("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("admins")]
        public List<Admin> Admins { get; set; } = new List<Admin>();

        [JsonProperty("logoUrl")]
        public string LogoUrl { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("sportType")]
        public string SportType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } // Формат турнира

        [JsonProperty("editMode")]
        public string EditMode { get; set; }

        [JsonProperty("accessMode")]
        public string AccessMode { get; set; }

        [JsonProperty("noScore")]
        public bool NoScore { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("contacts")]
        public string Contacts { get; set; }

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime? EndDate { get; set; } // Может быть null — турнир идёт

        [JsonProperty("teams")]
        public List<Team> Teams { get; set; } = new List<Team>();

        // Live — только если сейчас между началом и концом (или конец не указан)
        [JsonIgnore]
        public bool IsLive => DateTime.Now >= StartDate && (EndDate == null || DateTime.Now <= EndDate.Value);
    }
}