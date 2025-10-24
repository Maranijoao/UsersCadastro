using CadastroCliente.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using CadastroCliente.Models.Entities;
using CadastroCliente.Models.DTOs.Auth;
using CadastroCliente.Helpers;
using CadastroCliente.Repositories;

namespace CadastroCliente.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly TokenService _tokenService;
    private readonly EmailService _emailService;

    public AuthController(UserRepository userRepository, TokenService tokenService, EmailService emailService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    [HttpPost("login")] // A rota completa será /api/auth/login
    [AllowAnonymous]
    public async Task<ActionResult> Login([FromBody] LoginRequest login)
    {
        var user = await _userRepository.GetByEmailAsync(login.Email);

        if (user is null)
            return NotFound("Usuário não cadastrado");

        string passwordHash = SecurityHelper.ComputeSha256Hash(login.Password);
        if (user.Password != passwordHash)
            return NotFound("E-mail ou Senha inválidos.");

        if (user.RecordStatus == false)
            return NotFound("A sua conta está inativa. Por favor, entre em contato com o suporte.");

        var token = _tokenService.GenerateToken(user);
        user.Password = null;

        return Ok(new
        {
            token,
            user
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<User>> GetMe()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return NotFound();

        user.Password = null;
        return Ok(user);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user != null && user.RecordStatus)
        {
            var token = _tokenService.GeneratePasswordResetToken(user);

            await _emailService.SendPasswordResetEmailAsync(user.Email, token);
        }
 
        return Ok(new { message = "Se o e-mail estiver cadastrado, você receberá in'struções para redefinir sua senha." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var principal = _tokenService.ValidateToken(request.Token);
            var purposeClaim = principal.Claims.FirstOrDefault(c => c.Type == "purpose");

            if (purposeClaim?.Value != "password-reset")
            {
                return BadRequest("Token inválido para redefinição de senha.");
            }

            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return BadRequest(new { message = "Token Inválido" });
            }

            var newPasswordHash = SecurityHelper.ComputeSha256Hash(request.NewPassword);

            await _userRepository.UpdatePasswordAsync(userId, newPasswordHash);

            return Ok(new { message = "Senha redefinida com sucesso." });
        }
        catch (SecurityTokenExpiredException)
        {
            return BadRequest(new { message = "O seu link de redefinição expirou. Por favor, solicite um novo." });
        }
        catch
        {
            return BadRequest(new { message = "O seu link de redefinição é inválido ou já foi utilizado." });
        }
    }
}
