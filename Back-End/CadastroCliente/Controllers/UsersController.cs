using CadastroCliente.Data;
using CadastroCliente.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CadastroUser.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly TokenService _tokenService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserRepository userRepository, TokenService tokenService, ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    // --- Endpoints de Leitura (Acessíveis por qualquer utilizador logado) ---

    // Lista com filtro
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll([FromQuery] string? term = "")
    {
        var users = await _userRepository.GetAllAsync(term, 1);
        return Ok(users);
    }

    // Listar Users inativos
    [HttpGet("inactive")]
    public async Task<ActionResult<IEnumerable<User>>> GetInactive([FromQuery] string? term = "")
    {
        var inactiveUsers = await _userRepository.GetInactiveAsync(term);
        return Ok(inactiveUsers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet("me")]
    public async Task<ActionResult<User>> GetMe()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByEmailAsync(
            email);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<User>> Login([FromBody] LoginRequest login)
    {
        var user = await _userRepository.LoginAsync(login.Email, login.Password);
        Console.Write(user);

        string PasswordHash = SecurityHelper.ComputeSha256Hash(login.Password);

        if (user is null)
            return NotFound("user não cadastrado");

        if (user.Password != PasswordHash)
            return NotFound("Email ou Password inválidos");

        if (user.RecordStatus == false)
            return NotFound("Sua conta está inativa, Por favor entre em contato com o suporte");
        var token = _tokenService.GenerateToken(user);

        return Ok(new
        {
            token,
            user
        });
    }

    // --- Endpoints de Escrita (Apenas para Administradores) ---

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<User>> Create([FromBody] User user)
    {
        if (user == null) return BadRequest();

        var loggedInUser = User.Identity?.Name ?? "Usuário Desconhecido";

        var createdUser = await _userRepository.AddAsync(user, loggedInUser);

        return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Update(int id, [FromBody] User user)
    {
        if (id != user.Id)
        {
            return BadRequest("O ID da URL não corresponde ao ID do Usuário enviado.");
        }

        var userFromDb = await _userRepository.GetByIdAsync(id);
        if (userFromDb == null)
        {
            return NotFound("Usuário não encontrado.");
        }

        var Password = SecurityHelper.ComputeSha256Hash(user.Password);
        if (userFromDb.Password != user.Password)
            user.Password = Password;


        var loggedInUser = User.Identity?.Name ?? "Usuário Desconhecido";

        try
        {
            await _userRepository.UpdateAsync(user, loggedInUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Se ocorrer um erro no repositório, ele será logado aqui.
            _logger.LogError(ex, "Ocorreu um erro ao atualizar o user com ID {userId}", id);
            // Retorna um erro 500 para o frontend saber que algo correu mal.
            return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        var loggedInUser = User.Identity?.Name ?? "Usuário Desconhecido";

        await _userRepository.DeleteAsync(id, loggedInUser);

        return Ok(new { mensagem = "Usuário Inativado com sucesso " });
    }

    [HttpPatch("reactivate/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Reactivate(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound("Usuário não encontrado");

        if (user.RecordStatus is true)
            return BadRequest("Usuário já está ativo");

        await _userRepository.ReactivateAsync(id);
        return Ok(new { mensagem = "Usuário reativado com sucesso" });
    }
}
