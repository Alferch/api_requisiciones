using Microsoft.AspNetCore.Mvc;
using RequisicionesApi.Models.Autorizacion;
using RequisicionesApi.Repositorios;

namespace RequisicionesApi.Controllers
{
    [ApiController]
    [Route("api/flujo")]
    public sealed class FlujoAutController : ControllerBase
    {
        private readonly FlujoRepository _repo;
        //private readonly IEmailService _email;

        public FlujoAutController(FlujoRepository repo ) //, IEmailService email)
        {
            _repo = repo;
            //_email = email;
        }

        /// POST /api/flujo/enviar
        /// Lee los niveles desde tblRequisiciones.reqNiveles_Aut y genera PENDIENTES en dbo.tblFlujoAut.
        /// Si ValidarContraMatriz=true, solo compara contra tblAuthMatrix (no sobreescribe).
        [HttpPost("enviar")]
        public async Task<IActionResult> Enviar([FromBody] EnviarAutorizacionRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.ReqId))
                return BadRequest("ReqId requerido.");

            // 1) Niveles desde cabecera
            var csv = await _repo.GetReqNivelesCsvAsync(req.ReqId);
            if (string.IsNullOrWhiteSpace(csv))
                return BadRequest("La requisición no tiene niveles de autorización en reqNiveles_Aut.");

            // 2) (Opcional) Validar contra matriz (no modifica)
            if (req.ValidarContraMatriz)
            {
                var soc = await _repo.GetSociedadByReqAsync(req.ReqId);
                if (string.IsNullOrEmpty(soc)) return NotFound("No se encontró la sociedad de la requisición.");

                var ccs = await _repo.GetDistinctCCByReqAsync(req.ReqId);
                if (ccs.Count == 0) return BadRequest("La requisición no tiene detalle con centro de costo.");

                string cc;
                if (ccs.Count == 1)
                {
                    cc = ccs[0];
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(req.CentroCosto))
                        return BadRequest("La requisición tiene varios CC; indica CentroCosto para validar.");
                    if (!ccs.Contains(req.CentroCosto))
                        return BadRequest("El CentroCosto indicado no pertenece al detalle de la requisición.");
                    cc = req.CentroCosto!;
                }

                if (req.Importe is null || string.IsNullOrWhiteSpace(req.Moneda))
                    return BadRequest("Para validar contra la matriz envía Importe y Moneda.");

                var matrizCsv = await _repo.ResolverNivelesCsvAsync(soc!, cc, req.Moneda!, req.Importe.Value);
                if (string.IsNullOrWhiteSpace(matrizCsv))
                    return BadRequest("La matriz no tiene una regla para los parámetros especificados.");

                string norm(string s) => string.Join(",", s.Split(',').Select(x => x.Trim()).Where(x => x != ""));
                if (!string.Equals(norm(csv!), norm(matrizCsv), StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new
                    {
                        mensaje = "reqNiveles_Aut difiere de la matriz.",
                        reqNiveles_Aut = csv,
                        matrizSugerida = matrizCsv
                    });
                }
            }

            // 3) Generar pendientes
            await _repo.CrearPendientesAsync(req.ReqId, csv!);

            // 4) Responder estado
            var estado = await _repo.GetEstadoAsync(req.ReqId, req.Moneda ?? "", req.Importe);
            return Ok(estado);
        }

        /// POST /api/flujo/aprobar
        /// Aprueba el nivel actual si el usuario posee ese nivel en tblUsuario.usrNiveles_Aut.
        [HttpPost("aprobar")]
        public async Task<IActionResult> Aprobar([FromBody] AprobarNivelRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.ReqId) || string.IsNullOrWhiteSpace(req.UserId))
                return BadRequest("ReqId y UserId requeridos.");

            var nivelActual = await _repo.GetNivelActualAsync(req.ReqId);
            if (nivelActual is null)
                return BadRequest("No hay niveles pendientes (ya terminó, fue rechazada o cancelada).");

            var autorizado = await _repo.UsuarioTieneNivelAsync(req.UserId, nivelActual);
            if (!autorizado)
                return Forbid($"El usuario {req.UserId} no tiene el nivel {nivelActual}.");

            await _repo.MarcarNivelAsync(req.ReqId, nivelActual, req.UserId, "APROBADO", req.Comentario);

            var quedan = await _repo.ExistenPendientesAsync(req.ReqId);
            if (!quedan)
                await _repo.SelloVoBoFinalAsync(req.ReqId);

            return Ok(new { req.ReqId, nivelAprobado = nivelActual, flujoCompleto = !quedan });
        }

        /// POST /api/flujo/rechazar
        /// Rechaza el nivel actual (no avanza más el flujo).
        [HttpPost("rechazar")]
        public async Task<IActionResult> Rechazar([FromBody] AprobarNivelRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.ReqId) || string.IsNullOrWhiteSpace(req.UserId))
                return BadRequest("ReqId y UserId requeridos.");

            var nivelActual = await _repo.GetNivelActualAsync(req.ReqId);
            if (nivelActual is null)
                return BadRequest("No hay niveles pendientes.");

            var autorizado = await _repo.UsuarioTieneNivelAsync(req.UserId, nivelActual);
            if (!autorizado)
                return Forbid($"El usuario {req.UserId} no tiene el nivel {nivelActual}.");

            await _repo.MarcarNivelAsync(req.ReqId, nivelActual, req.UserId, "RECHAZADO", req.Comentario);
            return Ok(new { req.ReqId, nivelRechazado = nivelActual });
        }

        /// POST /api/flujo/cancelar
        /// Cancela la requisición: convierte todos los PENDIENTE en RECHAZADO (comentario “Cancelado”)
        /// y sella reqFecCanc / reqFecFin / reqHrFin en cabecera.
        [HttpPost("cancelar")]
        public async Task<IActionResult> Cancelar([FromBody] CancelarRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.ReqId) || string.IsNullOrWhiteSpace(req.UserId))
                return BadRequest("ReqId y UserId requeridos.");

            await _repo.CancelarAsync(req.ReqId, req.UserId, req.Motivo);
            return Ok(new { req.ReqId, canceladoPor = req.UserId, motivo = req.Motivo ?? "Cancelado" });
        }

        /// GET /api/flujo/pendientes?usrIdClave=...&usrIdSoc=...
        /// Devuelve las requisiciones que el usuario puede aprobar (según sus niveles y su CeCo).
        [HttpGet("pendientes")]
        public async Task<IActionResult> Pendientes([FromQuery] string usrIdClave, [FromQuery] string usrIdSoc)
        {
            if (string.IsNullOrWhiteSpace(usrIdClave) || string.IsNullOrWhiteSpace(usrIdSoc))
                return BadRequest("usrIdClave y usrIdSoc son requeridos.");

            var lista = await _repo.GetPendientesParaUsuarioAsync(usrIdClave, usrIdSoc);
            return Ok(lista);
        }


        /// GET /api/flujo/estado?reqId=...&moneda=MXN&importe=1234.56
        [HttpGet("estado")]
        public async Task<ActionResult<EstadoFlujoResponse>> Estado([FromQuery] string reqId, [FromQuery] string? moneda, [FromQuery] decimal? importe)
        {
            if (string.IsNullOrWhiteSpace(reqId))
                return BadRequest("ReqId requerido.");

            var estado = await _repo.GetEstadoAsync(reqId, moneda ?? "", importe);
            return Ok(estado);
        }

        /// POST /api/flujo/notificar-proveedor
        /// Solo permite notificar si ya no hay pendientes y la requisición NO está cancelada.
        [HttpPost("notificar-proveedor")]
        public async Task<IActionResult> NotificarProveedor([FromBody] NotificacionProveedorRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.ReqId) || string.IsNullOrWhiteSpace(req.ProveedorEmail))
                return BadRequest("ReqId y ProveedorEmail requeridos.");

            // Bloqueo por cancelación
            var cancelada = await _repo.FueCanceladaAsync(req.ReqId);
            if (cancelada)
                return BadRequest("La requisición está cancelada; no se notifica al proveedor.");

            var quedan = await _repo.ExistenPendientesAsync(req.ReqId);
            if (quedan)
                return BadRequest("Aún hay niveles pendientes; no se puede notificar al proveedor.");

            var subject = $"Requisición {req.ReqId} autorizada";
            var body = $"Estimado proveedor,\n\nLa requisición {req.ReqId} fue autorizada. Favor de proceder según instrucciones del comprador.\n\nSaludos.";
            //await _email.SendAsync(req.ProveedorEmail, subject, body);

            await _repo.SelloNotificadoProveedorAsync(req.ReqId);
            return Ok(new { req.ReqId, notificadoA = req.ProveedorEmail });
        }
    }
}
