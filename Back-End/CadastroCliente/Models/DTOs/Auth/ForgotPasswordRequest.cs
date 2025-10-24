using System.ComponentModel.DataAnnotations;

namespace CadastroCliente.Models.DTOs.Auth;

    public class ForgotPasswordRequest
    {
    [Required(ErrorMessage = "O campo Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do e-mail é inválido.")]
    public string Email { get; set; }
}
