using CadastroCliente.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CadastroCliente.Services
{
    public interface IRateTableService
    {
        Task<IEnumerable<RateTableDto>> GetActiveForSimulationAsync();

        Task<RateTableDto> CreateAsync(RateTableInputDto dto, string loggedInUser);
    }
}
