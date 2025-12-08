using CadastroCliente.Models.DTOs;
using CadastroCliente.Models.Entities;
using CadastroCliente.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CadastroCliente.Services
{
    public class RateTableService : IRateTableService
    {
        private readonly IRateTableRepository _rateTableRepository;

        public RateTableService(IRateTableRepository rateTableRepository)
        {
            _rateTableRepository = rateTableRepository;
        }

        public async Task<IEnumerable<RateTableDto>> GetActiveForSimulationAsync()
        {
            var entities = await _rateTableRepository.GetAllActiveAsync();

            return entities.Select(e => new RateTableDto
            {
                Id = e.Id,
                Name = e.Name,
                MinInstallmentAmount = e.MinInstallmentAmount,
                MaxInstallmentAmount = e.MaxInstallmentAmount,
                IsActive = e.IsActive,
                AvailableRates = e.GetRatesList(),
                AvailableTerms = e.GetTermsList()
            }).ToList();
        }

        public async Task<RateTableDto> CreateAsync(RateTableInputDto dto, string loggedInUser)
        {
            if (dto.MaxInstallmentAmount < dto.MinInstallmentAmount)
            {
                throw new System.ArgumentException("Valor máximo não pode ser menor que o mínimo.");
            }

            var entity = new RateTable
            {
                Name = dto.Name,
                MinInstallmentAmount = dto.MinInstallmentAmount,
                MaxInstallmentAmount = dto.MaxInstallmentAmount,
                IsActive = dto.IsActive,
                AvailableRates = JsonSerializer.Serialize(dto.AvailableRates ?? new List<decimal>()),
                AvailableTerms = JsonSerializer.Serialize(dto.AvailableTerms ?? new List<int>())
            };

            var savedEntity = await _rateTableRepository.CreateAsync(entity, loggedInUser);

            return new RateTableDto
            {
                Id = savedEntity.Id,
                Name = savedEntity.Name,
                MinInstallmentAmount = savedEntity.MinInstallmentAmount,
                MaxInstallmentAmount = savedEntity.MaxInstallmentAmount,
                IsActive = savedEntity.IsActive,
                AvailableRates = savedEntity.GetRatesList(),
                AvailableTerms = savedEntity.GetTermsList()
            };
        }
    }
}
