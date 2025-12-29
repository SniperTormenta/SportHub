// Services/Scheduling/RoundRobinBergerScheduleStrategy.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling
{
    /// Реализация кругового турнира по алгоритму Бергера.
    /// Генерирует матчи с ротацией: фиксирует первую команду, сдвигает остальные.
    /// Обработка нечётного количества команд через "выходной" слот (не создаёт матч).
    /// В архитектуре: Полноценная стратегия для Tournament.Type = "Круговой"; возвращает Match с Team1/Team2 как строки (Team.Name) для простоты.
    /// Улучшение: Добавить даты матчей (на основе Tournament.StartDate); рандомизацию порядка; валидацию команд.
    public class RoundRobinBergerScheduleStrategy : IScheduleStrategy
    {
        /// Имя стратегии.
        public string Name => "Круговой (Бергера)";

        /// Флаг реализации (true).
        public bool IsImplemented => true;

        /// Генерирует расписание: туры с парами команд.
        public IEnumerable<Match> GenerateSchedule(IList<Team> teams)
        {
            if (teams == null)
                throw new ArgumentNullException(nameof(teams));

            // Берём только названия команд, порядок как в турнире
            // Имя параметра лямбды не должно пересекаться с локальной переменной "n" ниже
            var teamNames = teams
                .Select(t => t.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            if (teamNames.Count < 2)
            {
                return Enumerable.Empty<Match>();
            }

            // Если нечётное количество — добавляем "выходной" слот
            bool hasBye = false;
            const string byeName = "Выходной";

            if (teamNames.Count % 2 != 0)
            {
                teamNames.Add(byeName);
                hasBye = true;
            }

            int n = teamNames.Count;
            int rounds = n - 1; // по Бергеру

            var rotation = new List<string>(teamNames);
            var matches = new List<Match>();

            for (int round = 1; round <= rounds; round++)
            {
                for (int i = 0; i < n / 2; i++)
                {
                    string home = rotation[i];
                    string away = rotation[n - 1 - i];

                    // Если кто-то попал на выходной — матч не создаём
                    if (hasBye && (home == byeName || away == byeName))
                        continue;

                    matches.Add(new Match
                    {
                        Round = round,
                        Team1 = home,
                        Team2 = away
                    });
                }

                // Ротация по алгоритму Бергера:
                // первый элемент фиксируется, остальные циклически сдвигаются
                var last = rotation[n - 1];
                for (int i = n - 1; i > 1; i--)
                {
                    rotation[i] = rotation[i - 1];
                }
                rotation[1] = last;
            }

            return matches;
        }
    }
}