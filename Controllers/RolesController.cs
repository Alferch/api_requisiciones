using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {

        private readonly IRolService _rolService;

        public RolesController(IRolService rolService)
        {
            _rolService = rolService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _rolService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var rol = await _rolService.GetByIdAsync(id);
            return rol is null ? NotFound() : Ok(rol);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RolDto rol)
        {
            await _rolService.CreateAsync(rol);
            return CreatedAtAction(nameof(GetById), new { id = rol.IdRol }, rol);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] RolDto rol)
        {
            if (id != rol.IdRol) return BadRequest();
            await _rolService.UpdateAsync(rol);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _rolService.DeleteAsync(id);
            return NoContent();
        }
    }
}
