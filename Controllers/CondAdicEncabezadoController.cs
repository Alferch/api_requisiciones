using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models.Condiciones;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CondAdicEncabezadoController : ControllerBase
    {


        private readonly ICondAdicEncabezadoService _service;

        public CondAdicEncabezadoController(ICondAdicEncabezadoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _service.GetAllAsync();
            return Ok(lista);
        }

        [HttpGet("{reqIdClave}/{idCondicion}")]
        public async Task<IActionResult> GetById(string reqIdClave, string idCondicion)
        {
            var entidad = await _service.GetByIdAsync(reqIdClave, idCondicion);
            if (entidad == null) return NotFound();
            return Ok(entidad);
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] List<CondAdicEncabezado> entidad)
        {
            await _service.InsertAsync(entidad);
            return Ok("Registro insertado correctamente");
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] List<CondAdicEncabezado> entidad)
        {
            await _service.UpdateAsync(entidad);
            return Ok("Registro actualizado correctamente");
        }

        [HttpDelete("{reqIdClave}/{idCondicion}")]
        public async Task<IActionResult> Delete(string reqIdClave, string idCondicion)
        {
            await _service.DeleteAsync(reqIdClave, idCondicion);
            return Ok("Registro eliminado correctamente");
        }



        [HttpGet("ListaCondic")]
        public async Task<IActionResult> GetAllCondicion()
        {
            var lista = await _service.GetAllAsyncCondicion();
            return Ok(lista);
        }




    }
}
