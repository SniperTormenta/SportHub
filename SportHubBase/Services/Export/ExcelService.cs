using SportHubBase.Interfaces;
using SportHubBase.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SportHubBase.Services.Export
{
    public class ExcelService : IExcelService
    {
        private const string Separator = ";";

        public void ExportResults(string tournamentName, IEnumerable<string> headers, IEnumerable<ResultRow> results, string filePath)
        {
            var sb = new StringBuilder();
            
            // Заголовок турнира
            sb.AppendLine(tournamentName);
            sb.AppendLine();

            // Заголовки таблицы
            sb.AppendLine(string.Join(Separator, headers));

            // Данные
            foreach (var row in results)
            {
                var line = new List<string>
                {
                    row.Place.ToString(),
                    row.TeamName,
                    // Для шахматной таблицы в CSV сложно отобразить ячейки красиво, 
                    // поэтому просто выведем основные показатели
                    row.Wins.ToString(),
                    row.Losses.ToString(),
                    row.SetsWon.ToString(),
                    row.SetsLost.ToString(),
                    row.Points.ToString()
                };
                sb.AppendLine(string.Join(Separator, line));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public void ExportTeams(IEnumerable<Team> teams, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Название команды" + Separator + "Капитан" + Separator + "Город");

            foreach (var team in teams)
            {
                sb.AppendLine(string.Format("{0}{1}{2}{3}{4}", 
                    Escape(team.Name), Separator, 
                    Escape(team.Captain), Separator, 
                    Escape(team.City)));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public void SaveTemplate(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Название команды" + Separator + "Капитан" + Separator + "Город");
            sb.AppendLine("Пример Команды 1" + Separator + "Иванов И." + Separator + "Москва");
            sb.AppendLine("Пример Команды 2" + Separator + "Петров П." + Separator + "СПб");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public List<Team> ImportTeams(string filePath)
        {
            var teams = new List<Team>();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            if (lines.Length <= 1) return teams;

            // Пропускаем заголовок
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(new[] { Separator }, StringSplitOptions.None);
                if (parts.Length >= 1)
                {
                    var team = new Team
                    {
                        Id = Guid.NewGuid(),
                        Name = Unescape(parts[0]),
                        Captain = parts.Length > 1 ? Unescape(parts[1]) : "",
                        City = parts.Length > 2 ? Unescape(parts[2]) : "",
                        Players = new List<Player>()
                    };
                    
                    if (!string.IsNullOrWhiteSpace(team.Name))
                        teams.Add(team);
                }
            }

            return teams;
        }

        private string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Contains(Separator) || text.Contains("\"") || text.Contains("\n"))
            {
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }
            return text;
        }

        private string Unescape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Trim();
            if (text.StartsWith("\"") && text.EndsWith("\""))
            {
                text = text.Substring(1, text.Length - 2);
                text = text.Replace("\"\"", "\"");
            }
            return text;
        }
    }
}
