using CadastroCliente.Models.DTOs;
using CadastroCliente.Models.DTOs.Installment;
using CadastroCliente.Models.Entities;
using CadastroCliente.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CadastroCliente.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InstallmentController : ControllerBase
    {
        private readonly IInstallmentService _service;
        private readonly ILogger<InstallmentController> _logger;

        public InstallmentController(IInstallmentService service, ILogger<InstallmentController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("simulation/{simulationId}")]
        public async Task<ActionResult<PaginatedInstallmentResult>> GetForSimulation(
            [FromRoute] int simulationId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _service.GetBySimulationIdAsync(simulationId, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter parcelas para a simulação {SimulationId}", simulationId);
                return StatusCode(500, "Ocorreu um erro ao buscar parcelas.");
            }
        }

        [HttpPost("pay/{id}")]
        public async Task<ActionResult> PayInstallment(int id, [FromBody] PaymentInputDto input)
        {
            try
            {
                if (input.Amount <= 0)
                {
                    return BadRequest(new { message = "O valor do pagamento deve ser maior que zero." });
                }

                await _service.RegisterPaymentAsync(id, input.Amount, input.PaymentDate);

                return Ok(new { message = "Pagamento registrado com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar pagamento da parcela {Id}", id);
                return StatusCode(500, new { message = "Erro ao registrar pagamento." });
            }
        }
    }
}
