using System;

namespace CadastroCliente.Models.Entities
{
    public class Installment
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
}
