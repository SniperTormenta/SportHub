using ClosedXML.Excel;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SportHubBase.Services.Export
{
    public class ExcelService : IExcelService
    {
        public void ExportResults(string tournamentName, IEnumerable<string> headers, IEnumerable<ResultRow> results, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Результаты");

                // Заголовок турнира
                worksheet.Cell(1, 1).Value = tournamentName;
                var titleRange = worksheet.Range(1, 1, 1, headers.Count());
                titleRange.Merge().Style.Font.Bold = true;
                titleRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                // Заголовки таблицы
                int col = 1;
                foreach (var header in headers)
                {
                    worksheet.Cell(2, col).Value = header;
                    worksheet.Cell(2, col).Style.Font.Bold = true;
                    col++;
                }

                // Данные
                int rowIdx = 3;
                foreach (var row in results)
                {
                    worksheet.Cell(rowIdx, 1).Value = row.Place;
                    worksheet.Cell(rowIdx, 2).Value = row.TeamName;
                    worksheet.Cell(rowIdx, 3).Value = row.Wins;
                    worksheet.Cell(rowIdx, 4).Value = row.Losses;
                    worksheet.Cell(rowIdx, 5).Value = row.SetsWon;
                    worksheet.Cell(rowIdx, 6).Value = row.SetsLost;
                    worksheet.Cell(rowIdx, 7).Value = row.Points;
                    rowIdx++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

        public void ExportTeams(IEnumerable<Team> teams, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                // Лист 1: Teams
                var teamsSheet = workbook.Worksheets.Add("Teams");
                teamsSheet.Cell(1, 1).Value = "Название команды";
                teamsSheet.Cell(1, 2).Value = "Капитан";
                
                // Стили для заголовков
                teamsSheet.Range(1, 1, 1, 2).Style.Font.Bold = true;

                int teamRowIdx = 2;
                // Лист 2: Players
                var playersSheet = workbook.Worksheets.Add("Players");
                playersSheet.Cell(1, 1).Value = "Название команды";
                playersSheet.Cell(1, 2).Value = "Имя игрока";

                // Стили для заголовков
                playersSheet.Range(1, 1, 1, 2).Style.Font.Bold = true;

                int playerRowIdx = 2;

                foreach (var team in teams)
                {
                    // Запись команды
                    teamsSheet.Cell(teamRowIdx, 1).Value = team.Name;
                    teamsSheet.Cell(teamRowIdx, 2).Value = team.Captain ?? "";
                    teamRowIdx++;

                    // Запись игроков команды (группировка по команде гарантирована порядком обхода)
                    if (team.Players != null)
                    {
                        foreach (var player in team.Players)
                        {
                            playersSheet.Cell(playerRowIdx, 1).Value = team.Name;
                            playersSheet.Cell(playerRowIdx, 2).Value = player.Name;
                            playerRowIdx++;
                        }
                    }
                }

                teamsSheet.Columns().AdjustToContents();
                playersSheet.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
            }
        }

        public void SaveTemplate(string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var teamsSheet = workbook.Worksheets.Add("Teams");
                teamsSheet.Cell(1, 1).Value = "Название команды";
                teamsSheet.Cell(1, 2).Value = "Капитан";
                teamsSheet.Cell(2, 1).Value = "Команда А";
                teamsSheet.Cell(2, 2).Value = "Иванов И.И.";
                teamsSheet.Columns().AdjustToContents();

                var playersSheet = workbook.Worksheets.Add("Players");
                playersSheet.Cell(1, 1).Value = "Название команды";
                playersSheet.Cell(1, 2).Value = "Имя игрока";
                playersSheet.Cell(2, 1).Value = "Команда А";
                playersSheet.Cell(2, 2).Value = "Петров П.П.";
                playersSheet.Cell(3, 1).Value = "Команда А";
                playersSheet.Cell(3, 2).Value = "Сидоров С.С.";
                playersSheet.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
            }
        }

        public List<Team> ImportTeams(string filePath)
        {
            var teams = new List<Team>();

            using (var workbook = new XLWorkbook(filePath))
            {
                // Читаем лист Teams
                IXLWorksheet teamsSheet;
                if (!workbook.TryGetWorksheet("Teams", out teamsSheet))
                {
                    // Fallback если лист называется иначе, пробуем первый лист
                    teamsSheet = workbook.Worksheet(1);
                }

                var teamRows = teamsSheet.RangeUsed().RowsUsed().Skip(1); // Пропускаем заголовок

                foreach (var row in teamRows)
                {
                    var teamName = row.Cell(1).GetValue<string>();
                    if (string.IsNullOrWhiteSpace(teamName)) continue;

                    var captain = row.Cell(2).GetValue<string>();

                    var team = new Team
                    {
                        Id = Guid.NewGuid(),
                        Name = teamName.Trim(),
                        Captain = captain?.Trim(),
                        City = "", // В новом формате города нет, оставляем пустым
                        Players = new List<Player>()
                    };
                    teams.Add(team);
                }

                // Читаем лист Players и матчим с командами
                IXLWorksheet playersSheet;
                if (workbook.TryGetWorksheet("Players", out playersSheet))
                {
                    var playerRows = playersSheet.RangeUsed().RowsUsed().Skip(1);

                    foreach (var row in playerRows)
                    {
                        var teamName = row.Cell(1).GetValue<string>();
                        var playerName = row.Cell(2).GetValue<string>();

                        if (string.IsNullOrWhiteSpace(teamName) || string.IsNullOrWhiteSpace(playerName)) continue;

                        var team = teams.FirstOrDefault(t => t.Name.Equals(teamName.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (team != null)
                        {
                            team.Players.Add(new Player { Name = playerName.Trim() });
                        }
                    }
                }
            }

            return teams;
        }
        public void SavePlayersTemplate(string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var playersSheet = workbook.Worksheets.Add("Players");
                playersSheet.Cell(1, 1).Value = "ФИО (например: Иванов Иван Иванович)";
                playersSheet.Cell(1, 1).Style.Font.Bold = true;
                playersSheet.Cell(2, 1).Value = "Иванов Иван Иванович";
                playersSheet.Cell(3, 1).Value = "Петров Пётр";
                playersSheet.Cell(4, 1).Value = "Смирнов";
                playersSheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

        public List<Player> ImportPlayers(string filePath)
        {
            var players = new List<Player>();
            using (var workbook = new XLWorkbook(filePath))
            {
                IXLWorksheet sheet;
                if (!workbook.TryGetWorksheet("Players", out sheet))
                    sheet = workbook.Worksheet(1); // Fallback to first sheet

                var rows = sheet.RangeUsed().RowsUsed().Skip(1);
                foreach (var row in rows)
                {
                    var fullName = row.Cell(1).GetValue<string>();
                    if (string.IsNullOrWhiteSpace(fullName)) continue;

                    fullName = fullName.Trim();
                    var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    string nickName = parts.Length > 0 ? parts[0] : "";

                    players.Add(new Player 
                    { 
                        Name = fullName,
                        Nickname = nickName
                    });
                }
            }
            return players;
        }
    }
}
