using CadastroCliente.Models;
using Microsoft.Data.SqlClient;

namespace CadastroCliente.Data
{
    public class ClienteLogRepository
    {
        private readonly SqlConnectionProvider _connectionProvider;

        public ClienteLogRepository(SqlConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task AddLogAsync(ClienteLog log, SqlConnection connection, SqlTransaction transaction)
        {
            var command = new SqlCommand(@"INSERT INTO ClienteLogs (ClienteId, DataAlteracao, UsuarioAlteracao, Acao, DadosAntigos, DadosNovos)
            VALUES (@ClienteId, @DataAlteracao, @UsuarioAlteracao, @Acao, @DadosAntigos, @DadosNovos)", connection, transaction);

            command.Parameters.AddWithValue("@ClienteId", log.ClienteId);
            command.Parameters.AddWithValue("@DataAlteracao", log.DataAlteracao);
            command.Parameters.AddWithValue("@UsuarioAlteracao", log.UsuarioAlteracao);
            command.Parameters.AddWithValue("@Acao", log.Acao);
            command.Parameters.AddWithValue("@DadosAntigos", (object)log.DadosAntigos ?? DBNull.Value);
            command.Parameters.AddWithValue("@DadosNovos", (object)log.DadosNovos ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<ClienteLog>> GetLogsByClienteIdAsync(int clienteId)
        {
            var logs = new List<ClienteLog>();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT Id, ClienteId, DataAlteracao, UsuarioAlteracao, Acao FROM ClienteLogs WHERE ClienteId = @ClienteId ORDER BY DataAlteracao DESC", connection);
            command.Parameters.AddWithValue("@ClienteId", clienteId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new ClienteLog()
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ClienteId = reader.GetInt32(reader.GetOrdinal("ClienteId")),
                    DataAlteracao = reader.GetDateTime(reader.GetOrdinal("DataAlteracao")),
                    UsuarioAlteracao = reader.GetString(reader.GetOrdinal("UsuarioAlteracao")),
                    Acao = reader.GetString(reader.GetOrdinal("Acao"))
                });
            }
            return logs;
        }
    }
}

