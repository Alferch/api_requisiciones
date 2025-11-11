using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;
using RequisicionesApi.Services;
using RequisicionesApi.Utilidades;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdjudicacionController : ControllerBase
    {
        private readonly IRequisicionService _requisicionService;
        private readonly IMailService _mailService;
        private readonly ILogger<AdjudicacionController> _logger;

        public AdjudicacionController(
            IRequisicionService requisicionService,
            IMailService mailService,
            ILogger<AdjudicacionController> logger)
        {
            _requisicionService = requisicionService;
            _mailService = mailService;
            _logger = logger;
        }

        [HttpPost("enviar-correo")]
        public async Task<IActionResult> EnviarCorreo([FromBody] AdjudicacionRequest request)
        {
            var adjudicacionDTO = await _requisicionService.ObtenerAdjudicacionAsync(request.ReqIdClave, request.ProveedorId, request.provIdSoc);

            if (adjudicacionDTO == null || adjudicacionDTO.Productos.Count == 0)
            {
                _logger.LogWarning("No se encontraron productos adjudicados para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", request.ReqIdClave, request.ProveedorId);
                return NotFound("No se encontraron productos adjudicados.");
            }

            var html = MailBuilder.GenerarCorreoAdjudicacion(adjudicacionDTO);

            var resultado = await _mailService.EnviarCorreoAdjudicacionAsync(adjudicacionDTO,  request.EmailUsuario);
            var resultado1 = await _requisicionService.ActualizarReqProvGanAsync(request.ReqIdClave, request.ProveedorId, request.provIdSoc);
            return resultado.Success ? Ok(resultado) : StatusCode(500, resultado);
        }

        [HttpPost("notificar-usuario")]

        public async Task<IActionResult> NotificarUsuario([FromBody] AdjudicacionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ReqIdClave)
                || string.IsNullOrWhiteSpace(request.ProveedorId)
                || string.IsNullOrWhiteSpace(request.provIdSoc)
                || string.IsNullOrWhiteSpace(request.EmailUsuario))
            {
                _logger.LogWarning("Solicitud inválida o faltante para notificación de usuario.");
                return BadRequest("Solicitud inválida o faltante.");
            }

            try
            {
                var adjudicacionDTO = await _requisicionService.ObtenerAdjudicacionAsync(request.ReqIdClave, request.ProveedorId, request.provIdSoc);

                if (adjudicacionDTO == null || adjudicacionDTO.Productos.Count == 0)
                {
                    _logger.LogWarning("No se encontraron productos adjudicados para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", request.ReqIdClave, request.ProveedorId);
                    return NotFound("No se encontraron productos adjudicados.");
                }

                var resultado1 = await _requisicionService.ActualizarReqProvGanAsync(request.ReqIdClave, request.ProveedorId, request.provIdSoc);

                var resultado = await _requisicionService.ObtenerRequisiciones(request.ReqIdClave);

                foreach (var item in resultado)
                {
                    _logger.LogInformation($"{item.ReqIdClave} - {item.UsrNombre} {item.UsrApellidoP}");
                }

                var resultado2 = await _mailService.EnviarCorreoUsuAutAsyncTask(adjudicacionDTO, resultado, request.EmailUsuario);

                if (resultado2.Success)
                {
                    return Ok(resultado);

                }
                else {                     _logger.LogError("Error al enviar correo al usuario para ReqIdClave: {reqIdClave}. Detalle: {detalle}", request.ReqIdClave, resultado2.Message);
                    return StatusCode(500, resultado2);
                    }
             
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en NotificarUsuario para ReqIdClave: {reqIdClave}", request?.ReqIdClave);
                return StatusCode(500, new { mensaje = "Error inesperado interno.", detalle = ex.Message });
            }
        }

        //public async Task<IActionResult> NotificarUsuario([FromBody] AdjudicacionRequest request)
        //{
        //    var adjudicacionDTO = await _requisicionService.ObtenerAdjudicacionAsync(request.ReqIdClave, request.ProveedorId, request.provIdSoc);

        //    if (adjudicacionDTO == null || adjudicacionDTO.Productos.Count == 0)
        //    {
        //        _logger.LogWarning("No se encontraron productos adjudicados para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", request.ReqIdClave, request.ProveedorId);
        //        return NotFound("No se encontraron productos adjudicados.");
        //    }

        //    //var html = MailBuilder.GenerarCorreoUsuarioAut(adjudicacionDTO);

        //    var resultado1 = await _requisicionService.ActualizarReqProvGanAsync(request.ReqIdClave, request.ProveedorId, request.provIdSoc);

        //    var resultado = await _requisicionService.ObtenerRequisiciones (request.ReqIdClave);

        //    foreach (var item in resultado)
        //    {
        //        Console.WriteLine($"{item.ReqIdClave} - {item.UsrNombre} {item.UsrApellidoP}");
        //    }

        //    var resultado2 = await _mailService.EnviarCorreoUsuAutAsyncTask(adjudicacionDTO, resultado, request.EmailUsuario);
        //   return resultado2.Success ? Ok(resultado) : StatusCode(500, resultado);
        //}


    }
}



