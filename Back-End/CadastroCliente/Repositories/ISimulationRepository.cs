using CadastroCliente.Models.DTOs.Dashboard;
using CadastroCliente.Models.Entities;

namespace CadastroCliente.Repositories
{
    public interface ISimulationRepository
    {
        Task<Simulation> AddAsync(Simulation simulation, string loggedInUser, int userId);
        Task<Simulation> UpdateAsync(int id, Simulation simulation, string loggedInUser);
        Task<PagedSimulationResult> GetAllAsync(string term, int pageNumber, int pageSize);
        Task<Simulation> GetByIdAsync(int id);
        Task<DashboardTotalsDtos> GetDashboardTotalsAsync();
    }
}
