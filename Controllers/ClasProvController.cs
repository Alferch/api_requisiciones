using Microsoft.AspNetCore.Mvc;
//using RequisicionesApi.Data;
using RequisicionesApi.Models;
using RequisicionesApi.Repositorios;
using System.Threading.Tasks;

namespace RequisicionesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClasProvController : ControllerBase
    {
        private readonly ClasProvRepository _repository;

        public ClasProvController(ClasProvRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _repository.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClasProv item)
        {
            var success = await _repository.CreateAsync(item);
            return success ? CreatedAtAction(nameof(GetById), new { id = item.CpIdclas }, item) : BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ClasProv item)
        {
            if (id != item.CpIdclas) return BadRequest("ID mismatch");
            var success = await _repository.UpdateAsync(item);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _repository.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }
    }
}
