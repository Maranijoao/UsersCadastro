using CadastroCliente.Models.DTOs;
using CadastroCliente.Models.DTOs.Installment;
using CadastroCliente.Models.Entities;
using CadastroCliente.Repositories.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CadastroCliente.Repositories
{
    public class InstallmentRepository : IInstallmentRepository
    {
        private readonly SqlConnectionProvider _connectionProvider;

        public InstallmentRepository(SqlConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task AddBulkAsync(List<Installment> installments)
        {
            if (installments == null || installments.Count == 0) return;

            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.DestinationTableName = "Installments";

                bulkCopy.ColumnMappings.Add(nameof(Installment.SimulationId), "SimulationId");
                bulkCopy.ColumnMappings.Add(nameof(Installment.InstallmentNumber), "InstallmentNumber");
                bulkCopy.ColumnMappings.Add(nameof(Installment.OriginalAmount), "OriginalAmount");
                bulkCopy.ColumnMappings.Add(nameof(Installment.Status), "Status");
                bulkCopy.ColumnMappings.Add(nameof(Installment.DueDate), "DueDate");

                bulkCopy.ColumnMappings.Add(nameof(Installment.OpeningBalance), "OpeningBalance");
                bulkCopy.ColumnMappings.Add(nameof(Installment.Interest), "Interest");
                bulkCopy.ColumnMappings.Add(nameof(Installment.Amortization), "Amortization");
                bulkCopy.ColumnMappings.Add(nameof(Installment.Balance), "Balance");

                bulkCopy.ColumnMappings.Add(nameof(Installment.PaidAmount), "PaidAmount");
                bulkCopy.ColumnMappings.Add(nameof(Installment.PaymentDate), "PaymentDate");

                var dt = new System.Data.DataTable();
                dt.Columns.Add("SimulationId", typeof(int));
                dt.Columns.Add("InstallmentNumber", typeof(int));
                dt.Columns.Add("OriginalAmount", typeof(decimal));
                dt.Columns.Add("Status", typeof(string));
                dt.Columns.Add("DueDate", typeof(DateTime));

                dt.Columns.Add("OpeningBalance", typeof(decimal));
                dt.Columns.Add("Interest", typeof(decimal));
                dt.Columns.Add("Amortization", typeof(decimal));
                dt.Columns.Add("Balance", typeof(decimal));

                dt.Columns.Add("PaidAmount", typeof(decimal));
                dt.Columns.Add("PaymentDate", typeof(DateTime));

                foreach (var inst in installments)
                {
                    object paidAmount = inst.PaidAmount.HasValue ? (object)inst.PaidAmount.Value : DBNull.Value;
                    object paymentDate = inst.PaymentDate.HasValue ? (object)inst.PaymentDate.Value : DBNull.Value;

                    dt.Rows.Add(inst.SimulationId, inst.InstallmentNumber, inst.OriginalAmount, inst.Status, inst.DueDate, inst.OpeningBalance, inst.Interest, inst.Amortization, inst.Balance, paidAmount, paymentDate);
                }

                await bulkCopy.WriteToServerAsync(dt);
            }
        }

        public async Task<PaginatedInstallmentResult> GetBySimulationIdAsync(int simulationId, int pageNumber, int pageSize)
        {
            var items = new List<InstallmentDto>();
            int totalCount = 0;

            using (var connection = _connectionProvider.GetConnection())
            {
                await connection.OpenAsync();

                var countCommand = new SqlCommand("SELECT COUNT(Id) FROM Installments WHERE SimulationId = @SimId", connection);
                countCommand.Parameters.AddWithValue("@SimId", simulationId);
                totalCount = (int)await countCommand.ExecuteScalarAsync();

                if (totalCount == 0)

                    return new PaginatedInstallmentResult
                    {
                        Items = items,
                        TotalCount = 0,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };

                var datacommand = new SqlCommand(
                    @"SELECT * FROM Installments 
                      WHERE SimulationId = @SimId 
                      ORDER BY InstallmentNumber 
                      OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", connection);

                datacommand.Parameters.AddWithValue("@SimId", simulationId);
                datacommand.Parameters.AddWithValue("@PageSize", pageSize);
                datacommand.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);

                using (var reader = await datacommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {

                        DateTime rawDate = (DateTime)reader["DueDate"];
                        DateTime safeDate = rawDate.Date.AddHours(12);

                        items.Add(new InstallmentDto
                        {
                            Id = (int)reader["Id"],
                            SimulationId = (int)reader["SimulationId"],
                            InstallmentNumber = (int)reader["InstallmentNumber"],
                            OriginalAmount = (decimal)reader["OriginalAmount"],
                            Status = reader["Status"] as string,
                            DueDate = safeDate,
                            PaidAmount = reader.IsDBNull(reader.GetOrdinal("PaidAmount")) ? (decimal?)null : (decimal)reader["PaidAmount"],
                            PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? (DateTime?)null : (DateTime)reader["PaymentDate"],
                            OpeningBalance = reader["OpeningBalance"] != DBNull.Value ? (decimal)reader["OpeningBalance"] : 0,
                            Interest = reader["Interest"] != DBNull.Value ? (decimal)reader["Interest"] : 0,
                            Amortization = reader["Amortization"] != DBNull.Value ? (decimal)reader["Amortization"] : 0,
                            Balance = reader["Balance"] != DBNull.Value ? (decimal)reader["Balance"] : 0
                        });
                    }
                }
            }
            return new PaginatedInstallmentResult
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task DeleteBySimulationIdAsync(int simulationId)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            var command = new SqlCommand("DELETE FROM Installments WHERE SimulationId = @SimId", connection);
            command.Parameters.AddWithValue("@SimId", simulationId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<Installment?> GetByIdAsync(int id)
        {
            Installment installment = null;
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT * FROM Installments WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                installment = MapReaderToInstallment(reader);
            }
            return installment;
        }

        public async Task UpdateAsync(Installment installment)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(
               @"UPDATE Installments 
                  SET Status = @Status, 
                      PaidAmount = @PaidAmount, 
                      PaymentDate = @PaymentDate,
                      OpeningBalance = @OpeningBalance,
                      Interest = @Interest,
                      Amortization = @Amort,
                      Balance = @Balance
                  WHERE Id = @Id", connection);

            command.Parameters.AddWithValue("@Id", installment.Id);
            command.Parameters.AddWithValue("@Status", installment.Status);

            command.Parameters.AddWithValue("@PaidAmount", (object)installment.PaidAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("@PaymentDate", (object)installment.PaymentDate ?? DBNull.Value);

            command.Parameters.AddWithValue("@OpeningBalance", installment.OpeningBalance);
            command.Parameters.AddWithValue("@Interest", installment.Interest);
            command.Parameters.AddWithValue("@Amort", installment.Amortization);
            command.Parameters.AddWithValue("@Balance", installment.Balance);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Installment>> GetFutureInstallmentsAsync(int simulationId, int currentInstallmentNumber)
        {
            var list = new List<Installment>();
            var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(
                @"SELECT * FROM Installments 
                  WHERE SimulationId = @SimId AND InstallmentNumber > @CurrentNum 
                  ORDER BY InstallmentNumber ASC", connection);

            command.Parameters.AddWithValue("@SimId", simulationId);
            command.Parameters.AddWithValue("@CurrentNum", currentInstallmentNumber);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                {
                    list.Add(MapReaderToInstallment(reader));
                }
                return list;
            }

        public async Task UpdateBulkAsync(List<Installment> installments)
        {
            if (installments == null || installments.Count == 0) return;

            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var inst in installments)
                {
                    var command = new SqlCommand(
                        @"UPDATE Installments 
                          SET OpeningBalance = @Open, 
                              Interest = @Int, 
                              Amortization = @Amort, 
                              Balance = @Bal, 
                              OriginalAmount = @Orig -- Importante: O valor da parcela pode mudar dependendo da regra de amortização
                          WHERE Id = @Id", connection, transaction);

                    command.Parameters.AddWithValue("@Id", inst.Id);
                    command.Parameters.AddWithValue("@Open", inst.OpeningBalance);
                    command.Parameters.AddWithValue("@Int", inst.Interest);
                    command.Parameters.AddWithValue("@Amort", inst.Amortization);
                    command.Parameters.AddWithValue("@Bal", inst.Balance);
                    command.Parameters.AddWithValue("@Orig", inst.OriginalAmount);

                    await command.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private Installment MapReaderToInstallment(SqlDataReader reader)
        {
            return new Installment
            {
                Id = (int)reader["Id"],
                SimulationId = (int)reader["SimulationId"],
                InstallmentNumber = (int)reader["InstallmentNumber"],
                OriginalAmount = (decimal)reader["OriginalAmount"],
                Status = reader["Status"] as string,
                DueDate = (DateTime)reader["DueDate"],

                PaidAmount = reader.IsDBNull(reader.GetOrdinal("PaidAmount")) ? null : (decimal)reader["PaidAmount"],
                PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? null : (DateTime)reader["PaymentDate"],

                OpeningBalance = reader["OpeningBalance"] != DBNull.Value ? (decimal)reader["OpeningBalance"] : 0,
                Interest = reader["Interest"] != DBNull.Value ? (decimal)reader["Interest"] : 0,
                Amortization = reader["Amortization"] != DBNull.Value ? (decimal)reader["Amortization"] : 0,
                Balance = reader["Balance"] != DBNull.Value ? (decimal)reader["Balance"] : 0
            };
        }
    }
}
