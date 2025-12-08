namespace CadastroCliente.Models.Entities
{
    public class Simulation
    {
        public int Id { get; set; }
        public string Product { get; set; }
        public string RateTable { get; set; }
        public decimal Rate { get; set; }
        public int Term { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal ReleasedAmount { get; set; }
        public decimal ContractValue { get; set; }

        public decimal TotalFinancedAmount { get; set; }
        public bool IOFFinanced { get; set; }
        public bool HasGracePeriod { get; set; }
        public int GracePeriodDays { get; set; }
        public decimal GracePeriodInterest { get; set; }
        public int FrequencyDays { get; set; }
        public decimal TotalIOF { get; set; }

        public DateTime SimulationDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
