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

        [JsonProperty("totalRounds")]
        public int TotalRounds { get; set; } = 0; // Для Швейцарской системы

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

        [JsonProperty("tieBreakerRule")]
        public string TieBreakerRule { get; set; } // "Standard", "GoalDiff", "HeadToHead", etc.

        [JsonProperty("isRemote")]
        public bool IsRemote { get; set; }

        [JsonProperty("serverAddress")]
        public string ServerAddress { get; set; }

        [JsonProperty("organizerLogin")]
        public string OrganizerLogin { get; set; }

        [JsonProperty("organizerPasswordHash")]
        public string OrganizerPasswordHash { get; set; }

        [JsonProperty("isOrganizerRemembered")]
        public bool IsOrganizerRemembered { get; set; }

        /// <summary>
        /// Флаг рантайма: турнир является удалённым, но сервер сейчас недоступен и данные загружены из локального оффлайн-кэша.
        /// </summary>
        [JsonIgnore]
        public bool IsOfflineCache { get; set; }

        [JsonIgnore]
        public string StorageBadgeText
        {
            get
            {
                if (!IsRemote) return "💻 Локальный";
                if (IsOfflineCache) return "⚡ Оффлайн-кэш";
                return "🌐 Сервер";
            }
        }

        [JsonIgnore]
        public string StorageBadgeBackground
        {
            get
            {
                if (!IsRemote) return "#F1F5F9";
                if (IsOfflineCache) return "#FEF3C7";
                return "#E0F2FE";
            }
        }

        [JsonIgnore]
        public string StorageBadgeForeground
        {
            get
            {
                if (!IsRemote) return "#475569";
                if (IsOfflineCache) return "#D97706";
                return "#0284C7";
            }
        }

        // Live — только если сейчас между началом и концом (или конец не указан)
        [JsonIgnore]
        public bool IsLive => DateTime.Now >= StartDate && (EndDate == null || DateTime.Now <= EndDate.Value);

        private string _ownerName;

        /// <summary>
        /// Имя организатора турнира.
        /// </summary>
        [JsonIgnore]
        public string OwnerName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_ownerName)) return _ownerName;
                if (!string.IsNullOrWhiteSpace(OrganizerLogin)) return OrganizerLogin;
                return IsRemote ? "Организатор сервера" : "Локальный";
            }
            set { _ownerName = value; }
        }
    }
}