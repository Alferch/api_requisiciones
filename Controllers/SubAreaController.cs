using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubAreaController : ControllerBase
    {
        private readonly ISubAreaRepository _repo;
        public SubAreaController(ISubAreaRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SubAreaDto dto)
        {
            var success = await _repo.CreateAsync(dto);
            return success ? CreatedAtAction(nameof(Get), new { id = dto.SarIdSArea }, dto) : BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SubAreaDto dto)
        {
            if (id != dto.SarIdSArea) return BadRequest();
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
