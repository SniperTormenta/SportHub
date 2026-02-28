using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TeamMatchHistory
    {
        public string TeamName { get; set; }
        public string Form { get; set; } // W L L W L
    }
}
