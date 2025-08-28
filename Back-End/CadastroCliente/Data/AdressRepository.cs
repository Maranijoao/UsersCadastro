using CadastroCliente.Models;
using Microsoft.Data.SqlClient;

namespace CadastroCliente.Data
{
    public class AdressRepository
    {
        private readonly SqlConnectionProvider _connectionProvider;

        public AdressRepository(SqlConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<List<Endereco>> GetByClienteIdAsync(int clienteId)
        {
            var enderecos = new List<Endereco>();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT Id, ClienteId, CEP, Logradouro, Numero, Complemento, Bairro, Cidade, UF FROM Enderecos WHERE ClienteId = @ClienteId";
            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ClienteId", clienteId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                enderecos.Add(MapEnderecoFromReader(reader));
            }
            return enderecos;
        }

        public async Task InsertAsync(Endereco endereco)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            await InsertAsync(endereco, connection, null);
        }

        public async Task InsertAsync(Endereco endereco, SqlConnection connection, SqlTransaction? transaction)
        {
            var command = new SqlCommand(@"
                INSERT INTO Enderecos (ClienteId, CEP, Logradouro, Numero, Complemento, Bairro, Cidade, UF) 
                VALUES (@ClienteId, @CEP, @Logradouro, @Numero, @Complemento, @Bairro, @Cidade, @UF)", connection, transaction);

            command.Parameters.AddWithValue("@ClienteId", endereco.ClienteId);
            command.Parameters.AddWithValue("@CEP", (object)endereco.CEP ?? DBNull.Value);
            command.Parameters.AddWithValue("@Logradouro", (object)endereco.Logradouro ?? DBNull.Value);
            command.Parameters.AddWithValue("@Numero", (object)endereco.Numero ?? DBNull.Value);
            command.Parameters.AddWithValue("@Complemento", (object)endereco.Complemento ?? DBNull.Value);
            command.Parameters.AddWithValue("@Bairro", (object)endereco.Bairro ?? DBNull.Value);
            command.Parameters.AddWithValue("@Cidade", (object)endereco.Cidade ?? DBNull.Value);
            command.Parameters.AddWithValue("@UF", (object)endereco.UF ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteByClienteIdAsync(int clienteId)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            await DeleteByClienteIdAsync(clienteId, connection, null);
        }

        public async Task DeleteByClienteIdAsync(int clienteId, SqlConnection connection, SqlTransaction? transaction)
        {
            var command = new SqlCommand("DELETE FROM Enderecos WHERE ClienteId = @ClienteId", connection, transaction);
            command.Parameters.AddWithValue("@ClienteId", clienteId);
            await command.ExecuteNonQueryAsync();
        }

        private Endereco MapEnderecoFromReader(SqlDataReader reader)
        {
            return new Endereco
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ClienteId = reader.GetInt32(reader.GetOrdinal("ClienteId")),
                CEP = reader.IsDBNull(reader.GetOrdinal("CEP")) ? string.Empty : reader.GetString(reader.GetOrdinal("CEP")),
                Logradouro = reader.IsDBNull(reader.GetOrdinal("Logradouro")) ? string.Empty : reader.GetString(reader.GetOrdinal("Logradouro")),
                Numero = reader.IsDBNull(reader.GetOrdinal("Numero")) ? string.Empty : reader.GetString(reader.GetOrdinal("Numero")),
                Complemento = reader.IsDBNull(reader.GetOrdinal("Complemento")) ? string.Empty : reader.GetString(reader.GetOrdinal("Complemento")),
                Bairro = reader.IsDBNull(reader.GetOrdinal("Bairro")) ? string.Empty : reader.GetString(reader.GetOrdinal("Bairro")),
                Cidade = reader.IsDBNull(reader.GetOrdinal("Cidade")) ? string.Empty : reader.GetString(reader.GetOrdinal("Cidade")),
                UF = reader.IsDBNull(reader.GetOrdinal("UF")) ? string.Empty : reader.GetString(reader.GetOrdinal("UF"))
            };
        }
    }
}
