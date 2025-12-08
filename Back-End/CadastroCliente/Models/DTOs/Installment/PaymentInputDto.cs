using System;
using System.ComponentModel.DataAnnotations;

namespace CadastroCliente.Models.DTOs
{
    public class PaymentInputDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }
    }
}