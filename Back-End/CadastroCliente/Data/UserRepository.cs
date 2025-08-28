using CadastroCliente.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CadastroCliente.Data
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

        // LISTAR TODOS OS userS ATIVOS
        public async Task<List<User>> GetAllAsync(string term = "", int status = 1)
        {
            var users = new List<User>();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            string query = @"SELECT Id, Name, Cpf, Email, PhoneNumber, Password, RecordStatus, Role FROM Users WHERE RecordStatus = @Status";
            if (!string.IsNullOrWhiteSpace(term))
            {
                query += @" AND (Name LIKE @Term OR Cpf LIKE @Term OR Email LIKE @Term OR PhoneNumber LIKE @Term)";
            }
            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Status", status);
            if (!string.IsNullOrWhiteSpace(term))
                command.Parameters.AddWithValue("@Term", $"%{term}%");
            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapUserFromReader(reader));
            }
            return users;
        }

        // LISTAR TODOS OS users INATIVOS
        public async Task<List<User>> GetInactiveAsync(string term = "")
        {
            var users = new List<User>();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            string query = "SELECT Id, Name, Cpf, Email, PhoneNumber, Password, RecordStatus, Role FROM Users WHERE RecordStatus = 0";
            if (!string.IsNullOrWhiteSpace(term))
            {
                query += @" AND (Name LIKE @Term OR Cpf LIKE @Term OR Email LIKE @Term OR PhoneNumber LIKE @Term)";
            }
            var command = new SqlCommand(query, connection);
            if (!string.IsNullOrWhiteSpace(term))
                command.Parameters.AddWithValue("@Term", $"%{term}%");
            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapUserFromReader(reader));
            }
            return users;
        }

        // BUSCAR POR ID
        public async Task<User?> GetByIdAsync(int id)
        {
            User? user = null;
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(@"
            SELECT Id, Name, Cpf, Email, PhoneNumber, Password, RecordStatus, Role,
                   CreatedAt, CreatedBy,
                   UpdatedAt, UpdatedBy,
                   InactivatedAt, InactivatedBy
            FROM Users WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                user = MapUserFromReader(reader);

                user.CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                user.CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy"));
                user.UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
                user.UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetString(reader.GetOrdinal("UpdatedBy"));
                user.InactivatedAt = reader.IsDBNull(reader.GetOrdinal("InactivatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("InactivatedAt"));
                user.InactivatedBy = reader.IsDBNull(reader.GetOrdinal("InactivatedBy")) ? null : reader.GetString(reader.GetOrdinal("InactivatedBy"));

                user.Address = await _addressRepository.GetByUserIdAsync(id); 
                user.Logs = await _logRepository.GetLogsByUserIdAsync(id);
            }
            return user;
        }

        // BUSCAR POR EMAIL
        public async Task<User?> GetByEmailAsync(string email)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT Id, Name, Cpf, Email, PhoneNumber, Password, RecordStatus, Role FROM Users WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);

            var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUserFromReader(reader);
            }
            return null;
        }

        // LOGIN (apenas user ativo)
        public async Task<User?> LoginAsync(string email, string password)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(@"
        SELECT Id, Name, Cpf, Email, PhoneNumber, Role, RecordStatus, Password 
        FROM users
        WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                    return MapUserFromReader(reader);
                //Id = reader.GetInt32(reader.GetOrdinal("Id")),
                //    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                //    Cpf = reader.IsDBNull(reader.GetOrdinal("Cpf")) ? string.Empty : reader.GetString(reader.GetOrdinal("Cpf")),
                //    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                //    Password = reader.IsDBNull(reader.GetOrdinal("Password")) ? string.Empty : reader.GetString(reader.GetOrdinal("Password")),
                //    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                //    Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? "user" : reader.GetString(reader.GetOrdinal("Role")),
                //    RecordStatus = reader.GetBoolean(reader.GetOrdinal("RecordStatus")
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
                INSERT INTO Users (Name, Cpf, Email, PhoneNumber, Password, RecordStatus, Role, CreatedAt, CreatedBy)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Cpf, @Email, @PhoneNumber, @Password, 1, @Role, @CreatedAt, @CreatedBy);", connection, transaction);

                commandUser.Parameters.AddWithValue("@Name", user.Name);
                commandUser.Parameters.AddWithValue("@Cpf", user.Cpf);
                commandUser.Parameters.AddWithValue("@Email", user.Email);
                commandUser.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                commandUser.Parameters.AddWithValue("@Password", SecurityHelper.ComputeSha256Hash(user.Password));
                commandUser.Parameters.AddWithValue("@Role", user.Role);
                commandUser.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
                commandUser.Parameters.AddWithValue("@CreatedBy", user.CreatedBy);

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
                    Password = @Password, Role = @Role, RecordStatus = @RecordStatus, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id", connection, transaction);


                commandUser.Parameters.AddWithValue("@Name", (object)user.Name ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@Cpf", (object)user.Cpf ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@Email", (object)user.Email ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@PhoneNumber", (object)user.PhoneNumber ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@Password", (object)user.Password ?? DBNull.Value);
                commandUser.Parameters.AddWithValue("@RecordStatus", user.RecordStatus);
                commandUser.Parameters.AddWithValue("@Role", (object)user.Role ?? DBNull.Value);
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
        public async Task ReactivateAsync(int id)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            var command = new SqlCommand("UPDATE users SET RecordStatus = 1 WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }

        // EXCLUSÃO LÓGICA
        public async Task DeleteAsync(int id, string loggedInUser)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Inativa o user e preenche os campos de log
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

        private User MapUserFromReader(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                Cpf = reader.IsDBNull(reader.GetOrdinal("Cpf")) ? string.Empty : reader.GetString(reader.GetOrdinal("Cpf")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                Password = reader.IsDBNull(reader.GetOrdinal("Password")) ? string.Empty : reader.GetString(reader.GetOrdinal("Password")),
                RecordStatus = reader.GetBoolean(reader.GetOrdinal("RecordStatus")),
                Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? string.Empty : reader.GetString(reader.GetOrdinal("Role"))
            };
        }
    }
}
