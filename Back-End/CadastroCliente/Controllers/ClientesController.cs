using CadastroCliente.Data;
using CadastroCliente.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CadastroCliente.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly ClienteRepository _clienteRepository;
    private readonly TokenService _tokenService;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(ClienteRepository clienteRepository, TokenService tokenService, ILogger<ClientesController> logger)
    {
        _clienteRepository = clienteRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    // --- Endpoints de Leitura (Acessíveis por qualquer utilizador logado) ---

    // Lista com filtro
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cliente>>> GetAll([FromQuery] string? termo = "")
    {
        var clientes = await _clienteRepository.GetAllAsync(termo, 1);
        return Ok(clientes);
    }

    // Listar clientes inativos
    [HttpGet("inativos")]
    public async Task<ActionResult<IEnumerable<Cliente>>> GetInativos([FromQuery] string? termo = "")
    {
        var clientesInativos = await _clienteRepository.GetInativosAsync(termo);
        return Ok(clientesInativos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cliente>> GetById(int id)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id);
        return cliente == null ? NotFound() : Ok(cliente);
    }

    [HttpGet("me")]
    public async Task<ActionResult<Cliente>> GetMe()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        var cliente = await _clienteRepository.GetByEmailAsync(
            email);
        return cliente == null ? NotFound() : Ok(cliente);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<Cliente>> Login([FromBody] LoginRequest login)
    {
        var cliente = await _clienteRepository.LoginAsync(login.Email, login.Password);
        Console.Write(cliente);

        string senhaHash = SecurityHelper.ComputeSha256Hash(login.Password);

        if (cliente is null)
            return NotFound("Cliente não cadastrado");

        if (cliente.Senha != senhaHash)
            return NotFound("Email ou Senha inválidos");

        if (cliente.RecordStatus == false)
            return NotFound("Sua conta está inativa, Por favor entre em contato com o suporte");
        var token = _tokenService.GenerateToken(cliente);

        return Ok(new
        {
            token,
            cliente
        });
    }

    // --- Endpoints de Escrita (Apenas para Administradores) ---

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Cliente>> Create([FromBody] Cliente cliente)
    {
        if (cliente == null) return BadRequest();

        var usuarioLogado = User.Identity?.Name ?? "Usuário Desconhecido";

        var clienteCriado = await _clienteRepository.AddAsync(cliente, usuarioLogado);

        return CreatedAtAction(nameof(GetById), new { id = clienteCriado.Id }, clienteCriado);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Update(int id, [FromBody] Cliente cliente)
    {
        if (id != cliente.Id)
        {
            return BadRequest("O ID da URL não corresponde ao ID do cliente enviado.");
        }

        var clienteDoBanco = await _clienteRepository.GetByIdAsync(id);
        if (clienteDoBanco == null)
        {
            return NotFound("Cliente não encontrado.");
        }

        var senha = SecurityHelper.ComputeSha256Hash(cliente.Senha);
        if (clienteDoBanco.Senha != cliente.Senha)
            cliente.Senha = senha;


        var usuarioLogado = User.Identity?.Name ?? "Usuário Desconhecido";

        try
        {
            await _clienteRepository.UpdateAsync(cliente, usuarioLogado);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Se ocorrer um erro no repositório, ele será logado aqui.
            _logger.LogError(ex, "Ocorreu um erro ao atualizar o cliente com ID {ClienteId}", id);
            // Retorna um erro 500 para o frontend saber que algo correu mal.
            return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id);
        if (cliente == null) return NotFound();

        var usuarioLogado = User.Identity?.Name ?? "Usuário Desconhecido";

        await _clienteRepository.DeleteAsync(id, usuarioLogado);
        return NoContent();
    }

    [HttpPatch("reativar/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Reativar(int id)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id);
        if (cliente == null)
            return NotFound("Cliente não encontrado");

        if (cliente.RecordStatus is true)
            return BadRequest("Cliente já está ativo");

        await _clienteRepository.ReativarAsync(id);
        return Ok(new { mensagem = "Cliente reativado com sucesso" });
    }
}
