using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Dtos;
using RequisicionesApi.Interfaces;
using System.Threading.Tasks;


namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CargasController : ControllerBase
    {

        private readonly ICargaService _service;
      //  private readonly ICargaServiceDir _serviceDir;


        public CargasController(ICargaService service)
        {
            _service = service;
        }
        /// <summary>
        /// Carga un archivo Excel (.xlsx) y, según 'opcion',
        /// 1 -> dbo.tblProveedores
        /// 2 -> dbo.tblMaestroMaterial
        /// Inserta válidas y devuelve detalle de inválidas.
        /// </summary>


        //public async Task<IActionResult> Upload([FromForm] ExcelUploadDto dto)
        //{
        //    if (!ModelState.IsValid)
        //        return ValidationProblem(ModelState);

        //    var file = dto.file;
        //    var opcion = dto.opcion;

        //    if (file == null || file.Length == 0)
        //        return BadRequest("Debe proporcionar un archivo Excel (.xlsx).");

        //    if (opcion == 1 && opcion == 2)
        //    {
        //        var result = await _service.UploadExcelAsync(file, opcion);
        //    }
        //    else
        //    {
        //        var result = await _serviceDir.UploadExcelAsync(file, opcion);

        //    }
        //    if (result.Insertados == 0 && result.Rechazados > 0)
        //        return BadRequest(result);

        //    return Ok(result);
        //}


        //[HttpPost("upload")]
        [HttpPost("excel")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(30_000_000)]       
        public async Task<IActionResult> Upload([FromForm] ExcelUploadDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var file = dto.file;
            var opcion = dto.opcion;

            if (file is null || file.Length == 0)
                return BadRequest("Debe proporcionar un archivo Excel (.xlsx).");

            UploadResult? result = null;

            // Enruta por opción
            switch (opcion)
            {
                case 1: // Proveedores
                case 2: // Maestro de Material
                    result = await _service.UploadExcelAsync(file, opcion);
                    break;

                case 3: // Direcciones (Bulk Insert)
                        // Si tu servicio de direcciones expone UploadExcelAsync con opcion==3, usa esa.
                        // Si implementaste el método específico de bulk, llama a ese:
                    result = await _service.UploadExcelDireccionesBulkAsync(file);
                    break;

                default:
                    return BadRequest("Opción inválida. Usa 1=Proveedores, 2=MaestroMaterial, 3=Direcciones.");
            }

            // Manejo de respuesta
            if (result.Insertados == 0 && result.Rechazados > 0)
                return Ok(result);

            return Ok(result);
        }

    }
}
