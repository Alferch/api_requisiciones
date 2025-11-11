using Microsoft.AspNetCore.Mvc;

using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace Reqs.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimelineController : ControllerBase
{
    private readonly ITimelineService _service;

    public TimelineController(ITimelineService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista eventos del timeline con filtros opcionales.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TimelineEventDto>>> Get(
        [FromQuery] string? numero,
        [FromQuery] bool useLike = false,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] string? hito = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? order = "timeline",
        CancellationToken ct = default)
    {
        var query = new TimelineQuery
        {
            Numero = numero,
            UseLike = useLike,
            Desde = desde,
            Hasta = hasta,
            Hito = hito,
            Estado = estado,
            Order = order ?? "timeline"
        };

        var data = await _service.GetAsync(query, ct);
        return Ok(data);
    }

    /// <summary>
    /// Obtiene el timeline de una requisición en específico.
    /// </summary>
    [HttpGet("{numero}")]
    public async Task<ActionResult<IEnumerable<TimelineEventDto>>> GetByNumero(string numero, CancellationToken ct)
    {
        var data = await _service.GetByNumeroAsync(numero, ct);
        if (data.Count == 0) return NotFound();
        return Ok(data);
    }
}