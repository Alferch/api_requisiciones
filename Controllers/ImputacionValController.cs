using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImputacionValController : ControllerBase
    {
        private readonly IImputacionValService _service;

        public ImputacionValController(IImputacionValService service)
        {
            _service = service;
        }

        // GET: api/ImputacionVal
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        // GET: api/ImputacionVal/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        // POST: api/ImputacionVal
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ImputacionValDto dto)
        {
            await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = dto.ImpIdImp }, dto);
        }

        // PUT: api/ImputacionVal/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ImputacionValDto dto)
        {
            if (id.ToString() != dto.ImpIdImp)
                return BadRequest("El ID de la URL no coincide con el del cuerpo de la solicitud.");

            await _service.UpdateAsync(dto);
            return NoContent();
        }

        // DELETE: api/ImputacionVal/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        // GET: api/ImputacionVal/detalle
        [HttpGet("detalle")]
        public async Task<IActionResult> GetAllDetalle()
        {
            var result = await _service.GetAllDetalleAsync();
            return Ok(result);
        }
    }
}
