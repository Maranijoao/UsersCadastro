using CadastroCliente.Models;
using Microsoft.Data.SqlClient;
using System.Net;

namespace CadastroCliente.Data
{
    public class AddressRepository
    {
        private readonly SqlConnectionProvider _connectionProvider;

        public AddressRepository(SqlConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<List<Address>> GetByUserIdAsync(int userId)
        {
            var addresses = new List<Address>();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT Id, UserId, CEP, Street, Number, Complement, Neighborhood, City, State FROM Addresses WHERE UserId = @UserId";
            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                addresses.Add(MapAddressFromReader(reader));
            }
            return addresses;
        }

        public async Task InsertAsync(Address address)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            await InsertAsync(address, connection, null);
        }

        public async Task InsertAsync(Address address, SqlConnection connection, SqlTransaction? transaction)
        {
            var command = new SqlCommand(@"
            INSERT INTO Addresses (UserId, CEP, Street, Number, Complement, Neighborhood, City, State) 
            VALUES (@UserId, @CEP, @Street, @Number, @Complement, @Neighborhood, @City, @State)", connection, transaction);

            command.Parameters.AddWithValue("@UserId", address.UserId);
            command.Parameters.AddWithValue("@CEP", (object)address.CEP ?? DBNull.Value);
            command.Parameters.AddWithValue("@Street", (object)address.Street ?? DBNull.Value);
            command.Parameters.AddWithValue("@Number", (object)address.Number ?? DBNull.Value);
            command.Parameters.AddWithValue("@Complement", (object)address.Complement ?? DBNull.Value);
            command.Parameters.AddWithValue("@Neighborhood", (object)address.Neighborhood ?? DBNull.Value);
            command.Parameters.AddWithValue("@City", (object)address.City ?? DBNull.Value);
            command.Parameters.AddWithValue("@State", (object)address.State ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteByUserIdAsync(int userId)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            await DeleteByUserIdAsync(userId, connection, null);
        }

        public async Task DeleteByUserIdAsync(int userId, SqlConnection connection, SqlTransaction? transaction)
        {
            var command = new SqlCommand("DELETE FROM Addresses WHERE UserId = @UserId", connection, transaction);
            command.Parameters.AddWithValue("@UserId", userId);
            await command.ExecuteNonQueryAsync();
        }

        private Address MapAddressFromReader(SqlDataReader reader)
        {
            return new Address 
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                CEP = reader.IsDBNull(reader.GetOrdinal("CEP")) ? string.Empty : reader.GetString(reader.GetOrdinal("CEP")),
                Street = reader.IsDBNull(reader.GetOrdinal("Street")) ? string.Empty : reader.GetString(reader.GetOrdinal("Street")),
                Number = reader.IsDBNull(reader.GetOrdinal("Number")) ? string.Empty : reader.GetString(reader.GetOrdinal("Number")),
                Complement = reader.IsDBNull(reader.GetOrdinal("Complement")) ? string.Empty : reader.GetString(reader.GetOrdinal("Complement")),
                Neighborhood = reader.IsDBNull(reader.GetOrdinal("Neighborhood")) ? string.Empty : reader.GetString(reader.GetOrdinal("Neighborhood")),
                City = reader.IsDBNull(reader.GetOrdinal("City")) ? string.Empty : reader.GetString(reader.GetOrdinal("City")),
                State = reader.IsDBNull(reader.GetOrdinal("State")) ? string.Empty : reader.GetString(reader.GetOrdinal("State"))
            };
        }
    }
}
