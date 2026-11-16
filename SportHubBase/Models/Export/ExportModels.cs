using Newtonsoft.Json;
using SportHubBase.Models;
using System;
using System.Collections.Generic;

namespace SportHubBase.Models.Export
{
    /// <summary>
    /// Метаданные экспорта.
    /// </summary>
    public class ExportInfo
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";

        [JsonProperty("exportedAt")]
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("application")]
        public string Application { get; set; } = "SportHub";

        [JsonProperty("format")]
        public string Format { get; set; } = "SportHub-Tournament-Export";
    }

    /// <summary>
    /// Элемент строки результатов (таблицы турнира) для экспорта на сайт.
    /// </summary>
    public class ExportStandingRow
    {
        [JsonProperty("place")]
        public int Place { get; set; }

        [JsonProperty("teamName")]
        public string TeamName { get; set; }

        [JsonProperty("teamId")]
        public Guid? TeamId { get; set; }

        [JsonProperty("matchesPlayed")]
        public int MatchesPlayed { get; set; }

        [JsonProperty("wins")]
        public int Wins { get; set; }

        [JsonProperty("losses")]
        public int Losses { get; set; }

        [JsonProperty("draws")]
        public int Draws { get; set; }

        [JsonProperty("setsWon")]
        public int SetsWon { get; set; }

        [JsonProperty("setsLost")]
        public int SetsLost { get; set; }

        [JsonProperty("setsRatio")]
        public double SetsRatio { get; set; }

        [JsonProperty("points")]
        public double Points { get; set; }

        [JsonProperty("pointsScored")]
        public int PointsScored { get; set; }

        [JsonProperty("pointsConceded")]
        public int PointsConceded { get; set; }

        [JsonProperty("pointsRatio")]
        public double PointsRatio { get; set; }

        [JsonProperty("buchholzCut1", NullValueHandling = NullValueHandling.Ignore)]
        public double? BuchholzCut1 { get; set; }

        [JsonProperty("roundResults", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> RoundResults { get; set; }

        [JsonProperty("headToHead", NullValueHandling = NullValueHandling.Ignore)]
        public List<ExportCellResult> HeadToHead { get; set; }
    }

    /// <summary>
    /// Ячейка встречи между командами в шахматной таблице.
    /// </summary>
    public class ExportCellResult
    {
        [JsonProperty("opponentIndex")]
        public int OpponentIndex { get; set; }

        [JsonProperty("displayText")]
        public string DisplayText { get; set; }

        [JsonProperty("outcome")]
        public string Outcome { get; set; }

        [JsonProperty("isSelf")]
        public bool IsSelf { get; set; }

        [JsonProperty("homeSets")]
        public int? HomeSets { get; set; }

        [JsonProperty("awaySets")]
        public int? AwaySets { get; set; }

        [JsonProperty("isTechnicalDefeat")]
        public bool IsTechnicalDefeat { get; set; }
    }

    /// <summary>
    /// Модель статистики турнира для экспорта на сайт.
    /// </summary>
    public class ExportStatistics
    {
        [JsonProperty("sportType")]
        public string SportType { get; set; }

        [JsonProperty("totalMatches")]
        public int TotalMatches { get; set; }

        [JsonProperty("playedMatches")]
        public int PlayedMatches { get; set; }

        [JsonProperty("remainingMatches")]
        public int RemainingMatches { get; set; }

        [JsonProperty("fiveSetMatches")]
        public int FiveSetMatches { get; set; }

        [JsonProperty("mostProductiveMatch")]
        public string MostProductiveMatch { get; set; }

        [JsonProperty("technicalDefeatsCount")]
        public int TechnicalDefeatsCount { get; set; }

        [JsonProperty("avgGoals")]
        public double AvgGoals { get; set; }

        [JsonProperty("leaderName")]
        public string LeaderName { get; set; }

        [JsonProperty("leaderTeam")]
        public string LeaderTeam { get; set; }

        [JsonProperty("leaderPoints")]
        public int LeaderPoints { get; set; }

        [JsonProperty("leaderForm")]
        public string LeaderForm { get; set; }

        [JsonProperty("topMvps")]
        public List<ExportMvpItem> TopMvps { get; set; } = new List<ExportMvpItem>();

        [JsonProperty("recentMatches")]
        public List<ExportTeamMatchHistory> RecentMatches { get; set; } = new List<ExportTeamMatchHistory>();
    }

    /// <summary>
    /// Элемент рейтинга MVP для экспорта.
    /// </summary>
    public class ExportMvpItem
    {
        [JsonProperty("playerName")]
        public string PlayerName { get; set; }

        [JsonProperty("teamName")]
        public string TeamName { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// История последних матчей команды для экспорта.
    /// </summary>
    public class ExportTeamMatchHistory
    {
        [JsonProperty("teamName")]
        public string TeamName { get; set; }

        [JsonProperty("form")]
        public string Form { get; set; }
    }

    /// <summary>
    /// Краткая информация о турнире для вложенного объекта tournament.
    /// </summary>
    public class ExportTournamentSummary
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sportType")]
        public string SportType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("contacts")]
        public string Contacts { get; set; }

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonProperty("scoringSystem")]
        public string ScoringSystem { get; set; }

        [JsonProperty("teamsCount")]
        public int TeamsCount { get; set; }

        [JsonProperty("matchesCount")]
        public int MatchesCount { get; set; }
    }

    /// <summary>
    /// Полный пакет экспорта всех данных турнира в JSON.
    /// Содержит как свойства турнира на верхнем уровне, так и структурированные блоки
    /// (команды, матчи, сетка, таблица результатов, статистика, метаданные экспорта).
    /// </summary>
    public class TournamentExportModel
    {
        [JsonProperty("exportInfo")]
        public ExportInfo ExportInfo { get; set; } = new ExportInfo();

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sportType")]
        public string SportType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; }

        [JsonProperty("ownerId")]
        public string OwnerId { get; set; }

        [JsonProperty("ownerName")]
        public string OwnerName { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("contacts")]
        public string Contacts { get; set; }

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonProperty("scoringSystem")]
        public string ScoringSystem { get; set; }

        [JsonProperty("customWinPoints")]
        public int CustomWinPoints { get; set; }

        [JsonProperty("customDrawPoints")]
        public int CustomDrawPoints { get; set; }

        [JsonProperty("customLossPoints")]
        public int CustomLossPoints { get; set; }

        [JsonProperty("tieBreakerRule")]
        public string TieBreakerRule { get; set; }

        [JsonProperty("totalRounds")]
        public int TotalRounds { get; set; }

        [JsonProperty("logoUrl")]
        public string LogoUrl { get; set; }

        [JsonProperty("cardBackgroundData")]
        public string CardBackgroundData { get; set; }

        [JsonProperty("teams")]
        public List<Team> Teams { get; set; } = new List<Team>();

        [JsonProperty("matches")]
        public List<Match> Matches { get; set; } = new List<Match>();

        [JsonProperty("bracket")]
        public TournamentBracket Bracket { get; set; }

        [JsonProperty("standings")]
        public List<ExportStandingRow> Standings { get; set; } = new List<ExportStandingRow>();

        [JsonProperty("statistics")]
        public ExportStatistics Statistics { get; set; } = new ExportStatistics();

        [JsonProperty("tournament")]
        public ExportTournamentSummary TournamentSummary { get; set; }
    }
}
