using CadastroCliente.Models.DTOs;
using CadastroCliente.Models.DTOs.Installment;
using System.Threading.Tasks;

namespace CadastroCliente.Services
{
    public interface IInstallmentService
    {
        Task<PaginatedInstallmentResult> GetBySimulationIdAsync(int simulationId, int pageNumber, int pageSize);

        Task RegisterPaymentAsync(int installmentId, decimal paidAmount, DateTime paymentDate);
    }
}