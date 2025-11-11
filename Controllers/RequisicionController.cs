using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RequisicionesApi.Dtos;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;
using RequisicionesApi.Services;
using System.Data;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequisicionesController : ControllerBase
    {


         private readonly string _connString;
         private readonly IRequisicionService _service;

        public RequisicionesController(IRequisicionService service, IConfiguration config)
        {
            _service = service;
            _connString = config.GetConnectionString("DefaultConnection")
                  ?? throw new System.Exception("Connection string 'DefaultConnection' no configurada.");
        }



        [HttpGet("reqdetalleprov/{clave}/{idSoc}")]
        public async Task<IActionResult> GetRequisicionesProv(string clave, int idSoc)
        {
            try
            {
                var resultado = await _service.GetRequisicionesProvAsync(clave, idSoc);

                if (resultado == null || resultado.Count == 0)
                    return NotFound(new { message = "No se encontraron requisiciones." });

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                // Log del error (puedes usar ILogger)
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }






        //lanza los correos a los proveedores y asigna fecha de VoBo
        [HttpPost("detalle-prov/lote")]
        public async Task<IActionResult> GuardarDetalleProvLote(
            [FromQuery] string soc,
            [FromBody] AutProvReqCerradaDetalleProvDtoList request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(soc))
                return BadRequest(new { error = "El parámetro 'soc' es obligatorio." });

            if (request.Modelos == null || request.Modelos.Count == 0)
                return BadRequest(new { error = "Se requiere una lista con al menos un elemento." });

            try
            {
                var result = await _service.GuardarDetalleProvAsync(soc, request, ct);

                return Ok(new
                {
                    ok = result.Ok,
                    procesados = result.Procesados,
                    detalles = result.DetallesUpserted,
                    proveedoresInsertados = result.ProveedoresInsertados,
                    vobosAplicados = result.VobosAplicados
                });
            }
            catch (InvalidOperationException ex)
            {
                // Escenario típico: VoBo no encontró encabezado -> rollback total
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
  




        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] RequisicionDto requisicion)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var clave = await _service.CrearAsync(requisicion);
                return Ok(new { message = "Requisición creada", reqIdClave = clave });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpGet("id/{id}")]
        public async Task<IActionResult> Get(string id, string soc  )
        {
            try
            {
                var result = await _service.ObtenerRequisicionAsync(id, soc);
                if (result == null)
                    return NotFound(new { message = $"Requisición {id} no encontrada" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("sociedad/{soc}/{idusuario}")]
        public async Task<IActionResult> Get2( string soc,  string idusuario )
         { 
            try
            {
                var result = await _service.ObtenerTodasAsync(soc, idusuario);
                if (result == null)
                    return NotFound(new { message = $"la sociedad no cuenta con Requisiciónes" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("comparativo/{soc}/{requisicion}")]
        public async Task<IActionResult> Get1(string soc, string requisicion, [FromQuery] string? opcion = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(soc) || string.IsNullOrWhiteSpace(requisicion))
                    return BadRequest(new { error = "Sociedad y requisición son obligatorias." });

                var result = await _service.ObtenerEvalAsync(soc, requisicion, opcion ?? "");

                if (result == null || !result.Any())
                    return NotFound(new { message = $"No se encontró información para sociedad '{soc}' y requisición '{requisicion}'." });

              //  if (result[0].reqNotifFecUsr == "1" )
              //      return BadRequest(new { message = $"la requisición '{requisicion}' ya se encuentra en proceso de autorizacion." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno", detalle = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] RequisicionDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.ActualizarRequisicionAsync(model);
                return result ? Ok(new { message = "Requisición actualizada" }) : NotFound(new { message = "No se encontró la requisición para actualizar" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _service.EliminarRequisicionAsync(id);
                return result ? Ok(new { message = "Requisición eliminada" }) : NotFound(new { message = "No se encontró la requisición para eliminar" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("cerradas")]
        public async Task<IActionResult> GetCerradas(CancellationToken ct)
        {
            try
            {
                var data = await _service.ListarCerradasAsync(ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error listando requisiciones cerradas");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ============= NUEVO 2 =============
        // GET: api/Requisiciones/cerradas/{id}/detalles
        [HttpGet("cerradas/{id}/detalles")]
        public async Task<IActionResult> GetCerradaDetalles([FromRoute] string id, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(new { error = "El parámetro 'id' es obligatorio." });

                var data = await _service.ListarCerradaDetallesAsync(id, ct);
                if (data == null || data.Count == 0)
                    return NotFound(new { message = $"Sin detalles para la requisición {id} (o no está cerrada)." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error listando detalles de requisición {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("cerradas/{idSociedad}/{reqId}/detallesProv")]
        public async Task<IActionResult> GetCerradasDetalles(string idSociedad, string reqId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idSociedad) || string.IsNullOrWhiteSpace(reqId))
                    return BadRequest(new { error = "Sociedad y requisición son obligatorias." });

                var result = await _service.ObtenerCerradasDetallesAsync(idSociedad, reqId);

                if (result == null || !result.Any())
                    return NotFound(new
                    {
                        message = $"No hay detalles para la requisición '{reqId}' en la sociedad '{idSociedad}'."
                    });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno", detalle = ex.Message });
            }
        }


        [HttpPost("cancelar")]
        public async Task<IActionResult> Cancelar(
      [FromQuery(Name = "idRequisicion")] string idRequisicion,
      [FromQuery(Name = "idSociedad")] int idSociedad,
      CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(idRequisicion))
                return BadRequest("El query param 'idRequisicion' es requerido.");
            if (idSociedad <= 0)
                return BadRequest("El query param 'idSociedad' debe ser un entero > 0.");

            var filas = await _service.CancelarAsync(idSociedad, idRequisicion.Trim(), ct);
            if (filas == 0)
                return NotFound("No se encontró la requisición para los parámetros especificados.");

            return Ok(new { ok = true, idRequisicion, idSociedad, filasAfectadas = filas });
        }


        /*
        [HttpGet("cerradas/{reqId}/detallesProv1")]
        public async Task<IActionResult> GetCerradasDetalles1(string reqId, )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reqId))
                    return BadRequest(new { error = "Requisición es obligatoria." });

                var result = await _service.ObtenerCerradasDetallesAsync(reqId);

                if (result == null || !result.Any())
                    return NotFound(new { message = $"No hay detalles para la requisición '{reqId}'." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno", detalle = ex.Message });
            }
        }  */


            /// <summary>
        /// GET /api/requisiciones/timeline?usrIdSoc=1&estados=Creada,Capturada,VoBo,Notificada,Cancelada
        /// o múltiple: ?estados=Creada&estados=VoBo
        /// </summary>
        [HttpGet("timeline")]
        public async Task<ActionResult<IEnumerable<RequisicionTimelineDto>>> GetTimeline(
            [FromQuery] string usrIdSoc,
            [FromQuery] List<string> estados,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(usrIdSoc))
                return BadRequest("usrIdSoc es requerido.");

            if (estados == null || estados.Count == 0)
                return BadRequest("Debe indicar al menos un estado en 'estados'.");

            var data = await _service.GetTimelineAsync(usrIdSoc, estados, ct);
            return Ok(data);
        }




        /// <summary>
        /// GET /api/hispedidos/proveedores?campo=...&opcin=...
        /// opcin=1 -> por descripción (LIKE %campo%)
        /// opcin=2 -> por hisidMaterial (= campo)
        /// Retorna DISTINCT hisProvId no nulos/ni vacíos.
        /// </summary>
        [HttpGet("GetProveedoresPorMaterial")]
        public async Task<ActionResult<IEnumerable<string>>> GetProveedores(
                [FromQuery] string campo,
                [FromQuery] int opcin,
                CancellationToken ct)
            {
                if (string.IsNullOrWhiteSpace(campo))
                    return BadRequest("El parámetro 'campo' es requerido.");

                if (opcin != 1 && opcin != 2)
                    return BadRequest("El parámetro 'opcin' debe ser 1 (descripción) o 2 (hisidMaterial).");

                // Queries EXACTOS según tu instrucción, con parámetros para evitar inyección.
                // Se agrega DISTINCT en el SELECT.
                string sql;
                var parameters = new List<SqlParameter>();

                if (opcin == 1)
                {
                    // Por descripción: LIKE %campo%
                    sql = @"
SELECT DISTINCT hp.hisProvId
FROM dbo.tblHisPedidos AS hp
WHERE hp.hisidMaterial IN (
    SELECT mm.mmatIdClave
    FROM dbo.tblMaestroMaterial AS mm
    WHERE mm.mmatDescripción LIKE '%' + @campo + '%'
)
AND hp.hisProvId IS NOT NULL
AND LEN(LTRIM(RTRIM(hp.hisProvId))) > 0;";
                    parameters.Add(new SqlParameter("@campo", SqlDbType.NVarChar, 4000) { Value = campo });
                }
                else
                {
                    // opcin == 2 -> por hisidMaterial: IN (@campo) (equivalente a '=' para un valor)
                    sql = @"
SELECT DISTINCT hp.hisProvId
FROM dbo.tblHisPedidos AS hp
WHERE hp.hisidMaterial IN (@campo)
AND hp.hisProvId IS NOT NULL
AND LEN(LTRIM(RTRIM(hp.hisProvId))) > 0;";
                    parameters.Add(new SqlParameter("@campo", SqlDbType.NVarChar, 200) { Value = campo });
                }

                var resultados = new List<string>();

                try
                {
                    await using var conn = new SqlConnection(_connString);
                    await conn.OpenAsync(ct);

                    await using var cmd = new SqlCommand(sql, conn);
                    foreach (var p in parameters) cmd.Parameters.Add(p);
                    cmd.CommandType = CommandType.Text;

                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        // hisProvId podría ser NVARCHAR/VARCHAR
                        var val = reader.GetString(0)?.Trim();
                        if (!string.IsNullOrEmpty(val))
                            resultados.Add(val);
                    }
                }
                catch (SqlException ex)
                {
                    // Devuelve 500 con el mensaje base (sin detalles sensibles)
                    return Problem($"Error al consultar SQL Server: {ex.Message}");
                }

                return Ok(resultados);
            }


        /// GET /api/requisiciones/pendientes?usuario=U12345&sociedad=IMX1&idRol=R06
        [HttpGet("pendientesAut")]
        public async Task<IActionResult> GetPendientes(
            [FromQuery] string? usuario,
            [FromQuery] string? sociedad,
            [FromQuery] string? idRol,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(sociedad))
                return BadRequest(new
                {
                    title = "Parámetros inválidos",
                    detail = "Debes enviar 'usuario' y 'sociedad'. Ej: ?usuario=U12345&sociedad=IMX1&idRol=R06"
                });

            idRol ??= "R06";

            try
            {
                var data = await _service.GetPendientesAsync(usuario.Trim(), sociedad.Trim(), idRol.Trim(), ct);
                return Ok(data);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { title = "Error SQL", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { title = "Error interno", detail = ex.Message });
            }
        }


    }
}
