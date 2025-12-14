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
        public string Status { get; set; } // "Активный" / "Неактивный"

        [JsonProperty("sportType")]
        public string SportType { get; set; } // "Футбол" и т.д.

        [JsonProperty("type")]
        public string Type { get; set; } // "Круговой" и т.д.

        [JsonProperty("editMode")]
        public string EditMode { get; set; } // "Только администраторы"

        [JsonProperty("accessMode")]
        public string AccessMode { get; set; } // "Доступно всем"

        [JsonProperty("noScore")]
        public bool NoScore { get; set; } // Без счета

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("contacts")]
        public string Contacts { get; set; }

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("teams")]
        public List<Team> Teams { get; set; } = new List<Team>();

        // Для статуса Live (динамический)
        [JsonIgnore] // Не сохраняем в JSON, вычисляем
        public bool IsLive => DateTime.Now >= StartDate; // Пример логики
    }
}