using CadastroCliente.Helpers;
using CadastroCliente.Models.DTOs.Shared;
using CadastroCliente.Models.Entities;
using CadastroCliente.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CadastroCliente.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(UserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    // --- Endpoints de Leitura (Acessíveis por qualquer utilizador logado) ---

    // Lista com filtro
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PagedResult<User>>> GetAll(
        [FromQuery] string term = "",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string statusFilter = "all",
        [FromQuery] string roleFilter = "all")
    {
        try
        {
            _logger.LogInformation("A iniciar a busca por utilizadores com o termo: '{Term}'", term);
            var pagedResult = await _userService.GetAllAsync(term, pageNumber, pageSize, statusFilter, roleFilter);
            _logger.LogInformation("Busca concluída com sucesso. Foram encontrados {Count} registos.", pagedResult.TotalCount);
            return Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao executar a pesquisa por utilizadores.");
            return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação de pesquisa.");
        }
    }

    [HttpGet("registrations-by-day")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IEnumerable<ChartDataPoint>>> GetUserRegistrationsByDayAsync()
    {
        try
        {
            var chartData = await _userService.GetUserRegistrationsByDayAsync();
            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao buscar os dados para o gráfico de registros.");
            return StatusCode(500, "Ocorreu um erro interno.");
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return user == null ? NotFound("Usuário não encontrado.") : Ok(user);
    }
    
    // --- Endpoints de Escrita (Apenas para Administradores) "[Authorize(Roles = "admin")]" ---

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<User>> Create([FromBody] User user)
    {
        _logger.LogInformation("Objeto recebido para CRIAR utilizador: {UserData}", JsonSerializer.Serialize(user));

        if (user == null) return BadRequest();
        var loggedInUser = User.Identity?.Name ?? "Sistema";
        var createdUser = await _userService.AddAsync(user, loggedInUser);

        return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Update(int id, [FromBody] User userFromRequest)
    {

        if (id != userFromRequest.Id)
        {
            return BadRequest("O ID da URL não corresponde com o ID do usuário.");
        }

        var loggedInUser = User.Identity?.Name ?? "Sistema";
        try
        {
            await _userService.UpdateAsync(id, userFromRequest, loggedInUser);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar o usuário com ID {userId}", id);
            return StatusCode(500, "Ocorreu um erro interno.");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();

        var loggedInUser = User.Identity?.Name ?? "Sistema";
        await _userService.DeleteAsync(id, loggedInUser);

        return Ok(new { message = "Usuário inativado com sucesso." });
    }

    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Reactivate(int id)
    {
        var loggedInUser = User.Identity?.Name ?? "Sistema";
        await _userService.ReactivateAsync(id, loggedInUser);
        return Ok(new { message = "Usuário reativado com sucesso." });
    }
}
