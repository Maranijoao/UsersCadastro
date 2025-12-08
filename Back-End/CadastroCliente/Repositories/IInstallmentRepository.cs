using CadastroCliente.Models.DTOs;
using CadastroCliente.Models.DTOs.Installment;
using CadastroCliente.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CadastroCliente.Repositories
{
    public interface IInstallmentRepository
    {
        Task AddBulkAsync(List<Installment> installments);

        Task<PaginatedInstallmentResult> GetBySimulationIdAsync(int simulationId, int pageNumber, int pageSize);

        Task DeleteBySimulationIdAsync(int simulationId);

        Task<Installment> GetByIdAsync(int id);

        Task UpdateAsync(Installment installment);

        Task<List<Installment>> GetFutureInstallmentsAsync(int simulationId, int currentInstallmentNumber);

        Task UpdateBulkAsync(List<Installment> installments);
    }
}