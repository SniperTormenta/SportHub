using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MvpItem
    {
        public string PlayerName { get; set; }
        public string TeamName { get; set; }
        public int Count { get; set; }
    }
}
