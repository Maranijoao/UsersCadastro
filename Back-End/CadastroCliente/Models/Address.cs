namespace CadastroCliente.Models
{
    public class Address
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Chave estrangeira para ligar ao Cliente
        public string? CEP { get; set; }
        public string? Street { get; set; }
        public string? Number { get; set; }
        public string? Complement { get; set; }
        public string? Neighborhood { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
    }
}
