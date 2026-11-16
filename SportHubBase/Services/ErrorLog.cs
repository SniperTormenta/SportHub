using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SportHubBase.Services
{
    public static class ErrorLog
    {
        private static readonly object Gate = new object();
        public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SportHub", "Logs");

        public static void Write(string operation, Exception error)
        {
            // Не записываем сообщения SQL-драйвера и значения параметров: они могут содержать реквизиты.
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(DirectoryPath);
                    var trace = Regex.Replace(error.StackTrace ?? "", @" in .*?:line \d+", "");
                    File.AppendAllText(Path.Combine(DirectoryPath, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log"),
                        DateTime.UtcNow.ToString("O") + " " + operation + " " + error.GetType().FullName + Environment.NewLine + trace + Environment.NewLine);
                    foreach (var file in Directory.GetFiles(DirectoryPath, "*.log"))
                        if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-30)) File.Delete(file);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public static string UserMessage(Exception error)
        {
            if (error is StorageConflictException) return error.Message;
            if (error is ArgumentException || error is InvalidOperationException)
                return error.Message;
            var sqlite = error as Microsoft.Data.Sqlite.SqliteException;
            if (sqlite != null)
            {
                if (sqlite.SqliteErrorCode == 5 || sqlite.SqliteErrorCode == 6) return "База данных занята. Подождите и повторите сохранение.";
                if (sqlite.SqliteErrorCode == 13) return "Недостаточно места на диске для сохранения данных.";
                if (sqlite.SqliteErrorCode == 19) return "Данные нарушают ограничения базы. Проверьте команды, игроков и матчи.";
            }
            var sql = error as Microsoft.Data.SqlClient.SqlException;
            if (sql != null && sql.Number == 18456) return "SQL Server отклонил учётные данные. Проверьте настройки подключения.";
            if (error is UnauthorizedAccessException) return "Нет прав для записи данных. Проверьте доступ к папке данных.";
            return "Операция не завершена. Проверьте подключение и доступ к данным. Подробности записаны в журнал ошибок.";
        }
    }
}
