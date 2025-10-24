using CadastroCliente.Helpers;
using CadastroCliente.Models.DTOs.Shared;
using CadastroCliente.Models.Entities;
using CadastroCliente.Repositories.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CadastroCliente.Repositories
{
    public class UserRepository
    {
        private readonly SqlConnectionProvider _connectionProvider;
        private readonly AddressRepository _addressRepository;
        private readonly UserLogRepository _logRepository;

        public UserRepository(SqlConnectionProvider connectionProvider, AddressRepository addressRepository, UserLogRepository logRepository)
        {
            _connectionProvider = connectionProvider;
            _addressRepository = addressRepository;
            _logRepository = logRepository;
        }

        // LISTAR TODOS OS USERS 
        public async Task<PagedResult<User>> GetAllAsync(
    string term = "",
    int pageNumber = 1,
    int pageSize = 10,
    string statusFilter = "all",
    string roleFilter = "all")
        {
            var users = new List<User>();
            int totalCount = 0;

            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var whereConditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(term))
            {
                whereConditions.Add("(Name LIKE @Term OR Email LIKE @Term OR Cpf LIKE @Term)");
                parameters["@Term"] = $"%{term}%";
            }

            if (!string.Equals(roleFilter, "all", StringComparison.OrdinalIgnoreCase))
            {
                whereConditions.Add("Role = @RoleFilter");
                parameters["@RoleFilter"] = roleFilter;
            }

            if (!string.Equals(statusFilter, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(statusFilter, out bool isActive))
                {
                    whereConditions.Add("RecordStatus = @StatusFilter");
                    parameters["@StatusFilter"] = isActive;
                }
            }

            string whereClause = whereConditions.Any() ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";

            // --- Consulta para contagem total ---
            string countQuery = $"SELECT COUNT(*) FROM Users {whereClause}";
            var countCommand = new SqlCommand(countQuery, connection);
            foreach (var p in parameters)
            {
                countCommand.Parameters.AddWithValue(p.Key, p.Value);
            }
            totalCount = (int)await countCommand.ExecuteScalarAsync();

            // --- Consulta para buscar os dados paginados ---
            string dataQuery = $@"
        SELECT Id, Name, Cpf, Email, PhoneNumber, RecordStatus, Role, BirthDate
        FROM Users
        {whereClause}
        ORDER BY Name
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

            var dataCommand = new SqlCommand(dataQuery, connection);
            foreach (var p in parameters)
            {
                dataCommand.Parameters.AddWithValue(p.Key, p.Value);
            } 
            dataCommand.Parameters.AddWithValue("@PageSize", pageSize);
            dataCommand.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);

            using (var reader = await dataCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    users.Add(MapUserFromReader(reader, includePassword: false));
                }
            }

            return new PagedResult<User>
            {
                Items = users,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // BUSCAR POR ID
        public async Task<User?> GetByIdAsync(int id)
        {
            User? user = null;
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(@"
            SELECT Id, Name, Cpf, Email, PhoneNumber, Password, RecordStatus, Role, BirthDate,
                   CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, InactivatedAt, InactivatedBy
            FROM Users WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                user = MapUserFromReader(reader, includePassword: true);

                user.CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                user.CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy"));
                user.UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
                user.UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetString(reader.GetOrdinal("UpdatedBy"));
                user.InactivatedAt = reader.IsDBNull(reader.GetOrdinal("InactivatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("InactivatedAt"));
                user.InactivatedBy = reader.IsDBNull(reader.GetOrdinal("InactivatedBy")) ? null : reader.GetString(reader.GetOrdinal("InactivatedBy"));

                user.Address = await _addressRepository.GetByUserIdAsync(id);
                user.Logs = await _logRepository.GetLogsByUserIdAsync(id);
            }
            return user;
        }

        public async Task<List<ChartDataPoint>> GetUserRegistrationsByDayAsync()
        {
            var chartData = new List<ChartDataPoint>();

            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            string query = @"
                SELECT CAST(CreatedAt AS DATE) as RegistrationDay, COUNT(Id) as UserCount
                FROM Users
                WHERE CreatedAt IS NOT NULL
                GROUP BY CAST(CreatedAt AS DATE)
                ORDER BY RegistrationDay ASC";

            var command = new SqlCommand(query, connection);
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    chartData.Add(new ChartDataPoint
                    {
                        X = reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                        Y = reader.GetInt32(1)
                    });
                }
            }
            return chartData;
        }

        // BUSCAR POR EMAIL
        public async Task<User?> GetByEmailAsync(string email)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT Id, Name, Cpf, Email, PhoneNumber, Password, RecordStatus, Role, BirthDate FROM Users WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);

            var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUserFromReader(reader, includePassword: true);
            }
            return null;
        }

        public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            var command = new SqlCommand("UPDATE Users SET Password = @Password, UpdatedAt = @UpdatedAt WHERE Id = @Id", connection);

            command.Parameters.AddWithValue("@Password", newPasswordHash);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Id", userId);

            await command.ExecuteNonQueryAsync();
        }

        //LOGIN(apenas user ativo)
        public async Task<User?> LoginAsync(string email, string password)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(@"
        SELECT Id, Name, Cpf, Email, PhoneNumber, Role, RecordStatus, Password, BirthDate
        FROM users
        WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUserFromReader(reader, includePassword: true);
            }
            return null;
        }

        // INSERIR NOVO user (Status ativo = 1 por padrão)
        public async Task<User> AddAsync(User user, string loggedInUser)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            user.CreatedAt = DateTime.UtcNow;
            user.CreatedBy = loggedInUser;

            try
            {
                var commandUser = new SqlCommand(@"
                    INSERT INTO Users (Name, Cpf, Email, PhoneNumber, Password, RecordStatus, Role, CreatedAt, CreatedBy, BirthDate)
                    OUTPUT INSERTED.Id
                    VALUES (@Name, @Cpf, @Email, @PhoneNumber, @Password, 1, @Role, @CreatedAt, @CreatedBy, @BirthDate);", connection, transaction);

                commandUser.Parameters.AddWithValue("@Name", user.Name);
                commandUser.Parameters.AddWithValue("@Cpf", user.Cpf);
                commandUser.Parameters.AddWithValue("@Email", user.Email);
                commandUser.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                commandUser.Parameters.AddWithValue("@Password", SecurityHelper.ComputeSha256Hash(user.Password));
                commandUser.Parameters.AddWithValue("@Role", user.Role);
                commandUser.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
                commandUser.Parameters.AddWithValue("@CreatedBy", user.CreatedBy);
                commandUser.Parameters.AddWithValue("@BirthDate", (object)user.BirthDate ?? DBNull.Value);

                var newUserId = (int)await commandUser.ExecuteScalarAsync();
                user.Id = newUserId;

                foreach (var address in user.Address)
                {
                    address.UserId = newUserId;
                    await _addressRepository.InsertAsync(address, connection, transaction);
                }

                var log = new UserLog()
                {
                    UserId = newUserId,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = loggedInUser,
                    Action = "Criação de Usuário"
                };
                await _logRepository.AddLogAsync(log, connection, transaction);

                await transaction.CommitAsync();
                user.Password = null;
                return user;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        
        // ATUALIZAR user
        public async Task UpdateAsync(User user, string loggedInUser)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = loggedInUser;

            try
            {
                var commandUser = new SqlCommand(@"
                    UPDATE Users SET 
                        Name = @Name, Cpf = @Cpf, Email = @Email, PhoneNumber = @PhoneNumber, 
                        Password = @Password, Role = @Role, RecordStatus = @RecordStatus, 
                        BirthDate = @BirthDate, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                    WHERE Id = @Id", connection, transaction);

                commandUser.Parameters.AddWithValue("@Name", (object)user.Name ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@Cpf", (object)user.Cpf ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@Email", (object)user.Email ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@PhoneNumber", (object)user.PhoneNumber ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@Password", (object)user.Password ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@RecordStatus", user.RecordStatus);
                commandUser.Parameters.AddWithValue("@Role", (object)user.Role ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@BirthDate", (object)user.BirthDate ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@Id", user.Id);
                commandUser.Parameters.AddWithValue("@UpdatedAt", user.UpdatedAt);
                commandUser.Parameters.AddWithValue("@UpdatedBy", user.UpdatedBy);

                await commandUser.ExecuteNonQueryAsync();

                await _addressRepository.DeleteByUserIdAsync(user.Id, connection, transaction);

                foreach (var address in user.Address)
                {
                    address.UserId = user.Id;
                    await _addressRepository.InsertAsync(address, connection, transaction);
                }

                var log = new UserLog()
                {
                    UserId = user.Id,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = loggedInUser,
                    Action = "Usuário Atualizado"
                };
                await _logRepository.AddLogAsync(log, connection, transaction);

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // REATIVAÇÃO
        public async Task ReactivateAsync(int id, string loggedInUser)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var command = new SqlCommand(@"
                    UPDATE Users SET 
                        RecordStatus = 1,
                        UpdatedAt = @UpdatedAt, 
                        UpdatedBy = @UpdatedBy,
                        InactivatedAt = NULL, 
                        InactivatedBy = NULL
                    WHERE Id = @Id", connection, transaction);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@UpdatedBy", loggedInUser);
                await command.ExecuteNonQueryAsync();

                var log = new UserLog
                {
                    UserId = id,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = loggedInUser,
                    Action = "Usuário Reativado"
                };

                await _logRepository.AddLogAsync(log, connection, transaction);

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // EXCLUSÃO LÓGICA
        public async Task DeleteAsync(int id, string loggedInUser)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // CORREÇÃO: A query UPDATE agora inclui os campos de inativação.
                var command = new SqlCommand(@"
                    UPDATE Users SET 
                        RecordStatus = 0,
                        InactivatedAt = @InactivatedAt,
                        InactivatedBy = @InactivatedBy
                    WHERE Id = @Id", connection, transaction);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@InactivatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@InactivatedBy", loggedInUser);
                await command.ExecuteNonQueryAsync();

                var log = new UserLog
                {
                    UserId = id,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = loggedInUser,
                    Action = "Usuário Inativado"
                };
                await _logRepository.AddLogAsync(log, connection, transaction);

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private User MapUserFromReader(SqlDataReader reader, bool includePassword)
        {
            var user = new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                Cpf = reader.IsDBNull(reader.GetOrdinal("Cpf")) ? string.Empty : reader.GetString(reader.GetOrdinal("Cpf")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                RecordStatus = reader.GetBoolean(reader.GetOrdinal("RecordStatus")),
                Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? string.Empty : reader.GetString(reader.GetOrdinal("Role")),
                BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? (DateOnly?)null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("BirthDate"))),
            };

            // A senha só é lida se o parâmetro for verdadeiro
            if (includePassword)
            {
                user.Password = reader.IsDBNull(reader.GetOrdinal("Password")) ? string.Empty : reader.GetString(reader.GetOrdinal("Password"));
            }

            return user;
        }
    }
}
