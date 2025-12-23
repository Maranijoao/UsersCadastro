using CadastroCliente.Models.DTOs.Dashboard;
using CadastroCliente.Models.Entities;
using CadastroCliente.Repositories.Data;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CadastroCliente.Repositories
{
    public class SimulationRepository : ISimulationRepository
    {
        private readonly SqlConnectionProvider _connectionProvider;
        private readonly UserLogRepository _logRepository;

        public SimulationRepository(SqlConnectionProvider connectionProvider, UserLogRepository logRepository)
        {
            _connectionProvider = connectionProvider;
            _logRepository = logRepository;
        }

        public async Task<Simulation> AddAsync(Simulation simulation, string loggedInUser, int userId)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                simulation.CreatedAt = DateTime.UtcNow;
                simulation.CreatedBy = loggedInUser;

                var sql = @"
                    INSERT INTO Simulations (
                        Product, RateTable, Rate, Term, InstallmentAmount, ReleasedAmount, ContractValue, 
                        TotalFinancedAmount, IOFFinanced, HasGracePeriod, GracePeriodDays, FrequencyDays, TotalIOF,
                        
                        -- Campos de Negócio (Seguro e TAC)
                        IncludeInsurance, InsuranceRate, InsuranceAmount, TacAmount, TacFinanced,
                        
                        -- Campos de Refinanciamento
                        RefinancedFromId, PayoffAmount,

                        SimulationDate, CreatedAt, CreatedBy
                    ) 
                    VALUES (
                        @Product, @RateTable, @Rate, @Term, @InstallmentAmount, @ReleasedAmount, @ContractValue, 
                        @TotalFinancedAmount, @IOFFinanced, @HasGracePeriod, @GracePeriodDays, @FrequencyDays, @TotalIOF,
                        
                        @IncludeInsurance, @InsuranceRate, @InsuranceAmount, @TacAmount, @TacFinanced,
                        
                        @RefinancedFromId, @PayoffAmount,

                        GETUTCDATE(), @CreatedAt, @CreatedBy
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                var command = new SqlCommand(sql, connection, transaction);

                // Dados Básicos
                command.Parameters.AddWithValue("@Product", simulation.Product);
                command.Parameters.AddWithValue("@RateTable", simulation.RateTable);
                command.Parameters.AddWithValue("@Rate", simulation.Rate);
                command.Parameters.AddWithValue("@Term", simulation.Term);
                command.Parameters.AddWithValue("@InstallmentAmount", simulation.InstallmentAmount);
                command.Parameters.AddWithValue("@ReleasedAmount", simulation.ReleasedAmount);
                command.Parameters.AddWithValue("@ContractValue", simulation.ContractValue);
                command.Parameters.AddWithValue("@TotalFinancedAmount", simulation.TotalFinancedAmount);
                command.Parameters.AddWithValue("@IOFFinanced", simulation.IOFFinanced);
                command.Parameters.AddWithValue("@HasGracePeriod", simulation.HasGracePeriod);
                command.Parameters.AddWithValue("@GracePeriodDays", simulation.GracePeriodDays);
                command.Parameters.AddWithValue("@FrequencyDays", simulation.FrequencyDays);
                command.Parameters.AddWithValue("@TotalIOF", simulation.TotalIOF);

                // Serviços
                command.Parameters.AddWithValue("@IncludeInsurance", simulation.IncludeInsurance);
                command.Parameters.AddWithValue("@InsuranceRate", simulation.InsuranceRate);
                command.Parameters.AddWithValue("@InsuranceAmount", simulation.InsuranceAmount);
                command.Parameters.AddWithValue("@TacAmount", simulation.TacAmount);
                command.Parameters.AddWithValue("@TacFinanced", simulation.TacFinanced);

                command.Parameters.AddWithValue("@RefinancedFromId", (object)simulation.RefinancedFromId ?? DBNull.Value);
                command.Parameters.AddWithValue("@PayoffAmount", simulation.PayoffAmount);

                // Auditoria
                command.Parameters.AddWithValue("@CreatedAt", simulation.CreatedAt);
                command.Parameters.AddWithValue("@CreatedBy", simulation.CreatedBy);

                var newId = await command.ExecuteScalarAsync();

                if (newId != null && int.TryParse(newId.ToString(), out int id))
                {
                    simulation.Id = id;
                }
                else
                {
                    throw new Exception("Falha ao gerar ID para a Simulação.");
                }

                var log = new UserLog()
                {
                    UserId = userId,
                    ChangedAt = simulation.CreatedAt,
                    ChangedBy = loggedInUser,
                    Action = $"Simulation Created (ID: {simulation.Id})"
                };
                await _logRepository.AddLogAsync(log, connection, transaction);

                await transaction.CommitAsync();
                return simulation;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Simulation> UpdateAsync(int id, Simulation simulation, string loggedInUser)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(
                @"UPDATE Simulations 
                  SET Product = @Product, 
                      RateTable = @RateTable, 
                      Rate = @Rate, 
                      Term = @Term,
                      InstallmentAmount = @InstallmentAmount, 
                      ReleasedAmount = @ReleasedAmount,
                      ContractValue = @ContractValue, 
                      
                      TotalFinancedAmount = @TotalFinancedAmount,
                      IOFFinanced = @IOFFinanced,
                      HasGracePeriod = @HasGracePeriod,
                      GracePeriodDays = @GracePeriodDays,
                      FrequencyDays = @FrequencyDays,
                      TotalIOF = @TotalIOF,

                      IncludeInsurance = @IncludeInsurance,
                      InsuranceRate = @InsuranceRate,
                      InsuranceAmount = @InsuranceAmount,
                      TacAmount = @TacAmount,
                      TacFinanced = @TacFinanced,

                      -- Atualiza Refinanciamento também se mudar
                      RefinancedFromId = @RefinancedFromId,
                      PayoffAmount = @PayoffAmount,

                      SimulationDate = GETUTCDATE()
                  WHERE Id = @Id",
                connection);

            command.Parameters.AddWithValue("@Id", simulation.Id);
            command.Parameters.AddWithValue("@Product", simulation.Product);
            command.Parameters.AddWithValue("@RateTable", simulation.RateTable);
            command.Parameters.AddWithValue("@Rate", simulation.Rate);
            command.Parameters.AddWithValue("@Term", simulation.Term);
            command.Parameters.AddWithValue("@InstallmentAmount", simulation.InstallmentAmount);
            command.Parameters.AddWithValue("@ReleasedAmount", simulation.ReleasedAmount);
            command.Parameters.AddWithValue("@ContractValue", simulation.ContractValue);
            command.Parameters.AddWithValue("@TotalFinancedAmount", simulation.TotalFinancedAmount);
            command.Parameters.AddWithValue("@IOFFinanced", simulation.IOFFinanced);
            command.Parameters.AddWithValue("@HasGracePeriod", simulation.HasGracePeriod);
            command.Parameters.AddWithValue("@GracePeriodDays", simulation.GracePeriodDays);
            command.Parameters.AddWithValue("@FrequencyDays", simulation.FrequencyDays);
            command.Parameters.AddWithValue("@TotalIOF", simulation.TotalIOF);

            command.Parameters.AddWithValue("@IncludeInsurance", simulation.IncludeInsurance);
            command.Parameters.AddWithValue("@InsuranceRate", simulation.InsuranceRate);
            command.Parameters.AddWithValue("@InsuranceAmount", simulation.InsuranceAmount);
            command.Parameters.AddWithValue("@TacAmount", simulation.TacAmount);
            command.Parameters.AddWithValue("@TacFinanced", simulation.TacFinanced);

            command.Parameters.AddWithValue("@RefinancedFromId", (object)simulation.RefinancedFromId ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayoffAmount", simulation.PayoffAmount);

            await command.ExecuteNonQueryAsync();
            return simulation;
        }

        public async Task<Simulation?> GetByIdAsync(int id)
        {
            Simulation? simulation = null;
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var sql = @"
                SELECT s.*,
                       (SELECT TOP 1 Id FROM Simulations child WHERE child.RefinancedFromId = s.Id) as RefinancedToId
                FROM Simulations s 
                WHERE s.Id = @Id";

            var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                decimal releasedAmount = (decimal)reader["ReleasedAmount"];
                decimal totalFinanced = reader["TotalFinancedAmount"] != DBNull.Value ? (decimal)reader["TotalFinancedAmount"] : 0;
                decimal totalIOF = reader["TotalIOF"] != DBNull.Value ? (decimal)reader["TotalIOF"] : 0;

                simulation = new Simulation()
                {
                    Id = (int)reader["Id"],
                    Product = reader["Product"] as string,
                    RateTable = reader["RateTable"] as string,
                    Rate = (decimal)reader["Rate"],
                    Term = (int)reader["Term"],
                    InstallmentAmount = (decimal)reader["InstallmentAmount"],
                    ReleasedAmount = (decimal)reader["ReleasedAmount"],
                    ContractValue = (decimal)reader["ContractValue"],
                    SimulationDate = (DateTime)reader["SimulationDate"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    CreatedBy = reader["CreatedBy"] as string,
                    TotalFinancedAmount = totalFinanced,
                    IOFFinanced = reader["IOFFinanced"] != DBNull.Value && (bool)reader["IOFFinanced"],
                    HasGracePeriod = reader["HasGracePeriod"] != DBNull.Value && (bool)reader["HasGracePeriod"],
                    GracePeriodDays = reader["GracePeriodDays"] != DBNull.Value ? (int)reader["GracePeriodDays"] : 0,
                    FrequencyDays = reader["FrequencyDays"] != DBNull.Value ? (int)reader["FrequencyDays"] : 30,
                    TotalIOF = totalIOF,
                    GracePeriodInterest = totalFinanced - releasedAmount - totalIOF,

                    IncludeInsurance = reader["IncludeInsurance"] != DBNull.Value && (bool)reader["IncludeInsurance"],
                    InsuranceRate = reader["InsuranceRate"] != DBNull.Value ? (decimal)reader["InsuranceRate"] : 0,
                    InsuranceAmount = reader["InsuranceAmount"] != DBNull.Value ? (decimal)reader["InsuranceAmount"] : 0,
                    TacAmount = reader["TacAmount"] != DBNull.Value ? (decimal)reader["TacAmount"] : 0,
                    TacFinanced = reader["TacFinanced"] != DBNull.Value && (bool)reader["TacFinanced"],

                    // Mapeamento Refinanciamento
                    RefinancedFromId = reader["RefinancedFromId"] != DBNull.Value ? (int)reader["RefinancedFromId"] : null,
                    PayoffAmount = reader["PayoffAmount"] != DBNull.Value ? (decimal)reader["PayoffAmount"] : 0,

                    // Mapeia o resultado da subquery
                    RefinancedToId = reader["RefinancedToId"] != DBNull.Value ? Convert.ToInt32(reader["RefinancedToId"]) : null
                };
            }
            return simulation;
        }

        public async Task<PagedSimulationResult> GetAllAsync(string term, int pageNumber, int pageSize)
        {
            var items = new List<SimulationListItemDto>();
            int totalCount = 0;

            string baseQuery = "FROM Simulations s";
            string whereClause = "";
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(term))
            {
                whereClause = " WHERE (s.Product LIKE @Term OR s.CreatedBy LIKE @TERM OR s.CreatedBy LIKE @Term)";
                parameters[@"Term"] = $"%{term}%";
            }

            using (var connection = _connectionProvider.GetConnection())
            {
                await connection.OpenAsync();
                string countQueyry = $"SELECT COUNT(s.Id)" + baseQuery + whereClause;
                var countCommand = new SqlCommand(countQueyry, connection);
                foreach (var p in parameters) countCommand.Parameters.AddWithValue(p.Key, p.Value);
                totalCount = (int)await countCommand.ExecuteScalarAsync();
            }

            using (var connection = _connectionProvider.GetConnection())
            {
                await connection.OpenAsync();
                string dataQuery = $@"
                SELECT 
                s.Id, s.Product, s.InstallmentAmount, s.ReleasedAmount, s.ContractValue, s.SimulationDate, s.CreatedAt, s.CreatedBy
                {baseQuery}
                {whereClause}
                ORDER BY s.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                var dataCommand = new SqlCommand(dataQuery, connection);
                foreach (var p in parameters)
                    dataCommand.Parameters.AddWithValue(p.Key, p.Value);
                dataCommand.Parameters.AddWithValue("@PageSize", pageSize);
                dataCommand.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);

                using (var reader = await dataCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new SimulationListItemDto()
                        {
                            Id = (int)reader["Id"],
                            Product = reader["Product"] as string,
                            ReleasedAmount = (decimal)reader["ReleasedAmount"],
                            InstallmentAmount = (decimal)reader["InstallmentAmount"],
                            ContractValue = (decimal)reader["ContractValue"],
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            CreatedBy = reader["CreatedBy"] as string
                        });
                    }
                }
            }
            return new PagedSimulationResult
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<DashboardTotalsDtos> GetDashboardTotalsAsync()
        {
            var totals = new DashboardTotalsDtos();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT ISNULL(SUM(ReleasedAmount), 0) as TotalReleased, ISNULL(SUM(ContractValue), 0) as TotalContract FROM Simulations", connection);

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    totals.TotalReleasedAmount = (decimal)reader["TotalReleased"];
                    totals.TotalContractValue = (decimal)reader["TotalContract"];
                }
            }
            return totals;
        }
    }
}