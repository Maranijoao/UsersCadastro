using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; // Adicionar 'using' para IConfiguration
using Microsoft.Extensions.Logging; // Adicionar 'using' para ILogger

namespace CadastroCliente.Services; // Adicionar namespace para organização

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger; // Injetar o logger

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var apiKey = _configuration["EmailSettings:ApiKey"];
        var fromEmail = _configuration["EmailSettings:FromEmail"];
        var fromName = _configuration["EmailSettings:FromName"];
        var resetUrl = $"{_configuration["FrontendUrls:ResetPassword"]}?token={resetToken}";

        var user = new SendGridClient(apiKey);
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = "Redefinição de Senha - ElevaDigital",
            PlainTextContent = $"Olá, clique no link a seguir para redefinir a sua senha: {resetUrl}",
            HtmlContent = $@"
                <h2>Redefinição de Senha</h2>
                <p>Recebemos uma solicitação para redefinir a sua senha. Clique no botão abaixo para criar uma nova senha:</p>
                <a href='{resetUrl}' style='background-color: #4f46e5; color: white; padding: 14px 25px; text-align: center; text-decoration: none; display: inline-block; border-radius: 8px;'>Redefinir Senha</a>
                <p>Se você não solicitou esta alteração, pode ignorar este e-mail.</p>
                <p>O link é válido por 15 minutos.</p>"
        };
        msg.AddTo(new EmailAddress(toEmail));

        var response = await user.SendEmailAsync(msg);

        // Lógica de erro melhorada
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Body.ReadAsStringAsync();
            _logger.LogError("Falha ao enviar e-mail via SendGrid. Status: {StatusCode}, Erro: {ErrorBody}", response.StatusCode, errorBody);

            // Lança uma exceção mais detalhada
            throw new Exception($"Não foi possível enviar o e-mail de redefinição de senha. Resposta do SendGrid: {errorBody}");
        }
    }
}