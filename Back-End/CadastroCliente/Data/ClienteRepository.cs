using CadastroCliente.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CadastroCliente.Data
{
    public class ClienteRepository
    {
        private readonly SqlConnectionProvider _connectionProvider;
        private readonly AdressRepository _adressRepository;
        private readonly ClienteLogRepository _logRepository;

        public ClienteRepository(SqlConnectionProvider connectionProvider, AdressRepository adressRepository, ClienteLogRepository logRepository)
        {
            _connectionProvider = connectionProvider;
            _adressRepository = adressRepository;
            _logRepository = logRepository;
        }

        // LISTAR TODOS OS CLIENTES ATIVOS
        public async Task<List<Cliente>> GetAllAsync(string termo = "", int status = 1)
        {
            var clientes = new List<Cliente>();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            string query = @"SELECT Id, Name, Cpf, Email, Telefone, Senha, RecordStatus, Role FROM Clientes WHERE RecordStatus = @Status";
            if (!string.IsNullOrWhiteSpace(termo))
            {
                query += @" AND (Name LIKE @Termo OR Cpf LIKE @Termo OR Email LIKE @Termo OR Telefone LIKE @Termo)";
            }
            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Status", status);
            if (!string.IsNullOrWhiteSpace(termo))
                command.Parameters.AddWithValue("@Termo", $"%{termo}%");
            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                clientes.Add(MapClienteFromReader(reader));
            }
            return clientes;
        }

        // LISTAR TODOS OS CLIENTES INATIVOS
        public async Task<List<Cliente>> GetInativosAsync(string termo = "")
        {
            var clientes = new List<Cliente>();
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            string query = "SELECT Id, Name, Cpf, Email, Telefone, Senha, RecordStatus, Role FROM Clientes WHERE RecordStatus = 0";
            if (!string.IsNullOrWhiteSpace(termo))
            {
                query += @" AND (Name LIKE @Termo OR Cpf LIKE @Termo OR Email LIKE @Termo OR Telefone LIKE @Termo)";
            }
            var command = new SqlCommand(query, connection);
            if (!string.IsNullOrWhiteSpace(termo))
                command.Parameters.AddWithValue("@Termo", $"%{termo}%");
            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                clientes.Add(MapClienteFromReader(reader));
            }
            return clientes;
        }

        // BUSCAR POR ID
        public async Task<Cliente?> GetByIdAsync(int id)
        {
            Cliente? cliente = null;
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(@"
        SELECT Id, Name, Cpf, Email, Telefone, Senha, RecordStatus, Role,
               DataCadastro, UsuarioCadastro,
               DataUltimaAlteracao, UsuarioUltimaAlteracao,
               DataInativacao, UsuarioInativacao
        FROM Clientes WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                cliente = MapClienteFromReader(reader);

                cliente.DataCadastro = reader.IsDBNull(reader.GetOrdinal("DataCadastro")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DataCadastro"));
                cliente.UsuarioCadastro = reader.IsDBNull(reader.GetOrdinal("UsuarioCadastro")) ? null : reader.GetString(reader.GetOrdinal("UsuarioCadastro"));
                cliente.DataUltimaAlteracao = reader.IsDBNull(reader.GetOrdinal("DataUltimaAlteracao")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DataUltimaAlteracao"));
                cliente.UsuarioUltimaAlteracao = reader.IsDBNull(reader.GetOrdinal("UsuarioUltimaAlteracao")) ? null : reader.GetString(reader.GetOrdinal("UsuarioUltimaAlteracao"));
                cliente.DataInativacao = reader.IsDBNull(reader.GetOrdinal("DataInativacao")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DataInativacao"));
                cliente.UsuarioInativacao = reader.IsDBNull(reader.GetOrdinal("UsuarioInativacao")) ? null : reader.GetString(reader.GetOrdinal("UsuarioInativacao"));

                cliente.Enderecos = await _adressRepository.GetByClienteIdAsync(id);
                cliente.Logs = await _logRepository.GetLogsByClienteIdAsync(id);
            }
            return cliente;
        }

        // BUSCAR POR EMAIL
        public async Task<Cliente?> GetByEmailAsync(string email)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            var command = new SqlCommand("SELECT Id, Name, Cpf, Email, Telefone, Senha, RecordStatus, Role FROM Clientes WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);
            var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapClienteFromReader(reader);
            }
            return null;
        }

        // LOGIN (apenas cliente ativo)
        public async Task<Cliente?> LoginAsync(string email, string senha)
        {
            string senhaHash = SecurityHelper.ComputeSha256Hash(senha);
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();


            var command = new SqlCommand(@"
        SELECT Id, Name, Cpf, Email, Telefone, Role, RecordStatus, Senha 
        FROM Clientes
        WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Cliente
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                    Cpf = reader.IsDBNull(reader.GetOrdinal("Cpf")) ? string.Empty : reader.GetString(reader.GetOrdinal("Cpf")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                    Senha = reader.IsDBNull(reader.GetOrdinal("Senha")) ? string.Empty : reader.GetString(reader.GetOrdinal("Senha")),
                    Telefone = reader.IsDBNull(reader.GetOrdinal("Telefone")) ? string.Empty : reader.GetString(reader.GetOrdinal("Telefone")),
                    Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? "user" : reader.GetString(reader.GetOrdinal("Role")),
                    RecordStatus = reader.GetBoolean(reader.GetOrdinal("RecordStatus"))
                };
            }
            return null;
        }

        // INSERIR NOVO CLIENTE (Status ativo = 1 por padrão)
        public async Task<Cliente> AddAsync(Cliente cliente, string usuarioLogado)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            cliente.DataCadastro = DateTime.UtcNow;
            cliente.UsuarioCadastro = usuarioLogado;

            try
            {
                var commandCliente = new SqlCommand(@"
                    INSERT INTO Clientes (Name, Cpf, Email, Telefone, Senha, RecordStatus, Role, DataCadastro, UsuarioCadastro)
                    OUTPUT INSERTED.Id
                    VALUES (@Name, @Cpf, @Email, @Telefone, @Senha, 1, @Role, @DataCadastro, @UsuarioCadastro);", connection, transaction);

                commandCliente.Parameters.AddWithValue("@Name", cliente.Name);
                commandCliente.Parameters.AddWithValue("@Cpf", cliente.Cpf);
                commandCliente.Parameters.AddWithValue("@Email", cliente.Email);
                commandCliente.Parameters.AddWithValue("@Telefone", cliente.Telefone);
                commandCliente.Parameters.AddWithValue("@Senha", SecurityHelper.ComputeSha256Hash(cliente.Senha));
                commandCliente.Parameters.AddWithValue("@Role", cliente.Role);
                commandCliente.Parameters.AddWithValue("@DataCadastro", cliente.DataCadastro);
                commandCliente.Parameters.AddWithValue("@UsuarioCadastro", cliente.UsuarioCadastro);

                var novoClienteId = (int)await commandCliente.ExecuteScalarAsync();
                cliente.Id = novoClienteId;

                foreach (var endereco in cliente.Enderecos)
                {
                    endereco.ClienteId = novoClienteId;
                    await _adressRepository.InsertAsync(endereco, connection, transaction);
                }

                var log = new ClienteLog()
                {
                    ClienteId = novoClienteId,
                    DataAlteracao = DateTime.UtcNow,
                    UsuarioAlteracao = usuarioLogado,
                    Acao = "Criação de Cliente"
                };
                await _logRepository.AddLogAsync(log, connection, transaction);

                await transaction.CommitAsync();
                cliente.Senha = null;
                return cliente;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ATUALIZAR CLIENTE
        public async Task UpdateAsync(Cliente cliente, string usuarioLogado)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            cliente.DataUltimaAlteracao = DateTime.UtcNow;
            cliente.UsuarioUltimaAlteracao = usuarioLogado;

            try
            {
                var commandCliente = new SqlCommand(@"
                    UPDATE Clientes SET 
                        Name = @Name, Cpf = @Cpf, Email = @Email, Telefone = @Telefone, 
                        Senha = @Senha, Role = @Role, RecordStatus = @RecordStatus, DataUltimaAlteracao = @DataUltimaAlteracao, UsuarioUltimaAlteracao = @UsuarioUltimaAlteracao
                    WHERE Id = @Id", connection, transaction);

                commandCliente.Parameters.AddWithValue("@Name", (object)cliente.Name ?? DBNull.Value);
                commandCliente.Parameters.AddWithValue("@Cpf", (object)cliente.Cpf ?? DBNull.Value);
                commandCliente.Parameters.AddWithValue("@Email", (object)cliente.Email ?? DBNull.Value);
                commandCliente.Parameters.AddWithValue("@Telefone", (object)cliente.Telefone ?? DBNull.Value);
                commandCliente.Parameters.AddWithValue("@Senha", (object)cliente.Senha ?? DBNull.Value);
                commandCliente.Parameters.AddWithValue("@Role", (object)cliente.Role ?? DBNull.Value);
                commandCliente.Parameters.AddWithValue("@RecordStatus", cliente.RecordStatus);
                commandCliente.Parameters.AddWithValue("@Id", cliente.Id);
                commandCliente.Parameters.AddWithValue("@DataUltimaAlteracao", cliente.DataUltimaAlteracao);
                commandCliente.Parameters.AddWithValue("@UsuarioUltimaAlteracao", cliente.UsuarioUltimaAlteracao);

                await commandCliente.ExecuteNonQueryAsync();

                await _adressRepository.DeleteByClienteIdAsync(cliente.Id, connection, transaction);

                foreach (var endereco in cliente.Enderecos)
                {
                    endereco.ClienteId = cliente.Id;
                    await _adressRepository.InsertAsync(endereco, connection, transaction);
                }

                var log = new ClienteLog()
                {
                    ClienteId = cliente.Id,
                    DataAlteracao = DateTime.UtcNow,
                    UsuarioAlteracao = usuarioLogado,
                    Acao = "Atualização de Dados"
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
        public async Task ReativarAsync(int id)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            var command = new SqlCommand("UPDATE Clientes SET RecordStatus = 1 WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }

        // EXCLUSÃO LÓGICA
        public async Task DeleteAsync(int id, string usuarioLogado)
        {
            using var connection = _connectionProvider.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Inativa o cliente e preenche os campos de log
                var command = new SqlCommand(@"
                    UPDATE Clientes SET 
                        RecordStatus = 0,
                        DataInativacao = @DataInativacao,
                        UsuarioInativacao = @UsuarioInativacao
                    WHERE Id = @Id", connection, transaction);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@DataInativacao", DateTime.UtcNow);
                command.Parameters.AddWithValue("@UsuarioInativacao", usuarioLogado);
                await command.ExecuteNonQueryAsync();

                var log = new ClienteLog
                {
                    ClienteId = id,
                    DataAlteracao = DateTime.UtcNow,
                    UsuarioAlteracao = usuarioLogado,
                    Acao = "Cliente Inativado"
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

        private Cliente MapClienteFromReader(SqlDataReader reader)
        {
            return new Cliente
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                Cpf = reader.IsDBNull(reader.GetOrdinal("Cpf")) ? string.Empty : reader.GetString(reader.GetOrdinal("Cpf")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                Telefone = reader.IsDBNull(reader.GetOrdinal("Telefone")) ? string.Empty : reader.GetString(reader.GetOrdinal("Telefone")),
                Senha = reader.IsDBNull(reader.GetOrdinal("Senha")) ? string.Empty : reader.GetString(reader.GetOrdinal("Senha")),
                RecordStatus = reader.GetBoolean(reader.GetOrdinal("RecordStatus")),
                Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? string.Empty : reader.GetString(reader.GetOrdinal("Role"))
            };
        }
    }
}
