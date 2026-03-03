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


        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; } = true;

        [JsonProperty("ownerId")]
        public string OwnerId { get; set; }

        [JsonProperty("allowedUserIds")]
        public List<Guid> AllowedUserIds { get; set; } = new List<Guid>();

        [JsonProperty("inviteCode")]
        public string InviteCode { get; set; }

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

        [JsonProperty("matches")]
        public List<Match> Matches { get; set; } = new List<Match>();

        [JsonProperty("bracket")]
        public TournamentBracket Bracket { get; set; }

        [JsonProperty("scoringSystem")]
        public string ScoringSystem { get; set; } // Итальянская, FIVB, Пользовательская

        [JsonProperty("customWinPoints")]
        public int CustomWinPoints { get; set; }

        [JsonProperty("customDrawPoints")]
        public int CustomDrawPoints { get; set; }

        [JsonProperty("customLossPoints")]
        public int CustomLossPoints { get; set; }

        // Live — только если сейчас между началом и концом (или конец не указан)
        [JsonIgnore]
        public bool IsLive => DateTime.Now >= StartDate && (EndDate == null || DateTime.Now <= EndDate.Value);

        /// <summary>
        /// Имя владельца турнира. Заполняется при загрузке из БД.
        /// Не сохраняется в JSON — только для отображения в UI.
        /// </summary>
        [JsonIgnore]
        public string OwnerName { get; set; }
    }
}