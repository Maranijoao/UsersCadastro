using CadastroCliente.Models.DTOs;
using CadastroCliente.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protege o controller inteiro
public class RateTablesController : ControllerBase
{
    private readonly IRateTableService _rateTableService;

    public RateTablesController(IRateTableService rateTableService)
    {
        _rateTableService = rateTableService;
    }

    [HttpGet("simulation")]
    public async Task<IActionResult> GetForSimulation()
    {
        var tables = await _rateTableService.GetActiveForSimulationAsync();
        return Ok(tables);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] RateTableInputDto dto)
    {
        try
        {
            var loggedInUser = User.Identity?.Name ?? "Admin";
            var createdTable = await _rateTableService.CreateAsync(dto, loggedInUser);
            return CreatedAtAction(nameof(GetForSimulation), new { id = createdTable.Id }, createdTable);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetById(int id)
        {
            return Ok(new { Message = $"Busca por ID {id} (não implementado)" });
        }
    }
