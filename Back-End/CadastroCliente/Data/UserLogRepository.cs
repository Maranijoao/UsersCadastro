using CadastroCliente.Models;
using Microsoft.Data.SqlClient;

namespace CadastroCliente.Data
{
    public class UserLogRepository
    {
        private readonly SqlConnectionProvider _connectionProvider;

        public UserLogRepository(SqlConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task AddLogAsync(UserLog log, SqlConnection connection, SqlTransaction transaction)
        {
            var command = new SqlCommand(@"
            INSERT INTO UserLogs (UserId, ChangedAt, ChangedBy, Action, OldValues, NewValues)
            VALUES (@UserId, @ChangedAt, @ChangedBy, @Action, @OldValues, @NewValues)", connection, transaction);

            command.Parameters.AddWithValue("@UserId", log.UserId);
            command.Parameters.AddWithValue("@ChangedAt", log.ChangedAt);
            command.Parameters.AddWithValue("@ChangedBy", log.ChangedBy);
            command.Parameters.AddWithValue("@Action", log.Action);
            command.Parameters.AddWithValue("@OldValues", (object)log.OldValues ?? DBNull.Value);
            command.Parameters.AddWithValue("@NewValues", (object)log.NewValues ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<UserLog>> GetLogsByUserIdAsync(int userId)
        {
            var logs = new List<UserLog>();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT Id, UserId, ChangedAt, ChangedBy, Action, OldValues, NewValues FROM UserLogs WHERE UserId = @UserId ORDER BY ChangedAt DESC", connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new UserLog()
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    ChangedAt = reader.GetDateTime(reader.GetOrdinal("ChangedAt")),
                    ChangedBy = reader.GetString(reader.GetOrdinal("ChangedBy")),
                    Action = reader.GetString(reader.GetOrdinal("Action")),
                    OldValues = reader.IsDBNull(reader.GetOrdinal("OldValues")) ? null : reader.GetString(reader.GetOrdinal("OldValues")),
                    NewValues = reader.IsDBNull(reader.GetOrdinal("NewValues")) ? null : reader.GetString(reader.GetOrdinal("NewValues"))
                });
            }
            return logs;
        }
    }
}

