using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;
using RequisicionesApi.Services;
using System.Data;
using static RequisicionesApi.Models.Autorizacion.AutorizacionDtos;

namespace RequisicionesApi.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class AutorizacionController : ControllerBase
    {
        private readonly IRequisicionService _requisicionService;    //FF
        private readonly ILogger<AdjudicacionController> _logger;     //FF
        private readonly IMailService _mailService;    //FF

        private readonly IAutorizacionService _svc;
           // public AutorizacionController(IAutorizacionService svc) => _svc = svc;


        public AutorizacionController(
            IAutorizacionService svc,
            IMailService mailService,
            ILogger<AdjudicacionController> logger,
            IRequisicionService requisicionService  
            )

        {
            _svc = svc;
            _mailService = mailService;
            _logger = logger;
             _requisicionService = requisicionService;
        }




        /// POST: api/autorizacion/flujo/generar
        [HttpPost("flujo/generar")]
            public async Task<IActionResult> GenerarFlujo([FromBody] GenerarFlujoRequest req, CancellationToken ct)
            {
                await _svc.GenerarFlujoAsync(req, ct);
            //var resultado = await _mailService.EnviarCorreoAdjudicacionAsync(adjudicacionDTO, request.EmailUsuario);
            return Ok(new { ok = true, message = "Flujo generado/actualizado." });
            }

            /// POST: api/autorizacion/accion
            /// Body: { reqIdClave, usuario, accion: APROBAR|RECHAZAR|CANCELAR, comentario? }
            [HttpPost("accion")]
            public async Task<IActionResult> Accion([FromBody] AccionRequest req, CancellationToken ct)
            {
            //    await _svc.AutorizarAsync(req, ct);
            //    return Ok(new { ok = true, message = $"Acción {req.Accion} aplicada." });
            try
            {
                await _svc.AutorizarAsync(req, ct);


                // obtene siguiente nivel para mandar correo
                DataTable flujoAut =  await _svc.GetFlujoAutPendienteAsync(req.ReqIdClave);


                if(flujoAut.Rows.Count > 0)
{
                    foreach (DataRow row in flujoAut.Rows)
                    {
                        string reqIdClave = row["reqIdClave"].ToString();
                        string correo = row["usrCorreo"].ToString();

                        if (correo == "completo")
                        {
                            Console.WriteLine("No hay registros pendientes. Estado: COMPLETO");

                            string proveedor = await _svc.GetProveedorGanador(req.ReqIdClave);
                            var adjudicacionDTOInterno = await _requisicionService.ObtenerAdjudicacionAsync(req.ReqIdClave, proveedor, "1000");
                            if (adjudicacionDTOInterno == null || adjudicacionDTOInterno.Productos.Count == 0)
                            {
                                _logger.LogWarning("No se encontraron productos adjudicados para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", req.ReqIdClave, proveedor);
                                return NotFound("No se encontraron productos adjudicados.");
                            }
                            string correoProv = await _svc.GetProveedorCorreoAsync(proveedor);
                            var resultado = await _mailService.EnviarCorreoAdjudicacionAsync(adjudicacionDTOInterno, correoProv);

                        }
                        else
                        {    // envia el correo al nivel pendiente
                            Console.WriteLine($"Clave: {reqIdClave}, Nivel: {row["reqLevelCode"]}, Creado: {row["reqCreado"]}, Usuario: {row["usrIdClave"]}, Correo: {correo}");

                            string proveedor = await _svc.GetProveedorGanador(req.ReqIdClave);
                            var adjudicacionDTOInterno = await _requisicionService.ObtenerAdjudicacionAsync(req.ReqIdClave, proveedor, "1000");
                            var resultado = await _requisicionService.ObtenerRequisiciones(req.ReqIdClave);
                            var resultado2 = await _mailService.EnviarCorreoUsuAutLiberar(adjudicacionDTOInterno,resultado, correo);
                        }
}
                }
                else
                {
                    Console.WriteLine("No se devolvió ninguna fila.");
                }






                // si encuentra un nivel pendiente busca al usuario
                // 
                //si y no encuentra nivel pendiente manda la requisicion, pero debemos obtener el proveedor ganador
              //  var adjudicacionDTO = await _requisicionService.ObtenerAdjudicacionAsync(req.ReqIdClave, "0000010200", "1000");

              //  if (adjudicacionDTO == null || adjudicacionDTO.Productos.Count == 0)
              //  {
              //      _logger.LogWarning("No se encontraron productos adjudicados para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", req.ReqIdClave, "0000010200");
              //      return NotFound("No se encontraron productos adjudicados.");
              //  }

              //  var resultado = await _mailService.EnviarCorreoAdjudicacionAsync(adjudicacionDTO, "mariom.mendoza@bicmx.com");


                return Ok(new {  ok = true, message = $"Acción {req.Accion} aplicada." });
            }
            catch (Exception ex)
            {
                // Aquí podrías usar ILogger para registrar el error
                return StatusCode(500, new { message = "Error intentar Autorizar Requisicion " + req.ReqIdClave , detail = ex.Message });
            }




            }

            /// GET: api/autorizacion/pendientes?usuario=EE0001
            [HttpGet("pendientes")]
            public async Task<IActionResult> Pendientes([FromQuery] string usuario, CancellationToken ct)
            {
                var data = await _svc.GetPendientesAsync(usuario, ct);
                return Ok(data);
            }

            /// GET: api/autorizacion/timeline/{reqIdClave}
            [HttpGet("timeline/{reqIdClave}")]
            public async Task<IActionResult> Timeline([FromRoute] string reqIdClave, CancellationToken ct)
            {
                var data = await _svc.GetTimelineAsync(reqIdClave, ct);
                return Ok(data);
            }

            /// GET: api/autorizacion/estado/{reqIdClave}
            [HttpGet("estado/{reqIdClave}")]
            public async Task<IActionResult> EstadoActual([FromRoute] string reqIdClave, CancellationToken ct)
            {
                var data = await _svc.GetEstadoActualAsync(reqIdClave, ct);
                return Ok(data);
            }
        }
}


 