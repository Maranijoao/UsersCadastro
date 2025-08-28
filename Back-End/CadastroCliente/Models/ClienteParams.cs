using System.ComponentModel.DataAnnotations;

namespace CadastroCliente.Models;
//Com validações

public class ClienteParams
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

    public List<EnderecoParams> Enderecos { get; set; } = new List<EnderecoParams>();
}
