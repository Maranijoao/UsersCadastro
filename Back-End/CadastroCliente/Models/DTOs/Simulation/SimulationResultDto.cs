using CadastroCliente.Models.DTOs.Installment;

namespace CadastroCliente.Models.DTOs.Simulation
{
    public class SimulationResultDto
    {

        public decimal ReleasedAmount { get; set; }
        public decimal TotalIOF { get; set; }
        public decimal GracePeriodInterest { get; set; }
        public decimal TotalFinancedAmount { get; set; } 
        public decimal ContractValue { get; set; }       
        public decimal InstallmentAmount { get; set; }   

        public string Product { get; set; }
        public string RateTable { get; set; }
        public decimal Rate { get; set; }
        public int Term { get; set; }
        public bool IOFFinanced { get; set; }     
        public int GracePeriodDays { get; set; }  
        public DateTime FirstDueDate { get; set; }

        public bool IncludeInsurance { get; set; }
        public decimal InsuranceAmount { get; set; }
        public decimal InsuranceRate { get; set; }
        public decimal TacAmount { get; set; }
        public bool TacFinanced { get; set; }
        public int? RefinancedFromId { get; set; }
        public decimal PayoffAmount { get; set; }

        public List<InstallmentDetailDto> Installments { get; set; }
            = new List<InstallmentDetailDto>();
    }

    public class InstallmentDetailDto
    {
        public int Number { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Interest { get; set; }     // Juros
        public decimal Amortization { get; set; } // Amortização
        public decimal Balance { get; set; }      // Saldo Devedor
        public decimal Value { get; set; }
    }
}
