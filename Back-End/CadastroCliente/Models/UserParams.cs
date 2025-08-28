using System.ComponentModel.DataAnnotations;

namespace CadastroCliente.Models;
//Com validações

public class UserParams
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

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Password { get; set; }

    public bool RecordStatus { get; set; }

    [Required(ErrorMessage = "O campo Role é obrigatório")]
    public string Role { get; set; }

    public List<AddressParams> Address { get; set; } = new List<AddressParams>();
}
