using CadastroCliente.Helpers;
using CadastroCliente.Models.DTOs.Shared; 
using System;

namespace CadastroCliente.Models.DTOs.Installment
{
    public class InstallmentDto
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public int InstallmentNumber { get; set; }
        public decimal OriginalAmount { get; set; }
        public string Status { get; set; }
        public DateTime DueDate { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal Interest { get; set; }
        public decimal Amortization { get; set; }
        public decimal Balance { get; set; }
    }
    public class PaginatedInstallmentResult : PagedResult<InstallmentDto>
    {
        public int TotalPages => (PageSize > 0) ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}

