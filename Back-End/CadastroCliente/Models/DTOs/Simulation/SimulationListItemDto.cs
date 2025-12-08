using CadastroCliente.Models.DTOs.Shared; // 🎯 1. ADICIONADO O USING QUE FALTAVA
using System;
using System.Collections.Generic;

public class SimulationListItemDto
    {
        public int Id { get; set; }
        public string Product { get; set; }
        public decimal ReleasedAmount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal ContractValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }

public class PagedSimulationResult : PagedResult<SimulationListItemDto>
{
    public int TotalPages => (PageSize > 0) ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => PageNumber < TotalPages;
}
