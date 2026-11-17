using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using SportHubBase.Models;
using SportHubBase.Services;
using SportHubBase.Services.Export;
using SportHubBase.Services.Results;
using SportHubBase.Services.Results.Data;
using SportHubBase.Services.Scheduling;
using SportHubBase.Services.Scheduling.Swiss;
using SportHubBase.Services.Statistics;
using SportHubBase.ViewModels;

internal static class Program
{
    private static int _passed;
    private static readonly string TestDirectory = Path.Combine(Path.GetTempPath(), "SportHubTests-" + Guid.NewGuid().ToString("N"));
    [STAThread]
    private static int Main()
    {
        try
        {
            Run("Круговой: все пары, чётное/нечётное число команд", RoundRobin);
            Run("Круговой: команда с названием Выходной", ReservedName);
            Run("Счёт: отрицательные, неполные и переполненные значения", Scores);
            Run("Результаты: победа, ничья, техническое поражение", Results);
            Run("Изменение счёта: одно уведомление и очистка старого формата", ScoreEditing);
            Run("Удаление сетов не оставляет старый счёт", ClearSets);
            Run("Швейцарка: полный цикл без повторов", Swiss);
            Run("Швейцарка: нечётный состав и пропуски тура", SwissOdd);
            Run("Швейцарка: возврат из тупика", SwissBacktracking);
            Run("SQLite: конфликт двух копий, откат, переименование", Storage);
            Run("SQLite: совместимость старой схемы", Migration);
            Run("SQLite: резервная копия и восстановление", Backup);
            Run("Кэш: серверная версия сохраняется без локального инкремента", Cache);
            Run("Операции: неудачный импорт восстанавливает модель", ImportRollback);
            Run("Экспорт: идентификаторы участников и отсутствие реквизитов", Export);
            Run("Восстановление: отложенное применение копии", Restore);
            Run("Плей-офф: числовой счёт и продвижение победителя", Bracket);
            Run("Волейбол: FIVB, победа 3:2 и 3:0", Volleyball);
            Run("Удаление: устаревшая версия не удаляет турнир", DeleteConflict);
            Run("Кэш: ошибка удалённого удаления сохраняет локальную копию", FailedRemoteDelete);
            Run("Восстановление: посторонняя база отклоняется", InvalidBackup);
            Run("Загрузка швейцарки не удаляет проведённые туры", LoadSwiss);
            Run("Круговое расписание сохраняет счёт при смене сторон", ReverseSchedule);
            Run("Снятие команды: матчи, таблица и сохранение расписания после загрузки", RemoveTeam);
            Run("Снятие команды: конфликт откатывает модель", RemoveTeamRollback);
            Run("Снятие команды: сетка не перестраивается при загрузке", RemoveBracketTeam);
            Run("Генерация тура: конфликт сохраняет предыдущую модель", RoundRollback);
            Run("SQLite: одновременная запись двух клиентов", ConcurrentSqlite);
            Run("Запуск: проверка настоящего контейнера приложения", Composition);
            Run("GitHub: версии тегов и выбор архива", UpdateVersions);
            Run("GitHub: отсутствие архива и отклонение посторонней ссылки", UpdateLinks);
            Run("GitHub: черновики и предварительные релизы", UpdateUnpublished);
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SPORTHUB_TEST_SQL_SERVER")))
                Run("SQL Server: схема, конфликт, откат и идентификаторы", SqlServer);
            else Console.WriteLine("SKIP: SQL Server — не задан SPORTHUB_TEST_SQL_SERVER.");
            Console.WriteLine("Проверок пройдено: " + _passed);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally
        {
            SqliteConnection.ClearAllPools();
            var fullPath = Path.GetFullPath(TestDirectory);
            if (fullPath.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(fullPath).StartsWith("SportHubTests-", StringComparison.Ordinal) && Directory.Exists(fullPath))
                Directory.Delete(fullPath, true);
        }
    }
    private static void Run(string name, Action test) { test(); _passed++; Console.WriteLine("OK: " + name); }
    private static void Check(bool condition) { if (!condition) throw new Exception("Проверка не пройдена."); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Ожидалось исключение " + typeof(T).Name);
    }
    private static Tournament Tournament(int count = 4) => new Tournament { Name = "Тест", Type = "Круговой", SportType = "Футбол",
        Teams = Enumerable.Range(1, count).Select(i => new Team { Name = "Команда " + i, InitialSeed = i }).ToList() };
    private static void RoundRobin()
    {
        for (int count = 2; count <= 17; count++)
        {
            var t = Tournament(count);
            var matches = new RoundRobinBergerScheduleStrategy().GenerateSchedule(t.Teams).ToList();
            Check(matches.Count == count * (count - 1) / 2);
            Check(matches.All(m => m.Team1Id.HasValue && m.Team2Id.HasValue && m.Team1Id != m.Team2Id));
            Check(matches.Select(Key).Distinct().Count() == matches.Count);
            Check(matches.GroupBy(m => m.Round).All(r => r.SelectMany(m => new[] { m.Team1Id, m.Team2Id }).Distinct().Count() == r.Count() * 2));
        }
    }
    private static string Key(Match m) => string.Join("/", new[] { m.Team1Id.ToString(), m.Team2Id.ToString() }.OrderBy(x => x));
    private static void ReservedName()
    {
        var t = Tournament(3); t.Teams[0].Name = "Выходной";
        Check(new RoundRobinBergerScheduleStrategy().GenerateSchedule(t.Teams).Count() == 3);
    }
    private static void Scores()
    {
        Check(ScorePair.Parse(" 2 : 1 ").Left == 2);
        foreach (var score in new[] { "-1:2", "1:", "1:2:3", "2147483648:0", "x:1" }) Check(ScorePair.Parse(score) == null);
        Throws<ArgumentException>(() => new ScorePair(-1, 0));
    }
    private static void Results()
    {
        var t = Tournament(3);
        t.Matches = new List<Match> { Played(t, 0, 1, "2", "0"), Played(t, 0, 2, "1", "1"), Played(t, 1, 2, "0", "3") };
        t.Matches[2].Status = "Техническое поражение";
        var rows = ((RoundRobinResultsData)new RoundRobinResultsProvider().ComputeResults(t, new ObservableCollection<Match>(t.Matches))).Rows;
        var first = rows.Single(r => r.TeamName == t.Teams[0].Name);
        Check(first.Points == 4 && first.Wins == 1 && first.Draws == 1 && first.Losses == 0);
        Check(rows.Sum(r => r.Wins) == rows.Sum(r => r.Losses));
        Check(rows.Sum(r => r.Draws) == 2);
    }
    private static Match Played(Tournament t, int a, int b, string left = "1", string right = "0", int round = 1) => new Match {
        Team1Id = t.Teams[a].Id, Team2Id = t.Teams[b].Id, Team1 = t.Teams[a].Name, Team2 = t.Teams[b].Name,
        Team1QuickScore = left, Team2QuickScore = right, Status = "Сыгран", Round = round };
    private static void ScoreEditing()
    {
        var m = new Match { SetsScore = "3:0" };
        int notifications = 0; m.PropertyChanged += (s, e) => notifications++;
        var edit = new SimpleScoreEditContext(m) { Team1QuickScore = "1", Team2QuickScore = "2" };
        edit.ApplyChanges();
        Check(notifications == 1 && m.Score.Left == 1 && m.Score.Right == 2 && m.Status == "Сыгран");
        edit.Team1QuickScore = "-1";
        Throws<ArgumentException>(() => edit.ApplyChanges());
        Check(m.Team1QuickScore == "1");
    }
    private static void ClearSets()
    {
        var m = new Match { SetsBySet = "25:10", SetsScore = "1:0", Team1QuickScore = "1", Team2QuickScore = "0" };
        var edit = new SetScoreEditContext(m); edit.SetsList.Clear(); edit.ApplyChanges();
        Check(m.Score == null && string.IsNullOrEmpty(m.SetsBySet));
    }
    private static SwissScheduleStrategy SwissStrategy() => new SwissScheduleStrategy(new TiebreakerCalculator(new StatisticsCalculatorFactory()), new PlayedMatchesService());
    private static void Swiss()
    {
        var t = Tournament(6); var strategy = SwissStrategy();
        for (int round = 1; round <= 5; round++)
        {
            var matches = strategy.GenerateNextRound(t, round).ToList();
            Check(matches.Count == 3);
            foreach (var m in matches) { m.Status = "Сыгран"; m.Team1QuickScore = "1"; m.Team2QuickScore = "0"; }
            t.Matches.AddRange(matches);
        }
        Check(t.Matches.Select(Key).Distinct().Count() == 15);
        Throws<InvalidOperationException>(() => strategy.GenerateNextRound(t, 6).ToList());
    }
    private static void SwissOdd()
    {
        var t = Tournament(5); var strategy = SwissStrategy();
        for (int round = 1; round <= 3; round++)
        {
            var matches = strategy.GenerateNextRound(t, round).ToList();
            Check(matches.Count == 3 && matches.Count(m => m.IsBye) == 1);
            foreach (var m in matches) { m.Status = "Сыгран"; m.Team1QuickScore = "1"; m.Team2QuickScore = "0"; }
            t.Matches.AddRange(matches);
        }
        Check(t.Matches.Where(m => m.IsBye).Select(m => m.Team1Id).Distinct().Count() == 3);
    }
    private static void SwissBacktracking()
    {
        // Допустимые пары AB, AC, BD. Выбор AB ведёт в тупик CD; решение AC + BD существует.
        var t = Tournament(4);
        t.Matches.Add(Played(t, 0, 3)); t.Matches.Add(Played(t, 1, 2)); t.Matches.Add(Played(t, 2, 3));
        var matches = SwissStrategy().GenerateNextRound(t, 2).ToList();
        Check(matches.Count == 2 && !matches.Any(m => t.Matches.Any(old => Key(old) == Key(m))));
    }
    private static SqliteDatabaseService Database() => new SqliteDatabaseService(Path.Combine(TestDirectory, Guid.NewGuid().ToString("N"), "sporthub.db"));
    private static void Storage()
    {
        var db = Database(); var store = new SqliteStorageService(db); var t = Tournament();
        t.Matches.Add(Played(t, 0, 1)); store.CreateTournament(t);
        var first = store.GetTournamentById(t.Id); var stale = store.GetTournamentById(t.Id);
        first.Teams.Add(new Team { Name = "Новая команда" }); store.UpdateTournament(first);
        stale.Name = "Устаревшая запись";
        Throws<StorageConflictException>(() => store.UpdateTournament(stale));
        Check(store.GetTournamentById(t.Id).Teams.Count == 5 && stale.Revision == 1);
        first.Teams[0].Name = "Новое имя"; store.UpdateTournament(first);
        var loaded = store.GetTournamentById(t.Id);
        Check(loaded.Matches[0].Team1 == "Новое имя" && loaded.Matches[0].Team1Id == first.Teams[0].Id);
        store.DeleteTournament(t.Id);
        Throws<StorageConflictException>(() => store.UpdateTournament(first));
    }
    private static void Migration()
    {
        var db = Database(); var store = new SqliteStorageService(db); var t = Tournament(); store.CreateTournament(t);
        using (var c = new SqliteConnection(db.GetConnectionString()))
        {
            c.Open(); using (var cmd = c.CreateCommand()) { cmd.CommandText = "ALTER TABLE Tournaments DROP COLUMN Revision; ALTER TABLE Matches DROP COLUMN Team1Id; ALTER TABLE Matches DROP COLUMN Team2Id;"; cmd.ExecuteNonQuery(); }
        }
        var migrated = new SqliteDatabaseService(db.DatabasePath);
        var loaded = new SqliteStorageService(migrated).GetTournamentById(t.Id);
        Check(loaded != null && loaded.Revision == 0 && Directory.GetFiles(migrated.Backups.BackupDirectory).Length > 0);
    }
    private static void Backup()
    {
        var db = Database(); var store = new SqliteStorageService(db); var t = Tournament(); store.CreateTournament(t);
        string backup = db.Backups.CreateBackup(true);
        t.Name = "Изменён"; store.UpdateTournament(t);
        string restoredPath = Path.Combine(Path.GetDirectoryName(db.DatabasePath), "restored.db");
        DatabaseBackupService.CopyDatabase(backup, restoredPath);
        Check(new SqliteStorageService(new SqliteDatabaseService(restoredPath)).GetTournamentById(t.Id).Name == "Тест");
        Throws<ArgumentException>(() => DatabaseBackupService.CopyDatabase(backup, backup));
    }
    private static void Cache()
    {
        var db = Database(); var local = new SqliteStorageService(db); var t = Tournament(); t.IsRemote = true; t.Revision = 42;
        local.CacheTournament(t);
        Check(t.Revision == 42 && local.GetTournamentById(t.Id).Revision == 42);
        t.Revision = 43; local.CacheTournament(t);
        Check(local.GetTournamentById(t.Id).Revision == 43);
    }
    private static void ImportRollback()
    {
        var store = new SqliteStorageService(Database()); var t = Tournament(); store.CreateTournament(t);
        var stale = store.GetTournamentById(t.Id); t.Name = "Новее"; store.UpdateTournament(t);
        var previous = stale.Teams;
        Throws<StorageConflictException>(() => new TournamentOperationsService(store).ReplaceTeams(stale, new List<Team> { new Team { Name = "Импорт" } }));
        Check(ReferenceEquals(previous, stale.Teams));
    }
    private static void Export()
    {
        var t = Tournament(); t.Matches.Add(Played(t, 0, 1)); t.OrganizerPasswordHash = "TEST_HASH_DO_NOT_EXPORT";
        var service = new TournamentJsonExportService();
        var model = service.BuildExportModel(t, null, null);
        Check(model.Matches[0].Team1Id == t.Teams[0].Id);
        Check(!service.ExportToJsonString(t, null, null).Contains("TEST_HASH_DO_NOT_EXPORT"));
    }
    private static void Restore()
    {
        var db = Database(); var store = new SqliteStorageService(db); var t = Tournament(); store.CreateTournament(t);
        var backup = db.Backups.CreateBackup(true);
        t.Name = "После копии"; store.UpdateTournament(t);
        db.Backups.StageRestore(backup);
        Check(store.GetTournamentById(t.Id).Name == "После копии");
        var restored = new SqliteStorageService(new SqliteDatabaseService(db.DatabasePath));
        Check(restored.GetTournamentById(t.Id).Name == "Тест");
    }
    private static void Bracket()
    {
        var t = Tournament(); var next = new BracketMatch();
        var match = new BracketMatch { Team1 = t.Teams[0], Team2 = t.Teams[1], NextMatch = next, IsTeam1InNext = true };
        match.ApplyScore("3", "1"); match.TryAdvance();
        Check(match.Winner.Id == t.Teams[0].Id && next.Team1.Id == t.Teams[0].Id);
        Throws<ArgumentException>(() => match.ApplyScore("-1", "0"));
    }
    private static void SqlServer()
    {
        string server = Environment.GetEnvironmentVariable("SPORTHUB_TEST_SQL_SERVER");
        string database = "SportHubTests_" + Guid.NewGuid().ToString("N");
        var master = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder { DataSource = server, InitialCatalog = "master", IntegratedSecurity = true, TrustServerCertificate = true };
        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(master.ConnectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand()) { command.CommandText = "CREATE DATABASE [" + database + "]"; command.ExecuteNonQuery(); }
            try
            {
                var config = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(master.ConnectionString) { InitialCatalog = database };
                var store = new SqlServerStorageService(new SqlServerDatabaseService());
                var t = Tournament(); t.ServerAddress = config.ConnectionString; t.Matches.Add(Played(t, 0, 1));
                store.CreateTournament(t);
                var first = store.GetTournamentById(t.Id, t.ServerAddress);
                var stale = store.GetTournamentById(t.Id, t.ServerAddress);
                first.Teams.Add(new Team { Name = "Новая команда" }); store.UpdateTournament(first);
                stale.Teams.Clear(); stale.Matches.Clear();
                Throws<StorageConflictException>(() => store.UpdateTournament(stale));
                var saved = store.GetTournamentById(t.Id, t.ServerAddress);
                Check(saved.Teams.Count == 5 && saved.Matches.Count == 1 && saved.Revision == 2);
                Check(saved.Matches[0].Team1Id == t.Teams[0].Id);
                Concurrent(() => store.GetTournamentById(t.Id, t.ServerAddress), item => store.UpdateTournament(item));
            }
            finally
            {
                Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "ALTER DATABASE [" + database + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + database + "]";
                    command.ExecuteNonQuery();
                }
            }
        }
    }
    private static void Volleyball()
    {
        var t = Tournament(3); t.SportType = "Волейбол"; t.ScoringSystem = "FIVB";
        t.Matches = new List<Match> { Played(t, 0, 1, "3", "2"), Played(t, 0, 2, "3", "0") };
        var rows = ((RoundRobinResultsData)new RoundRobinResultsProvider().ComputeResults(t, new ObservableCollection<Match>(t.Matches))).Rows;
        Check(rows.Single(r => r.TeamName == t.Teams[0].Name).Points == 5);
        Check(rows.Single(r => r.TeamName == t.Teams[1].Name).Points == 1);
    }
    private static void DeleteConflict()
    {
        var store = new SqliteStorageService(Database()); var t = Tournament(); store.CreateTournament(t);
        var stale = store.GetTournamentById(t.Id); t.Name = "Новее"; store.UpdateTournament(t);
        Throws<StorageConflictException>(() => new TournamentOperationsService(store).Delete(stale));
        Check(store.GetTournamentById(t.Id).Name == "Новее");
    }
    private static void FailedRemoteDelete()
    {
        var sqlite = new SqliteStorageService(Database()); var t = Tournament();
        t.IsRemote = true; t.ServerAddress = "Server=invalid;UnknownOption=value;"; t.IsOrganizerRemembered = true;
        sqlite.CacheTournament(t);
        var dynamicStorage = new DynamicStorageService(sqlite, new SqlServerStorageService(new SqlServerDatabaseService()));
        Throws<InvalidOperationException>(() => dynamicStorage.DeleteTournament(t));
        Check(sqlite.GetTournamentById(t.Id) != null);
    }
    private static void InvalidBackup()
    {
        var db = Database(); var path = Path.Combine(Path.GetDirectoryName(db.DatabasePath), "foreign.db");
        using (var c = new SqliteConnection("Data Source=" + path)) { c.Open(); }
        Throws<ArgumentException>(() => db.Backups.StageRestore(path));
        Check(!File.Exists(Path.Combine(Path.GetDirectoryName(db.DatabasePath), "pending-restore.db")));
    }
    private static void LoadSwiss()
    {
        var store = new SqliteStorageService(Database()); var t = Tournament(5); t.Type = "Швейцарка";
        store.CreateTournament(t);
        var service = new TournamentScheduleService(store, new MatchService()); var strategy = SwissStrategy();
        service.Prepare(t, strategy);
        Check(t.Matches.Count == 3);
        var ids = t.Matches.Select(m => m.Id).ToList(); long revision = t.Revision;
        var loaded = store.GetTournamentById(t.Id); service.Prepare(loaded, strategy);
        Check(loaded.Matches.Select(m => m.Id).OrderBy(x => x).SequenceEqual(ids.OrderBy(x => x)) && loaded.Revision == revision);
        Check(loaded.Matches.Single(m => m.Team2 == "BYE").IsBye);
    }
    private static void ReverseSchedule()
    {
        var store = new SqliteStorageService(Database()); var t = Tournament(2);
        var match = Played(t, 0, 1, "2", "1"); match.SetsBySet = "25:20;25:21"; t.Matches.Add(match);
        store.CreateTournament(t); t.Teams.Reverse();
        new TournamentScheduleService(store, new MatchService()).Prepare(t, new RoundRobinBergerScheduleStrategy());
        Check(t.Matches.Single().Id == match.Id && t.Matches[0].Team1Id == match.Team2Id);
        Check(t.Matches[0].Score.Left == 1 && t.Matches[0].Score.Right == 2 && t.Matches[0].SetsBySet == "20:25;21:25");
        long revision = t.Revision;
        new TournamentScheduleService(store, new MatchService()).Prepare(t, new RoundRobinBergerScheduleStrategy());
        Check(t.Revision == revision);
    }
    private static void RemoveTeam()
    {
        var store = new SqliteStorageService(Database()); var t = Tournament(4);
        var removedId = t.Teams[0].Id;
        var kept = Played(t, 1, 2, "3", "1", 5);
        kept.MatchNumber = 17; kept.MatchDate = new DateTime(2026, 10, 15); kept.Location = "Зал";
        var future = new Match { Team1Id = t.Teams[2].Id, Team2Id = t.Teams[3].Id,
            Team1 = t.Teams[2].Name, Team2 = t.Teams[3].Name, Round = 6, MatchNumber = 24 };
        t.Matches.AddRange(new[] { Played(t, 0, 1), kept, future,
            new Match { Team1 = t.Teams[0].Name, Team2 = t.Teams[3].Name } });
        store.CreateTournament(t);
        // Проверяем совместимость со старыми записями без ссылок по ID.
        t.Matches.Last().Team1Id = null; t.Matches.Last().Team2Id = null;
        new TournamentOperationsService(store).RemoveTeam(t, removedId);
        Check(t.Matches.Count == 2 && ReferenceEquals(t.Matches[0], kept) && t.Teams.Count == 3);
        t = store.GetTournamentById(t.Id);
        Check(t.PreserveSchedule && t.Matches.Count == 2 && t.Teams.All(x => x.Id != removedId));
        var before = Newtonsoft.Json.JsonConvert.SerializeObject(t.Matches);
        new TournamentScheduleService(store, new MatchService()).Prepare(t, new RoundRobinBergerScheduleStrategy());
        Check(before == Newtonsoft.Json.JsonConvert.SerializeObject(t.Matches));
        var saved = t.Matches.Single(m => m.Id == kept.Id);
        Check(saved.Round == 5 && saved.MatchNumber == 17 && saved.MatchDate == kept.MatchDate
            && saved.Team1QuickScore == "3" && saved.Team2QuickScore == "1" && saved.Location == "Зал");
        var rows = ((RoundRobinResultsData)new RoundRobinResultsProvider().ComputeResults(t, new ObservableCollection<Match>(t.Matches))).Rows;
        Check(rows.Count == 3 && rows.Sum(r => r.Wins) == 1 && rows.Sum(r => r.Losses) == 1);
        new TournamentOperationsService(store).RemoveTeam(t, t.Teams.Single(x => x.Id == kept.Team2Id).Id);
        t = store.GetTournamentById(t.Id);
        Check(t.Teams.Count == 2 && t.Matches.Count == 0);
        new TournamentScheduleService(store, new MatchService()).Prepare(t, new RoundRobinBergerScheduleStrategy());
        Check(t.Matches.Count == 0);
    }
    private static void RemoveTeamRollback()
    {
        var store = new SqliteStorageService(Database()); var t = Tournament();
        t.Matches.Add(Played(t, 0, 1)); store.CreateTournament(t);
        var stale = store.GetTournamentById(t.Id); t.Name = "Новее"; store.UpdateTournament(t);
        var teams = stale.Teams; var matches = stale.Matches;
        Throws<StorageConflictException>(() => new TournamentOperationsService(store).RemoveTeam(stale, stale.Teams[0].Id));
        Check(ReferenceEquals(teams, stale.Teams) && ReferenceEquals(matches, stale.Matches) && !stale.PreserveSchedule);
        Check(store.GetTournamentById(t.Id).Teams.Count == 4);
    }
    private static void RemoveBracketTeam()
    {
        var store = new SqliteStorageService(Database()); var t = Tournament(); t.Type = "Олимпийский";
        var removed = new BracketMatch { Team1 = t.Teams[0], Team2 = t.Teams[1] };
        var kept = new BracketMatch { Team1 = t.Teams[2], Team2 = t.Teams[3], Score1 = "2", Score2 = "0" };
        var final = new BracketMatch { Team1 = t.Teams[0], Team2 = t.Teams[2] };
        removed.NextMatch = final; kept.NextMatch = final;
        t.Bracket = new TournamentBracket();
        t.Bracket.Rounds.Add(new BracketRound { Matches = new ObservableCollection<BracketMatch> { removed, kept } });
        t.Bracket.Rounds.Add(new BracketRound { Matches = new ObservableCollection<BracketMatch> { final } });
        store.CreateTournament(t);
        new TournamentOperationsService(store).RemoveTeam(t, t.Teams[0].Id);
        t = store.GetTournamentById(t.Id);
        Check(t.Bracket.PreserveStructure && t.Bracket.Rounds[1].Matches.Count == 0);
        var match = t.Bracket.Rounds[0].Matches.Single();
        Check(match.Id == kept.Id && match.Score1 == "2" && match.Score2 == "0" && match.MatchNumber == 2 && match.NextMatch == null);
    }
    private static void RoundRollback()
    {
        var store = new SqliteStorageService(Database()); var t = Tournament(); t.Type = "Швейцарка";
        store.CreateTournament(t); var stale = store.GetTournamentById(t.Id);
        t.Name = "Новее"; store.UpdateTournament(t);
        var previous = stale.Matches;
        Throws<StorageConflictException>(() => new TournamentScheduleService(store, new MatchService()).GenerateRound(stale, SwissStrategy(), 1));
        Check(ReferenceEquals(previous, stale.Matches) && stale.Matches.Count == 0);
    }
    private static void Concurrent(Func<Tournament> load, Action<Tournament> save)
    {
        var copies = new[] { load(), load() };
        using (var barrier = new System.Threading.Barrier(2))
        {
            var tasks = copies.Select((copy, index) => System.Threading.Tasks.Task.Run(() =>
            {
                copy.Name = "Параллельно " + index;
                barrier.SignalAndWait();
                try { save(copy); return true; }
                catch (StorageConflictException) { return false; }
            })).ToArray();
            System.Threading.Tasks.Task.WaitAll(tasks);
            Check(tasks.Count(task => task.Result) == 1);
        }
    }
    private static void ConcurrentSqlite()
    {
        var store = new SqliteStorageService(Database()); var t = Tournament(); store.CreateTournament(t);
        Concurrent(() => store.GetTournamentById(t.Id), item => store.UpdateTournament(item));
        Check(store.GetTournamentById(t.Id).Revision == 2);
    }
    private static void Composition()
    {
        var application = new SportHubBase.App();
        var configure = typeof(SportHubBase.App).GetMethod("ConfigureContainer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        configure.Invoke(application, new object[] { Path.Combine(TestDirectory, "composition", "sporthub.db") });
        var storage = SportHubBase.App.Container.GetInstance<SportHubBase.Interfaces.IStorage>();
        var t = Tournament(); storage.CreateTournament(t);
        Check(storage.GetTournamentById(t.Id).Name == t.Name);
        SportHubBase.App.Container.Dispose();
    }

    private static string ReleaseJson(string tag, string name = null, string url = null)
    {
        return new Newtonsoft.Json.Linq.JObject {
            ["tag_name"] = tag, ["body"] = "Изменения версии", ["draft"] = false, ["prerelease"] = false,
            ["assets"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject {
                ["name"] = name ?? "SportHubBase-" + tag.TrimStart('v', 'V') + ".zip",
                ["state"] = "uploaded", ["browser_download_url"] = url ?? "https://github.com/SniperTormenta/SportHub/releases/download/" + tag + "/SportHubBase-" + tag.TrimStart('v', 'V') + ".zip"
            })
        }.ToString();
    }
    private static void UpdateVersions()
    {
        var release = UpdateRelease.FromGitHubJson(ReleaseJson("v1.2.3"), "SniperTormenta/SportHub");
        Check(release.IsNewerThan(new Version(1, 2, 2, 0)));
        Check(!release.IsNewerThan(new Version(1, 2, 3, 0)) && !release.IsNewerThan(new Version(2, 0)));
        Check(release.DownloadUrl.AbsoluteUri.EndsWith("/SportHubBase-1.2.3.zip"));
        Check(!UpdateRelease.FromGitHubJson(ReleaseJson("V1.0"), "SniperTormenta/SportHub").IsNewerThan(new Version(1, 0, 0, 0)));
        Throws<InvalidOperationException>(() => UpdateRelease.FromGitHubJson(ReleaseJson("latest"), "SniperTormenta/SportHub"));
    }
    private static void UpdateLinks()
    {
        foreach (var url in new[] { "https://example.org/archive.zip", "file:///C:/update.exe", "https://github.com/Other/Repo/releases/download/v2/test.zip" })
        {
            var release = UpdateRelease.FromGitHubJson(ReleaseJson("v2.0", null, url), "SniperTormenta/SportHub");
            Check(release.DownloadUrl.AbsoluteUri == "https://github.com/SniperTormenta/SportHub/releases/tag/v2.0");
        }
        var missing = UpdateRelease.FromGitHubJson(ReleaseJson("v2.0", "update.xml"), "SniperTormenta/SportHub");
        Check(missing.DownloadUrl.AbsolutePath.EndsWith("/releases/tag/v2.0"));
    }
    private static void UpdateUnpublished()
    {
        var json = Newtonsoft.Json.Linq.JObject.Parse(ReleaseJson("v2.0"));
        json["draft"] = true;
        Check(UpdateRelease.FromGitHubJson(json.ToString(), "SniperTormenta/SportHub") == null);
        json["draft"] = false; json["prerelease"] = true;
        Check(UpdateRelease.FromGitHubJson(json.ToString(), "SniperTormenta/SportHub") == null);
        Check(!UpdateRelease.IsValidRepository("../invalid?repo") && UpdateRelease.IsValidRepository("SniperTormenta/SportHub"));
    }
}
