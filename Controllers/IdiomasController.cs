using Microsoft.AspNetCore.Mvc;
//using RequisicionesApi.Data;
using RequisicionesApi.Models;
using RequisicionesApi.Repositorios;
using System.Threading.Tasks;


namespace RequisicionesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IdiomasController : ControllerBase
    {
        private readonly IdiomaRepository _repository;

        public IdiomasController(IdiomaRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _repository.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var idioma = await _repository.GetByIdAsync(id);
            return idioma == null ? NotFound() : Ok(idioma);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Idioma idioma)
        {
            var success = await _repository.CreateAsync(idioma);
            return success ? CreatedAtAction(nameof(GetById), new { id = idioma.IdIdIdioma }, idioma) : BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Idioma idioma)
        {
            if (id != idioma.IdIdIdioma) return BadRequest("ID mismatch");
            var success = await _repository.UpdateAsync(idioma);
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
