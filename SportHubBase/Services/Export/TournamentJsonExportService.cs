using Newtonsoft.Json;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Models.Export;
using SportHubBase.Services.Results.Data;
using SportHubBase.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SportHubBase.Services.Export
{
    /// <summary>
    /// Сервис экспорта всех данных конкретного турнира в JSON-файл для последующей загрузки на сайт.
    /// </summary>
    public class TournamentJsonExportService : ITournamentJsonExportService
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            DateFormatString = "yyyy-MM-ddTHH:mm:ss"
        };

        /// <summary>
        /// Формирует структурированную DTO-модель со всеми данными турнира.
        /// </summary>
        public TournamentExportModel BuildExportModel(Tournament tournament, ResultsData resultsData, TournamentStatistics statistics)
        {
            if (tournament == null)
                throw new ArgumentNullException(nameof(tournament));

            var exportModel = new TournamentExportModel
            {
                Id = tournament.Id,
                Name = tournament.Name ?? string.Empty,
                SportType = tournament.SportType ?? string.Empty,
                Type = tournament.Type ?? string.Empty,
                Status = tournament.Status ?? string.Empty,
                IsPublic = tournament.IsPublic,
                OwnerId = tournament.OwnerId ?? string.Empty,
                OwnerName = tournament.OwnerName ?? string.Empty,
                City = tournament.City ?? string.Empty,
                Description = tournament.Description ?? string.Empty,
                Contacts = tournament.Contacts ?? string.Empty,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                ScoringSystem = tournament.ScoringSystem ?? string.Empty,
                CustomWinPoints = tournament.CustomWinPoints,
                CustomDrawPoints = tournament.CustomDrawPoints,
                CustomLossPoints = tournament.CustomLossPoints,
                TieBreakerRule = tournament.TieBreakerRule ?? string.Empty,
                TotalRounds = tournament.TotalRounds,
                LogoUrl = tournament.LogoUrl ?? string.Empty,
                CardBackgroundData = tournament.CardBackgroundData ?? string.Empty,
                Bracket = tournament.Bracket
            };

            // Команды
            if (tournament.Teams != null)
            {
                exportModel.Teams = tournament.Teams.Select(CloneTeam).ToList();
            }

            // Матчи
            if (tournament.Matches != null)
            {
                exportModel.Matches = tournament.Matches.Select(CloneMatch).ToList();
            }

            // Таблица результатов (стендинги)
            exportModel.Standings = ExtractStandings(resultsData, tournament);

            // Статистика
            exportModel.Statistics = ExtractStatistics(statistics, tournament.SportType);

            // Краткая сводка по турниру
            exportModel.TournamentSummary = new ExportTournamentSummary
            {
                Id = tournament.Id,
                Name = tournament.Name,
                SportType = tournament.SportType,
                Type = tournament.Type,
                Status = tournament.Status,
                City = tournament.City,
                Description = tournament.Description,
                Contacts = tournament.Contacts,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                ScoringSystem = tournament.ScoringSystem,
                TeamsCount = exportModel.Teams != null ? exportModel.Teams.Count : 0,
                MatchesCount = exportModel.Matches != null ? exportModel.Matches.Count : 0
            };

            return exportModel;
        }

        /// <summary>
        /// Сериализует все данные турнира в отформатированную JSON-строку.
        /// </summary>
        public string ExportToJsonString(Tournament tournament, ResultsData resultsData, TournamentStatistics statistics)
        {
            var exportModel = BuildExportModel(tournament, resultsData, statistics);
            return JsonConvert.SerializeObject(exportModel, SerializerSettings);
        }

        /// <summary>
        /// Сохраняет все данные турнира в указанный JSON-файл.
        /// </summary>
        public void ExportToFile(Tournament tournament, ResultsData resultsData, TournamentStatistics statistics, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым.", nameof(filePath));

            string json = ExportToJsonString(tournament, resultsData, statistics);

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Запись в файл в кодировке UTF-8 без BOM для максимальной совместимости с веб-сайтами
            File.WriteAllText(filePath, json, new UTF8Encoding(false));
        }

        /// <summary>
        /// Открывает стандартный диалог сохранения файла и сохраняет JSON.
        /// Возвращает путь к сохранённому файлу или null, если пользователь отменил диалог.
        /// </summary>
        public string ExportWithDialog(Tournament tournament, ResultsData resultsData, TournamentStatistics statistics)
        {
            if (tournament == null)
                throw new ArgumentNullException(nameof(tournament));

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                FileName = GenerateDefaultFileName(tournament),
                Title = "Экспорт данных турнира в JSON (для сайта)"
            };

            if (sfd.ShowDialog() == true)
            {
                ExportToFile(tournament, resultsData, statistics, sfd.FileName);
                return sfd.FileName;
            }

            return null;
        }

        private string GenerateDefaultFileName(Tournament tournament)
        {
            string tournamentName = !string.IsNullOrWhiteSpace(tournament.Name) ? tournament.Name.Trim() : "Tournament";

            // Очищаем от недопустимых символов для имени файла
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (char c in tournamentName)
            {
                if (Array.IndexOf(invalidChars, c) < 0 && c != ' ')
                {
                    sb.Append(c);
                }
                else if (c == ' ')
                {
                    sb.Append('_');
                }
            }

            string safeName = sb.ToString();
            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = "tournament";
            }

            string date = DateTime.Now.ToString("yyyy-MM-dd");
            return string.Format("{0}_{1}.json", safeName, date);
        }

        private List<ExportStandingRow> ExtractStandings(ResultsData resultsData, Tournament tournament)
        {
            var result = new List<ExportStandingRow>();

            // 1. Круговой турнир (RoundRobinResultsData)
            var roundRobin = resultsData as RoundRobinResultsData;
            if (roundRobin != null && roundRobin.Rows != null)
            {
                foreach (var row in roundRobin.Rows)
                {
                    var standingRow = new ExportStandingRow
                    {
                        Place = row.Place,
                        TeamName = row.TeamName,
                        Wins = row.Wins,
                        Losses = row.Losses,
                        Draws = row.Draws,
                        SetsWon = row.SetsWon,
                        SetsLost = row.SetsLost,
                        SetsRatio = row.SetsRatio,
                        Points = row.Points,
                        PointsScored = row.PointsScored,
                        PointsConceded = row.PointsConceded,
                        PointsRatio = row.PointsRatio,
                        MatchesPlayed = row.Wins + row.Losses,
                        HeadToHead = new List<ExportCellResult>()
                    };

                    if (tournament.Teams != null)
                    {
                        var team = tournament.Teams.FirstOrDefault(t => string.Equals(t.Name, row.TeamName, StringComparison.OrdinalIgnoreCase));
                        if (team != null)
                        {
                            standingRow.TeamId = team.Id;
                        }
                    }

                    if (row.Cells != null)
                    {
                        for (int i = 0; i < row.Cells.Count; i++)
                        {
                            var cell = row.Cells[i];
                            standingRow.HeadToHead.Add(new ExportCellResult
                            {
                                OpponentIndex = i + 1,
                                DisplayText = cell.DisplayText,
                                Outcome = cell.Outcome,
                                IsSelf = cell.IsSelf,
                                HomeSets = cell.HomeSets,
                                AwaySets = cell.AwaySets,
                                IsTechnicalDefeat = cell.IsTechnicalDefeat
                            });
                        }
                    }

                    result.Add(standingRow);
                }
                return result;
            }

            // 2. Швейцарская система (SwissResultsData)
            var swiss = resultsData as SwissResultsData;
            if (swiss != null && swiss.Rows != null)
            {
                foreach (var row in swiss.Rows)
                {
                    var standingRow = new ExportStandingRow
                    {
                        Place = row.Place,
                        TeamName = row.TeamName,
                        MatchesPlayed = row.MatchesPlayed,
                        Wins = row.Wins,
                        Losses = row.Losses,
                        Draws = row.Draws,
                        Points = row.Points,
                        BuchholzCut1 = row.BuchholzCut1,
                        RoundResults = row.RoundResults != null ? row.RoundResults.ToList() : new List<string>()
                    };

                    if (tournament.Teams != null)
                    {
                        var team = tournament.Teams.FirstOrDefault(t => string.Equals(t.Name, row.TeamName, StringComparison.OrdinalIgnoreCase));
                        if (team != null)
                        {
                            standingRow.TeamId = team.Id;
                        }
                    }

                    result.Add(standingRow);
                }
                return result;
            }

            return result;
        }

        private ExportStatistics ExtractStatistics(TournamentStatistics statistics, string defaultSportType)
        {
            var exportStats = new ExportStatistics
            {
                SportType = !string.IsNullOrWhiteSpace(statistics?.SportType) ? statistics.SportType : defaultSportType,
                TotalMatches = statistics?.TotalMatches ?? 0,
                PlayedMatches = statistics?.PlayedMatches ?? 0,
                RemainingMatches = statistics?.RemainingMatches ?? 0,
                FiveSetMatches = statistics?.FiveSetMatches ?? 0,
                MostProductiveMatch = statistics?.MostProductiveMatch ?? "—",
                TechnicalDefeatsCount = statistics?.TechnicalDefeatsCount ?? 0,
                AvgGoals = statistics?.AvgGoals ?? 0.0,
                LeaderName = statistics?.LeaderName ?? "—",
                LeaderTeam = statistics?.LeaderTeam ?? "—",
                LeaderPoints = statistics?.LeaderPoints ?? 0,
                LeaderForm = statistics?.LeaderForm ?? "—"
            };

            if (statistics?.TopMvps != null)
            {
                foreach (var mvp in statistics.TopMvps)
                {
                    exportStats.TopMvps.Add(new ExportMvpItem
                    {
                        PlayerName = mvp.PlayerName,
                        TeamName = mvp.TeamName,
                        Count = mvp.Count
                    });
                }
            }

            if (statistics?.LastMatches != null)
            {
                foreach (var match in statistics.LastMatches)
                {
                    exportStats.RecentMatches.Add(new ExportTeamMatchHistory
                    {
                        TeamName = match.TeamName,
                        Form = match.Form
                    });
                }
            }

            return exportStats;
        }

        private Team CloneTeam(Team source)
        {
            if (source == null) return null;

            var copy = new Team
            {
                Id = source.Id,
                Name = source.Name,
                InitialSeed = source.InitialSeed,
                Captain = source.Captain,
                City = source.City,
                LogoUrl = source.LogoUrl,
                OwnerUserId = source.OwnerUserId,
                MemberUserIds = source.MemberUserIds != null ? new List<Guid>(source.MemberUserIds) : new List<Guid>(),
                Players = new List<Player>()
            };

            if (source.Players != null)
            {
                foreach (var p in source.Players)
                {
                    copy.Players.Add(new Player
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Nickname = p.Nickname,
                        Role = p.Role,
                        IsCaptain = p.IsCaptain,
                        UserId = p.UserId
                    });
                }
            }

            return copy;
        }

        private Match CloneMatch(Match source)
        {
            if (source == null) return null;

            return new Match
            {
                Id = source.Id,
                MatchNumber = source.MatchNumber,
                Team1Id = source.Team1Id,
                Team2Id = source.Team2Id,
                Round = source.Round,
                RoundName = !string.IsNullOrWhiteSpace(source.RoundName) ? source.RoundName : string.Format("Тур {0}", source.Round),
                Team1 = source.Team1,
                Team2 = source.Team2,
                Status = source.Status,
                Team1QuickScore = source.Team1QuickScore,
                Team2QuickScore = source.Team2QuickScore,
                SetsScore = source.SetsScore,
                SetsBySet = source.SetsBySet,
                TotalScore = source.TotalScore,
                Duration = source.Duration,
                Referee = source.Referee,
                Location = source.Location,
                Mvp = source.Mvp,
                IsBye = source.IsBye,
                Team1Color = source.Team1Color,
                Team2Color = source.Team2Color,
                MatchDate = source.MatchDate,
                StartTime = source.StartTime,
                WinnerId = source.WinnerId,
                CreatedByUserId = source.CreatedByUserId
            };
        }
    }
}
