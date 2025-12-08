using CadastroCliente.Models.DTOs;
using CadastroCliente.Models.DTOs.Dashboard;
using CadastroCliente.Models.DTOs.Simulation;
using CadastroCliente.Models.Entities;

namespace CadastroCliente.Services
{
    public interface ISimulationService
    {
        Task<SimulationResultDto> CalculateAsync(SimulationInputDto input);
        Task<Simulation> ConfirmAsync(SimulationResultDto resultDto, string loggedInUser, int userId);
        Task<Simulation> UpdateAsync(int id, Simulation simulation, string loggedInUser);
        Task<PagedSimulationResult> GetAllAsync(string term, int pageNumber, int pageSize);
        Task<Simulation?> GetByIdAsync(int id);
        Task<DashboardTotalsDtos> GetDashboardTotalsAsync();
    }
}
