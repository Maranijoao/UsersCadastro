namespace CadastroCliente.Models
{
    public class ClienteLog
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public DateTime DataAlteracao { get; set; }
        public string UsuarioAlteracao { get; set; }
        public string Acao { get; set; }
        public string? DadosAntigos { get; set; }
        public string? DadosNovos { get; set; }
    }
}
