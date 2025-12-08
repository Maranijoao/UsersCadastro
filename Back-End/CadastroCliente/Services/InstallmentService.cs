using CadastroCliente.Models.DTOs;
using CadastroCliente.Models.DTOs.Installment;
using CadastroCliente.Repositories;
using System.Threading.Tasks;

namespace CadastroCliente.Services
{

    public class InstallmentService : IInstallmentService
    {
        private readonly IInstallmentRepository _repo;
        private readonly ISimulationRepository _simRepo; 
        private readonly ILogger<InstallmentService> _logger;

        public InstallmentService(IInstallmentRepository repo, ISimulationRepository simRepo, ILogger<InstallmentService> logger)
        {
            _repo = repo;
            _simRepo = simRepo;
            _logger = logger;
        }

        public Task<PaginatedInstallmentResult> GetBySimulationIdAsync(int simulationId, int pageNumber, int pageSize)
        {
            return _repo.GetBySimulationIdAsync(simulationId, pageNumber, pageSize);
        }

        public async Task RegisterPaymentAsync(int installmentId, decimal paidAmount, DateTime paymentDate)
        {
            var installment = await _repo.GetByIdAsync(installmentId);
            if (installment == null) throw new Exception("Parcela não encontrada");

            installment.PaidAmount = paidAmount;
            installment.PaymentDate = paymentDate;
            installment.Status = "Paid";

            decimal amortizacaoExtra = 0;
            if (paidAmount > installment.OriginalAmount)
            {
                amortizacaoExtra = paidAmount - installment.OriginalAmount;
            }

            if (amortizacaoExtra > 0)
            {
                installment.Balance -= amortizacaoExtra;
                if (installment.Balance < 0) installment.Balance = 0;
            }

            await _repo.UpdateAsync(installment);

            if (amortizacaoExtra > 0)
            {
                await RecalculateFutureInstallments(installment.SimulationId, installment.InstallmentNumber, installment.Balance);
            }
        }

        private async Task RecalculateFutureInstallments(int simulationId, int currentNumber, decimal startingBalance)
        {
            var futureInstallments = await _repo.GetFutureInstallmentsAsync(simulationId, currentNumber);
            if (futureInstallments.Count == 0) return;

            var simulation = await _simRepo.GetByIdAsync(simulationId);
            decimal taxaMensal = simulation.Rate / 100.0m;

            decimal ValorParcelaAlvo = simulation.InstallmentAmount;

            decimal saldoAtual = startingBalance;

            foreach (var inst in futureInstallments)
            {
                if (saldoAtual <= 0)
                {
                    inst.OpeningBalance = 0;
                    inst.Interest = 0;
                    inst.Amortization = 0;
                    inst.Balance = 0;
                    inst.OriginalAmount = 0;
                    inst.Status = "Skipped"; // Marca como cancelada/pula
                    continue;
                }

                inst.OpeningBalance = Math.Round(saldoAtual, 2);

                inst.Interest = Math.Round(inst.OpeningBalance * taxaMensal, 2);

                decimal totalParaQuitar = inst.OpeningBalance + inst.Interest;

                if (totalParaQuitar <= ValorParcelaAlvo)
                {
                    inst.OriginalAmount = totalParaQuitar;
                    inst.Amortization = inst.OpeningBalance;
                    inst.Balance = 0;
                }
                else
                {
                    inst.OriginalAmount = ValorParcelaAlvo;
                    inst.Amortization = Math.Round(inst.OriginalAmount - inst.Interest, 2);
                    inst.Balance = Math.Round(inst.OpeningBalance - inst.Amortization, 2);
                }

                saldoAtual = inst.Balance;
            }

            await _repo.UpdateBulkAsync(futureInstallments);
        }
    }
}
