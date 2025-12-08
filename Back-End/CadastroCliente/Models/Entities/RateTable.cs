using System;
using System.Collections.Generic;
using System.Text.Json; // Importe o Serializador

namespace CadastroCliente.Models.Entities
{
    public class RateTable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal MinInstallmentAmount { get; set; }
        public decimal MaxInstallmentAmount { get; set; }
        public bool IsActive { get; set; }
        public string AvailableRates { get; set; }
        public string AvailableTerms { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        // Helpers para desserializar as listas de taxas e prazos
        public List<decimal> GetRatesList()
        {
            if (string.IsNullOrEmpty(AvailableRates))
                return new List<decimal>();
            return JsonSerializer.Deserialize<List<decimal>>(AvailableRates) ?? new List<decimal>();
        }

        public List<int> GetTermsList()
        {
            if (string.IsNullOrEmpty(AvailableTerms))
                return new List<int>();
            return JsonSerializer.Deserialize<List<int>>(AvailableTerms) ?? new List<int>();
        }
    }
}
