using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportHubBase.Services
{
    /// <summary>Подготовка расписания с сохранением идентичности матчей и ориентации счёта.</summary>
    public sealed class TournamentScheduleService
    {
        private readonly IStorage _storage;
        private readonly IMatchService _matches;
        public TournamentScheduleService(IStorage storage, IMatchService matches) { _storage = storage; _matches = matches; }

        public void Prepare(Tournament tournament, IScheduleStrategy strategy)
        {
            TournamentIntegrityService.Normalize(tournament);
            var previous = tournament.Matches ?? new List<Match>();
            if (!_storage.IsTournamentEditable(tournament)) return;
            List<Match> prepared;
            if (strategy is ISequentialScheduleStrategy sequential)
            {
                // Загрузка уже проведённых туров никогда не должна пересоздавать или удалять их.
                if (previous.Count > 0) return;
                prepared = sequential.GenerateNextRound(tournament, 1).ToList();
            }
            else if (tournament.Type == "Олимпийский" || tournament.Type == "Плей-офф") return;
            else
            {
                prepared = strategy.GenerateSchedule(tournament.Teams).ToList();
                foreach (var match in prepared)
                {
                    var saved = previous.FirstOrDefault(m => (m.Team1Id == match.Team1Id && m.Team2Id == match.Team2Id)
                        || (m.Team1Id == match.Team2Id && m.Team2Id == match.Team1Id));
                    if (saved != null) CopyResult(saved, match, saved.Team1Id != match.Team1Id);
                }
                if (prepared.Count == previous.Count && prepared.All(m => previous.Any(p => p.Id == m.Id && p.Round == m.Round
                    && p.Team1Id == m.Team1Id && p.Team2Id == m.Team2Id && p.MatchNumber.HasValue))) return;
            }
            _matches.AssignAutoNumbers(prepared);
            tournament.Matches = prepared;
            try { _storage.UpdateTournament(tournament); }
            catch (Exception ex) { tournament.Matches = previous; ErrorLog.Write("Подготовка расписания", ex); throw; }
        }

        public void GenerateRound(Tournament tournament, ISequentialScheduleStrategy strategy, int round)
        {
            if (!_storage.IsTournamentEditable(tournament)) throw new InvalidOperationException("Турнир доступен только для чтения.");
            var previous = tournament.Matches ?? new List<Match>();
            if (previous.Any(m => m.Round > round)) throw new InvalidOperationException("Нельзя пересоздать тур после формирования следующих туров.");
            var generated = strategy.GenerateNextRound(tournament, round).ToList();
            if (generated.Count == 0) throw new InvalidOperationException("Для генерации тура недостаточно участников.");
            var prepared = previous.Where(m => m.Round != round).Concat(generated).ToList();
            int nextNumber = previous.Max(m => m.MatchNumber) ?? 0;
            foreach (var match in generated) match.MatchNumber = ++nextNumber;
            tournament.Matches = prepared;
            try { _storage.UpdateTournament(tournament); }
            catch (Exception ex) { tournament.Matches = previous; ErrorLog.Write("Генерация тура", ex); throw; }
        }

        private static string ReversePair(string value)
        {
            var pair = ScorePair.Parse(value);
            return pair == null ? value : new ScorePair(pair.Right, pair.Left).ToString();
        }

        private static void CopyResult(Match source, Match target, bool reverse)
        {
            target.Id = source.Id;
            target.Status = source.Status;
            target.Team1QuickScore = reverse ? source.Team2QuickScore : source.Team1QuickScore;
            target.Team2QuickScore = reverse ? source.Team1QuickScore : source.Team2QuickScore;
            target.SetsScore = reverse ? ReversePair(source.SetsScore) : source.SetsScore;
            target.TotalScore = reverse ? ReversePair(source.TotalScore) : source.TotalScore;
            target.SetsBySet = reverse && source.SetsBySet != null ? string.Join(";", source.SetsBySet.Split(';').Select(ReversePair)) : source.SetsBySet;
            target.Duration = source.Duration;
            target.Referee = source.Referee;
            target.Location = source.Location;
            target.Mvp = source.Mvp;
            target.MatchNumber = source.MatchNumber;
            target.MatchDate = source.MatchDate;
            target.StartTime = source.StartTime;
            target.WinnerId = source.WinnerId;
            target.CreatedByUserId = source.CreatedByUserId;
        }
    }
}
