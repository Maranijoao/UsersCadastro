using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CadastroCliente.Models.DTOs
{
    public class RateTableDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal MinInstallmentAmount { get; set; }
        public decimal MaxInstallmentAmount { get; set; }
        public bool IsActive { get; set; }
        public List<decimal> AvailableRates { get; set; }
        public List<int> AvailableTerms { get; set; }
    }

    public class RateTableInputDto
    {
        [Required]
        public string Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinInstallmentAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MaxInstallmentAmount { get; set; }
        public bool IsActive { get; set; } = true;

        public List<decimal> AvailableRates { get; set; } = new List<decimal>();
        public List<int> AvailableTerms { get; set; } = new List<int>();
    }
}
