using CadastroCliente.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CadastroCliente.Repositories
{
    public interface IRateTableRepository
    {
        Task<IEnumerable<RateTable>> GetAllActiveAsync();
        Task<RateTable?> GetByNameAsync(string name);
        Task<RateTable> CreateAsync(RateTable rateTable, string loggedInUser);
    }
}
