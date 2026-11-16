using SportHubBase.Models;
using System;
using System.Data.Common;

namespace SportHubBase.Services
{
    internal static class TournamentRevisionService
    {
        public static long Check(DbConnection connection, DbTransaction transaction, Tournament tournament, bool cache)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                var locking = connection is Microsoft.Data.SqlClient.SqlConnection ? " WITH (UPDLOCK, HOLDLOCK)" : "";
                command.CommandText = "SELECT Revision, IsDeleted FROM Tournaments" + locking + " WHERE Id = @Id";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@Id";
                parameter.Value = tournament.Id.ToString();
                command.Parameters.Add(parameter);
                using (var reader = command.ExecuteReader())
                {
                    bool exists = reader.Read();
                    if (!cache && (exists ? Convert.ToInt64(reader[0]) != tournament.Revision || Convert.ToBoolean(reader[1]) : tournament.Revision != 0))
                        throw new StorageConflictException();
                }
            }
            return cache ? tournament.Revision : checked(tournament.Revision + 1);
        }

        public static void Store(DbConnection connection, DbTransaction transaction, Guid id, long revision)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "UPDATE Tournaments SET Revision = @Revision WHERE Id = @Id";
                var idParameter = command.CreateParameter();
                idParameter.ParameterName = "@Id";
                idParameter.Value = id.ToString();
                command.Parameters.Add(idParameter);
                var revisionParameter = command.CreateParameter();
                revisionParameter.ParameterName = "@Revision";
                revisionParameter.Value = revision;
                command.Parameters.Add(revisionParameter);
                command.ExecuteNonQuery();
            }
        }
    }
}
