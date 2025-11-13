using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Dtos;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public sealed class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService service, ILogger<DashboardController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Devuelve la fila de dashboard (conteos y porcentajes)</summary>
        [HttpGet("pivot")]
        [ProducesResponseType(typeof(DashboardPivotDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPivot(CancellationToken ct)
        {
            var data = await _service.GetDashboardPivotAsync(ct);
            return Ok(data);
        }

        /// <summary>Devuelve los conteos por Resultado más el total</summary>
        [HttpGet("conteo")]
        [ProducesResponseType(typeof(IEnumerable<ConteoResultadoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConteo(CancellationToken ct)
        {
            var data = await _service.GetConteosAsync(ct);
            return Ok(data);
        }

        /// <summary>
        /// Devuelve el resumen paginado. Puedes filtrar por Resultado.
        /// Ej: /api/dashboard/resumen?page=1&pageSize=50&resultado=AUTORIZADA
        /// </summary>
        [HttpGet("resumen")]
        [ProducesResponseType(typeof(PagedResult<RequisicionResumenDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResumen(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? resultado = null,
            CancellationToken ct = default)
        {
            var data = await _service.GetResumenAsync(page, pageSize, resultado, ct);
            return Ok(data);
        }
    }


    }
