using CadastroCliente.Helpers;
using CadastroCliente.Models.DTOs.Shared;
using CadastroCliente.Models.Entities;
using CadastroCliente.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CadastroCliente.Services
{
    public class UserService
    {
        private readonly UserRepository _userRepository;

        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // --- MÉTODOS DE LEITURA ---
        public Task<PagedResult<User>> GetAllAsync(string term, int pageNumber, int pageSize, string statusFilter, string roleFilter)
        {
            return _userRepository.GetAllAsync(term, pageNumber, pageSize, statusFilter, roleFilter);
        }

        public Task<List<ChartDataPoint>> GetUserRegistrationsByDayAsync()
        {
            return _userRepository.GetUserRegistrationsByDayAsync();
        }

        public Task<User?> GetByIdAsync(int id)
        {
            return _userRepository.GetByIdAsync(id);
        }

        // --- MÉTODOS DE ESCRITA (COM LÓGICA DE NEGÓCIO) ---

        public Task<User> AddAsync(User user, string loggedInUser)
        {
            return _userRepository.AddAsync(user, loggedInUser);
        }

        public async Task UpdateAsync(int id, User userFromRequest, string loggedInUser)
        {
            var userFromDb = await _userRepository.GetByIdAsync(id);
            if (userFromDb == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            userFromDb.Name = userFromRequest.Name;
            userFromDb.BirthDate = userFromRequest.BirthDate;
            userFromDb.Email = userFromRequest.Email;
            userFromDb.Cpf = userFromRequest.Cpf;
            userFromDb.PhoneNumber = userFromRequest.PhoneNumber;
            userFromDb.Role = userFromRequest.Role;
            userFromDb.Address = userFromRequest.Address;

            if (!string.IsNullOrEmpty(userFromRequest.Password))
            {
                userFromDb.Password = SecurityHelper.ComputeSha256Hash(userFromRequest.Password);
            }

            if (userFromDb.RecordStatus != userFromRequest.RecordStatus)
            {
                userFromDb.RecordStatus = userFromRequest.RecordStatus;
            }

            await _userRepository.UpdateAsync(userFromDb, loggedInUser);
        }

        public Task DeleteAsync(int id, string loggedInUser)
        {
            return _userRepository.DeleteAsync(id, loggedInUser);
        }

        public Task ReactivateAsync(int id, string loggedInUser)
        {
            return _userRepository.ReactivateAsync(id, loggedInUser);
        }
    }
}