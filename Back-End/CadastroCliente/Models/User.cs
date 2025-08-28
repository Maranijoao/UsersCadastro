using System.ComponentModel.DataAnnotations;

namespace CadastroCliente.Models;

public class User
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

    public List<Address> Address { get; set; } = new List<Address>();

    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? InactivatedAt { get; set; }
    public string? InactivatedBy { get; set; }

    public List<UserLog> Logs { get; set; } = new List<UserLog>();
}
