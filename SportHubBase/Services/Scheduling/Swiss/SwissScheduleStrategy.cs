using System;
using System.Collections.Generic;
using System.Linq;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling.Swiss
{
    public class SwissScheduleStrategy : ISequentialScheduleStrategy
    {
        public string Name => "Швейцарская система";
        public bool IsImplemented => true;

        private readonly ITiebreakerCalculator _tiebreakerCalculator;
        private readonly IPlayedMatchesService _playedMatchesService;

        public SwissScheduleStrategy(ITiebreakerCalculator tiebreakerCalculator, IPlayedMatchesService playedMatchesService)
        {
            _tiebreakerCalculator = tiebreakerCalculator;
            _playedMatchesService = playedMatchesService;
        }

        /// <summary>
        /// IScheduleStrategy legacy - просто генерирует первый тур
        /// </summary>
        public IEnumerable<Match> GenerateSchedule(IList<Team> teams)
        {
            var fakeTournament = new Tournament { Teams = new List<Team>(teams), Matches = new List<Match>() };
            return GenerateNextRound(fakeTournament, 1);
        }

        public TournamentBracket GenerateBracket(IList<Team> teams)
        {
            throw new NotSupportedException("Швейцарская система не поддерживает олимпийскую сетку.");
        }

        public IEnumerable<Match> GenerateNextRound(Tournament tournament, int roundNumber)
        {
            var result = new List<Match>();
            var teams = tournament.Teams.Where(t => !t.IsBye).ToList();
            
            Console.WriteLine($"[Swiss] Generating round {roundNumber} for {teams.Count} teams.");

            if (teams.Count < 2)
            {
                Console.WriteLine("[Swiss] Not enough teams to pair.");
                return result;
            }

            // Используем только матчи, сыгранные до генерируемого тура
            var history = (tournament.Matches ?? new List<Match>())
                .Where(m => m.Round < roundNumber)
                .ToList();

            Console.WriteLine($"[Swiss] History size: {history.Count} matches.");

            // 1. Собираем историю игр и Bye
            var playedGraph = _playedMatchesService.BuildPlayedOpponentsGraph(teams, history);
            var byeList = _playedMatchesService.BuildByeCountsList(teams, history);

            // 2. Выдаем Bye при нечетном числе участников
            SwissTeamProgress byeTeam = null;
            if (teams.Count % 2 != 0)
            {
                var sortedForBye = _tiebreakerCalculator.CalculateAndSort(tournament, teams, playedGraph, byeList);
                
                // Ищем команду с конца (самую слабую), у которой еще не было Bye
                for (int i = sortedForBye.Count - 1; i >= 0; i--)
                {
                    if (sortedForBye[i].ByeCount == 0)
                    {
                        byeTeam = sortedForBye[i];
                        break;
                    }
                }

                // Если у всех уже был Bye (уникальный случай), даем самому слабому
                if (byeTeam == null) byeTeam = sortedForBye.Last();

                Console.WriteLine($"[Swiss] Bye assigned to: {byeTeam.Team.Name}");

                result.Add(new Match
                {
                    Round = roundNumber,
                    RoundName = $"Тур {roundNumber}",
                    Team1 = byeTeam.Team.Name,
                    Team2 = "BYE",
                    IsBye = true,
                    WinnerId = byeTeam.Team.Id,
                    Status = "Сыгран",
                    Team1QuickScore = "1",
                    Team2QuickScore = "0"
                });
            }

            // Исключаем команду с Bye из жеребьевки пар
            var teamsToPair = teams.Where(t => byeTeam == null || t.Id != byeTeam.Team.Id).ToList();

            // 3. Жеребьевка
            if (roundNumber == 1)
            {
                Console.WriteLine("[Swiss] Round 1 pairing (Initial Seed).");
                // Первый тур: по InitialSeed (1-2, 3-4...), либо рандом при (Seeds == 0)
                if (teamsToPair.All(t => t.InitialSeed == 0))
                {
                    // Random shuffle
                    var rnd = new Random();
                    teamsToPair = teamsToPair.OrderBy(x => rnd.Next()).ToList();
                }
                else
                {
                    // Seed (чем меньше, тем сильнее), пустые (0) кидаем в конец
                    teamsToPair = teamsToPair.OrderBy(t => t.InitialSeed > 0 ? t.InitialSeed : int.MaxValue).ToList();
                }

                for (int i = 0; i < teamsToPair.Count / 2; i++)
                {
                    var t1 = teamsToPair[i];
                    var t2 = teamsToPair[i + teamsToPair.Count / 2];
                    result.Add(new Match
                    {
                        Round = roundNumber,
                        RoundName = $"Тур {roundNumber}",
                        Team1 = t1.Name,
                        Team2 = t2.Name,
                        Team1Color = "White",
                        Team2Color = "Black"
                    });
                }
            }
            else
            {
                Console.WriteLine($"[Swiss] Round {roundNumber} pairing (Points).");
                // Последующие туры: жадное парование по Score Groups
                var sortedProgress = _tiebreakerCalculator.CalculateAndSort(tournament, teamsToPair, playedGraph, byeList);
                var pairedTeams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Разбиваем на очки (score groups)
                var scoreGroups = sortedProgress.GroupBy(p => p.Points).OrderByDescending(g => g.Key).ToList();
                var floatedProgression = new List<SwissTeamProgress>();

                foreach (var group in scoreGroups)
                {
                    var currentGroup = group.Where(p => !pairedTeams.Contains(p.Name)).ToList();
                    currentGroup.InsertRange(0, floatedProgression); // Добавляем тех, кто спустился (downfloat)
                    floatedProgression.Clear();

                    while (currentGroup.Count > 1)
                    {
                        var team1 = currentGroup[0];
                        bool paired = false;

                        // Пытаемся найти лучшую пару (сначала верхняя половина играет с нижней, классика Swiss, 
                        // но для простоты алгоритма и избежания бесконечных циклов жадно ищем первого подходящего со второй половины)
                        int halfIndex = currentGroup.Count / 2;
                        
                        for (int i = halfIndex; i < currentGroup.Count; i++)
                        {
                            var opp = currentGroup[i];
                            if (!playedGraph[team1.Name].Contains(opp.Name))
                            {
                                // Найдена пара
                                result.Add(CreateMatch(roundNumber, team1, opp));
                                pairedTeams.Add(team1.Name);
                                pairedTeams.Add(opp.Name);
                                currentGroup.Remove(team1);
                                currentGroup.Remove(opp);
                                paired = true;
                                break;
                            }
                        }

                        // Если не нашли во второй половине, ищем любого, с кем не играли (fallback)
                        if (!paired)
                        {
                            for (int i = 1; i < currentGroup.Count; i++)
                            {
                                var opp = currentGroup[i];
                                if (!playedGraph[team1.Name].Contains(opp.Name))
                                {
                                    result.Add(CreateMatch(roundNumber, team1, opp));
                                    pairedTeams.Add(team1.Name);
                                    pairedTeams.Add(opp.Name);
                                    currentGroup.Remove(team1);
                                    currentGroup.Remove(opp);
                                    paired = true;
                                    break;
                                }
                            }
                        }

                        // Если и так не вышло (тупик), допускаем повторную встречу или опускаем дальше
                        // В классике мы бы делали backtracking (отменяли пару выше). 
                        // Жадный алгоритм: переносим team1 в downfloat
                        if (!paired)
                        {
                            floatedProgression.Add(team1);
                            currentGroup.Remove(team1);
                        }
                    }

                    // Оставшийся без пары (нечетное число или не спаровался) падает вниз
                    if (currentGroup.Count == 1)
                    {
                        floatedProgression.Add(currentGroup[0]);
                    }
                }

                // Force pairing for any remainders (extreme fallback to prevent infinity)
                if (floatedProgression.Count > 0)
                {
                    Console.WriteLine($"[Swiss] Floating teams: {floatedProgression.Count}");
                    while (floatedProgression.Count > 1)
                    {
                        // Fallback: даже если играли, паруем (нарушение, но избавляет от краша). Ищем первого.
                        var team1 = floatedProgression[0];
                        bool paired = false;
                        for (int i = 1; i < floatedProgression.Count; i++)
                        {
                            var opp = floatedProgression[i];
                            if (!playedGraph[team1.Name].Contains(opp.Name))
                            {
                                result.Add(CreateMatch(roundNumber, team1, opp));
                                pairedTeams.Add(team1.Name);
                                pairedTeams.Add(opp.Name);
                                floatedProgression.Remove(team1);
                                floatedProgression.Remove(opp);
                                paired = true;
                                break;
                            }
                        }
                        
                        // Абсолютный тупик - паруем с первым попавшимся
                        if (!paired)
                        {
                            var opp = floatedProgression[1];
                            Console.WriteLine($"[Swiss] Force pairing repeat: {team1.Name} vs {opp.Name}");
                            result.Add(CreateMatch(roundNumber, team1, opp)); // Компромисс: допускаем повторную встречу
                            floatedProgression.Remove(team1);
                            floatedProgression.Remove(opp);
                        }
                    }
                }
            }

            Console.WriteLine($"[Swiss] Round {roundNumber} generated: {result.Count} matches.");
            return result;
        }

        private Match CreateMatch(int roundNumber, SwissTeamProgress t1, SwissTeamProgress t2)
        {
            return new Match
            {
                Round = roundNumber,
                RoundName = $"Тур {roundNumber}",
                Team1 = t1.Name,
                Team2 = t2.Name,
                Team1Color = "White",  // TODO: Полноценное чередование цветов
                Team2Color = "Black"
            };
        }
    }
}
