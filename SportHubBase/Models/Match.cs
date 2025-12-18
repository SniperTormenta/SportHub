// Models/Match.cs
using System;

namespace SportHubBase.Models
{
    /// <summary>
    /// Простая модель матча для расписания.
    /// Пока что храним только названия команд и номер тура.
    /// </summary>
    public class Match
    {
        public int Round { get; set; }

        public string Team1 { get; set; }

        public string Team2 { get; set; }
    }
}


