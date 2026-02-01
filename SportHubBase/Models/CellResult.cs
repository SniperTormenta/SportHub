using System;

namespace SportHubBase.Models
{
    public class CellResult
    {
        public string DisplayText { get; set; }
        public string Outcome { get; set; }
        public int? HomeSets { get; set; }
        public int? AwaySets { get; set; }

        public bool IsSelf
        {
            get { return Outcome == "SELF"; }
        }

        public bool IsPlayed
        {
            get { return Outcome != "NOT_PLAYED" && Outcome != "SELF"; }
        }

        public bool IsWin
        {
            get { return Outcome == "WIN"; }
        }

        public bool IsLoss
        {
            get { return Outcome == "LOSS"; }
        }

        public bool IsDraw
        {
            get { return Outcome == "DRAW"; }
        }

        public CellResult()
        {
            DisplayText = string.Empty;
            Outcome = "NOT_PLAYED";
        }
    }
}