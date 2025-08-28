namespace CadastroCliente.Models
{
    public class Endereco
    {
        public int Id { get; set; }
        public int ClienteId { get; set; } // Chave estrangeira para ligar ao Cliente
        public string? CEP { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? UF { get; set; }
    }
}
