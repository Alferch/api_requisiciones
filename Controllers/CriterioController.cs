using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CriterioController : ControllerBase
    {
        private readonly ICriterioService _service;

        public CriterioController(ICriterioService service)
        {
            _service = service;
        }

        // GET: api/criterio
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var criterios = await _service.GetAllAsync();
            return Ok(criterios);
        }

        // GET: api/criterio/soc/{socId}
        [HttpGet("soc/{socId}")]
        public async Task<IActionResult> GetBySocId(string socId)
        {
            var criterios = await _service.GetBySocIdAsync(socId);
            return criterios.Count > 0 ? Ok(criterios) : NotFound();
        }

        // GET: api/criterio/nombre/{nombre}
        [HttpGet("nombre/{nombre}")]
        public async Task<IActionResult> GetByNombre(string nombre)
        {
            var criterios = await _service.GetByNombreAsync(nombre);
            return criterios.Count > 0 ? Ok(criterios) : NotFound();
        }

        // POST: api/criterio
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Criterio dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var id = await _service.CreateAsync(dto);
            dto.CriId = id;
            return CreatedAtAction(nameof(GetAll), new { id }, dto);
        }

        // PUT: api/criterio/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Criterio dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto.CriId = id;
            var updated = await _service.UpdateAsync(dto);
            return updated ? NoContent() : NotFound();
        }

        // DELETE: api/criterio/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }


    }
}
