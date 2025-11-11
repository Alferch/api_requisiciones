using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RequisicionesApi.Models;
using RequisicionesApi.Services;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProvCRequisicionesController : ControllerBase
    {
        private readonly ProvCRequisicionesService _service;
        private readonly ILogger<RequisicionesController> _logger;

        public ProvCRequisicionesController(ProvCRequisicionesService service, ILogger<RequisicionesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("pendientes/{correo}")]
        public ActionResult<IEnumerable<ProvCRequisicion>> GetPendientes(string correo)
        {
            var result = _service.ObtenerRequisicionesPendientes(correo);
            return Ok(result);
        }

        [HttpGet("detalle/{idRequisicion}")]
        public ActionResult<IEnumerable<ProvCRequisicionDetalle>> GetDetalle(string idRequisicion, [FromQuery] string proveedorId)
        {
            var result = _service.ObtenerDetalleRequisicion(idRequisicion, proveedorId);
            return Ok(result);
        }




        [HttpPost]
        public IActionResult Insertar([FromBody] List<ProvCInsertRequisicionRequest> requests)
        {
            try
            {
                var ok = _service.InsertarRequisicionesProveedor(requests);
                return ok ? Ok(new { message = "Insertadas correctamente." })
                          : StatusCode(StatusCodes.Status500InternalServerError, new { message = "Operación fallida sin excepción." });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error al insertar requisiciones.");

                // Puedes mapear errores comunes, por ejemplo violación de PK/UK, timeouts, etc.
                return Problem(
                    title: "Error de base de datos",
                    detail: sqlEx.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    instance: HttpContext.TraceIdentifier);
            }
            catch (InvalidOperationException invEx)
            {
                _logger.LogError(invEx, "Error de operación al insertar requisiciones.");

                return Problem(
                    title: "Error en la operación",
                    detail: invEx.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    instance: HttpContext.TraceIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al insertar requisiciones.");

                return Problem(
                    title: "Error inesperado",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    instance: HttpContext.TraceIdentifier);
            }
        }



        //[HttpPost("grabar")]
        //public IActionResult Insertar([FromBody] List<ProvCInsertRequisicionRequest> requests)
        //{
        //    var success = _service.InsertarRequisicionesProveedor(requests);
        //    if (success)
        //        return Ok(new { message = "Requisiciones grabadas correctamente" });
        //    return BadRequest(new { message = "Error al grabar requisiciones" });
        //}


        //[HttpPost("grabar")]
        //public IActionResult Insertar([FromBody] ProvCInsertRequisicionRequest request)
        //{
        //    var success = _service.InsertarRequisicionProveedor(request);
        //    if (success)
        //        return Ok(new { message = "Requisición grabada correctamente" });
        //    return BadRequest(new { message = "Error al grabar requisición" });
        //}


    }
}
