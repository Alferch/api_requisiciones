using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreaController : ControllerBase
    {
        private readonly IAreaRepository _repo;
        public AreaController(IAreaRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var area = await _repo.GetByIdAsync(id);
            return area == null ? NotFound() : Ok(area);
        }

        [HttpPost]
        public async Task<IActionResult> Create(AreaDto dto)
        {
            var success = await _repo.CreateAsync(dto);
            return success ? CreatedAtAction(nameof(Get), new { id = dto.ArIdArea }, dto) : BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, AreaDto dto)
        {
            if (id != dto.ArIdArea) return BadRequest();
            var success = await _repo.UpdateAsync(dto);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _repo.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }
    }
}
