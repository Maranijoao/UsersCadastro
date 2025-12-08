using CadastroCliente.Models.Entities;
using CadastroCliente.Repositories.Data;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CadastroCliente.Repositories
{
    public class RateTableRepository : IRateTableRepository
    {
        private readonly SqlConnectionProvider _connectionProvider;

        public RateTableRepository(SqlConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }
        private RateTable MapFromReader(SqlDataReader reader)
        {
            return new RateTable
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                MinInstallmentAmount = reader.GetDecimal(reader.GetOrdinal("MinInstallmentAmount")),
                MaxInstallmentAmount = reader.GetDecimal(reader.GetOrdinal("MaxInstallmentAmount")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                AvailableRates = reader.GetString(reader.GetOrdinal("AvailableRates")),
                AvailableTerms = reader.GetString(reader.GetOrdinal("AvailableTerms")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }

        public async Task<IEnumerable<RateTable>> GetAllActiveAsync()
        {
            var tables = new List<RateTable>();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            var command = new SqlCommand("SELECT * FROM RateTables WHERE IsActive = 1", connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(MapFromReader(reader));
            }
            return tables;
        }

        public async Task<RateTable> GetByNameAsync(string name)
        {
            RateTable table = null;
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            var command = new SqlCommand("SELECT TOP 1* FROM RateTables WHERE Name = @Name", connection);
            command.Parameters.AddWithValue("@Name", name);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }
            return table;
        }

        public async Task<RateTable> CreateAsync(RateTable rateTable, string loggedInUser)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            var command = new SqlCommand(
        @"INSERT INTO RateTables (Name, MinInstallmentAmount, MaxInstallmentAmount, AvailableRates, AvailableTerms, IsActive, CreatedAt, CreatedBy)
          OUTPUT INSERTED.Id, INSERTED.CreatedAt
          VALUES (@Name, @Min, @Max, @Rates, @Terms, @IsActive, GETDATE(), @CreatedBy)", connection);

            command.Parameters.AddWithValue("@Name", rateTable.Name);
            command.Parameters.AddWithValue("@Min", rateTable.MinInstallmentAmount);
            command.Parameters.AddWithValue("@Max", rateTable.MaxInstallmentAmount);
            command.Parameters.AddWithValue("@Rates", rateTable.AvailableRates);
            command.Parameters.AddWithValue("@Terms", rateTable.AvailableTerms);
            command.Parameters.AddWithValue("@IsActive", rateTable.IsActive);
            command.Parameters.AddWithValue("@CreatedBy", loggedInUser);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                rateTable.Id = reader.GetInt32(0);
                rateTable.CreatedAt = reader.GetDateTime(1);
            }
            return rateTable;
        }
    }
}
