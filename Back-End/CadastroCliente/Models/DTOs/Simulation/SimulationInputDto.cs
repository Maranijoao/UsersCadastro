using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;

namespace CadastroCliente.Models.DTOs.Simulation
{
    public class SimulationInputDto
    {
        [Required]
        public string Product { get; set; } // Produto

        [Required]
        public string RateTable { get; set; } // Tabela de Taxas

        [Range(0.01, 100)]
        public decimal Rate { get; set; } // Taxa

        [Range(1, 999)]
        public int Term { get; set; } // Prazo em meses

        [Range(1, double.MaxValue)]
        public decimal RequestedAmount { get; set; }

        public bool FinanceIOF { get; set; }
        public int GracePeriodDays { get; set; }
        public int FrequencyDays { get; set; } = 30;

        public bool IncludeInsurance { get; set; }
        public decimal InsuranceRate { get; set; }
        public int? RefinancedFromId { get; set; }
        public decimal TacAmount { get; set; }
        public bool FinanceTac { get; set; }

    }
}
