using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;

namespace SportHubBase.Services
{
    public sealed class DatabaseBackupService
    {
        private readonly string _databasePath;
        private readonly object _gate = new object();
        private DateTime _lastBackupUtc;
        public string BackupDirectory => Path.Combine(Path.GetDirectoryName(_databasePath), "Backups");

        public DatabaseBackupService(string databasePath) { _databasePath = databasePath; }

        public string CreateBackup(bool force = false)
        {
            lock (_gate)
            {
                if (!File.Exists(_databasePath)) return null;
                if (!force && DateTime.UtcNow - _lastBackupUtc < TimeSpan.FromMinutes(5)) return null;
                Directory.CreateDirectory(BackupDirectory);
                var path = Path.Combine(BackupDirectory, "sporthub-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".db");
                CopyDatabase(_databasePath, path);
                _lastBackupUtc = DateTime.UtcNow;
                foreach (var old in Directory.GetFiles(BackupDirectory, "sporthub-*.db").OrderByDescending(File.GetCreationTimeUtc).Skip(20))
                    File.Delete(old);
                return path;
            }
        }

        public void StageRestore(string source)
        {
            using (var input = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            {
                input.Open();
                using (var check = input.CreateCommand())
                {
                    check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('Tournaments', 'Teams', 'Players', 'Matches', 'Accounts')";
                    if (Convert.ToInt32(check.ExecuteScalar()) != 5) throw new ArgumentException("Файл не является резервной копией SportHub.");
                }
            }
            CopyDatabase(source, Path.Combine(Path.GetDirectoryName(_databasePath), "pending-restore.db"));
        }

        public static void CopyDatabase(string source, string destination)
        {
            if (Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Источник и назначение резервной копии должны отличаться.");
            using (var input = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            using (var output = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destination, Pooling = false }.ToString()))
            {
                input.Open();
                using (var check = input.CreateCommand())
                {
                    check.CommandText = "PRAGMA integrity_check;";
                    if (!string.Equals(Convert.ToString(check.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Проверка целостности резервной копии не пройдена.");
                }
                output.Open();
                input.BackupDatabase(output);
            }
        }
    }
}
