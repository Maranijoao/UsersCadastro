using System.ComponentModel.DataAnnotations;

namespace CadastroCliente.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O campo Nome é obrigatório")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(14, MinimumLength = 11)]
    public string Cpf { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string? Senha { get; set; }
    public bool RecordStatus { get; set; }
    [Required(ErrorMessage = "O campo Role é obrigatório")]
    public string Role { get; set; }
    public List<Endereco> Enderecos { get; set; } = new List<Endereco>();

    public DateTime? DataCadastro { get; set; }
    public string? UsuarioCadastro { get; set; }
    public DateTime? DataUltimaAlteracao { get; set; }
    public string? UsuarioUltimaAlteracao { get; set; }
    public DateTime? DataInativacao { get; set; }
    public string? UsuarioInativacao { get; set; }

    public List<ClienteLog> Logs { get; set; } = new List<ClienteLog>();
}
