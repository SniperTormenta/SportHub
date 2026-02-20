// Models/BracketRound.cs
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    /// <summary>
    /// Модель раунда (столбца) в олимпийской сетке.
    /// </summary>
    public class BracketRound
    {
        /// <summary>
        /// Название раунда (например, "1/4 финала").
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Индекс раунда.
        /// </summary>
        [JsonProperty("roundIndex")]
        public int RoundIndex { get; set; }

        /// <summary>
        /// Список матчей в данном раунде. 
        /// Используется ObservableCollection для автоматического обновления UI в WPF.
        /// </summary>
        [JsonProperty("matches")]
        public ObservableCollection<BracketMatch> Matches { get; set; } = new ObservableCollection<BracketMatch>();
    }
}
