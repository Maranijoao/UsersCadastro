using CadastroCliente.Models.DTOs;
using CadastroCliente.Models.DTOs.Dashboard;
using CadastroCliente.Models.DTOs.Simulation;
using CadastroCliente.Models.Entities;
using CadastroCliente.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendGrid.Helpers.Errors.Model;
using System.Security.Claims;

namespace CadastroCliente.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SimulationsController : ControllerBase
    {
        private readonly ISimulationService _simulationService;
        private readonly ILogger<SimulationsController> _logger;
        public SimulationsController(ISimulationService simulationService, ILogger<SimulationsController> logger)
        {
            _simulationService = simulationService;
            _logger = logger;
        }

        [HttpPost("calculate")]
        public async Task<ActionResult<SimulationResultDto>> Calculate([FromBody] SimulationInputDto input)
        {
            try
            {
                var result = await _simulationService.CalculateAsync(input);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Ocorreu um erro interno ao calcular a simulação. Input: {SimulationInput}", input);

                return StatusCode(500, new { message = "Ocorreu um erro interno ao calcular a simulação." });
            }
        }

        [HttpPost("confirm")]
        [Authorize]
        public async Task<ActionResult<Simulation>> Confirm([FromBody] SimulationResultDto resultDto)
        {
            var loggedInUser = User.Identity?.Name ?? "Sistema";
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = userIdClaim != null ? int.Parse(userIdClaim) : 0;

            try
            {
                _logger.LogInformation("Recebendo confirmação de simulação. Produto: {Product}, Valor: {Value}", resultDto.Product, resultDto.ContractValue);
                var savedSimulation = await _simulationService.ConfirmAsync(resultDto, loggedInUser, userId);

                if (savedSimulation == null || savedSimulation.Id == 0)
                {
                    _logger.LogError("Falha ao salvar a simulação: ID retornou 0.");
                    return StatusCode(500, new { message = "Não foi possível salvar a simulação no banco." });
                }

                return CreatedAtAction(nameof(GetById), new { id = savedSimulation.Id }, savedSimulation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar simulação.");
                return StatusCode(500, new { message = "Ocorreu um erro interno ao confirmar a simulação." });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Simulation>> Update(int id, [FromBody] Simulation simulationData)
        {
            if (id != simulationData.Id)
            {
                return BadRequest(new { message = "O Id da URL não corresponde ao Id da simulação" });
            }

            var loggedInUser = User.Identity?.Name ?? "Sistema";

            try
            {
                var updateSim = await _simulationService.UpdateAsync(id, simulationData, loggedInUser);
                return Ok(updateSim);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao Atualizar simulação { id }", id);
                return StatusCode(500, new { message = "Erro interno ao atualizar simulação. " });
            }
        }

        [HttpGet]
        public async Task<ActionResult<PagedSimulationResult>> GetAll(

            [FromQuery] string term = "",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _simulationService.GetAllAsync(term, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar a lista de simulações");
                return StatusCode(500, new { message = "Erro interno ao buscar simulações" });
            }
        }

        [HttpGet("totals")]
        public async Task<ActionResult<DashboardTotalsDtos>> GetTotals()
        {
            try
            {
                var totals = await _simulationService.GetDashboardTotalsAsync();
                return Ok(totals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar totais do dashboard");
                return StatusCode(500, new { message = "Erro interno ao buscar totais do dashboard" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var simulation = await _simulationService.GetByIdAsync(id);

            if (simulation == null)
            {
                return NotFound(new { message = "Simulação não encontrada." });
            }
            return Ok(simulation);
        }
    }
}
