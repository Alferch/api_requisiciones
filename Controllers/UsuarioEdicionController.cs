using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioEdicionController : ControllerBase
    {
        private readonly IUsuarioEdicionRepository _repo;
        public UsuarioEdicionController(IUsuarioEdicionRepository repo) => _repo = repo;

        [HttpGet("{usrIdClave}")]
        public async Task<IActionResult> Get(string usrIdClave)
        {
            var result = await _repo.GetAsync(usrIdClave);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UsuarioEdicionDto dto)
        {
            var success = await _repo.CreateAsync(dto);
            return success ? Ok() : BadRequest();
        }

        [HttpPut("{usrIdClave}")]
        public async Task<IActionResult> Update(string usrIdClave, [FromBody] UsuarioEdicionDto dto)
        {
            if (usrIdClave != dto.Usuario?.UsrIdClave) return BadRequest();
            var success = await _repo.UpdateAsync(dto);
            return success ? Ok() : NotFound();
        }

        [HttpDelete("{usrIdClave}")]
        public async Task<IActionResult> Delete(string usrIdClave)
        {
            var success = await _repo.DeleteAsync(usrIdClave);
            return success ? Ok() : NotFound();
        }
    }
}
