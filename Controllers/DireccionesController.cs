using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RequisicionesApi.Dtos;
using RequisicionesApi.Models;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;


namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DireccionesController : ControllerBase
    {

        private readonly string _connStr;



        public DireccionesController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")!;
        }


        [HttpGet("codigo-postal/{cp}")]
        [ProducesResponseType(typeof(IEnumerable<DireccionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByCodigoPostal(string cp, CancellationToken ct)
        {
            // Validación de CP mexicano: 5 dígitos
            var cpNorm = cp?.Trim();
            if (string.IsNullOrEmpty(cpNorm) || !Regex.IsMatch(cpNorm, @"^\d{5}$"))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validación",
                    Detail = "El Código Postal debe ser de 5 dígitos numéricos (ej. 01234)."
                });
            }

            const string sql = @"
SELECT IdDir, CodigoPostalDir, CveColoniaDir, NombreColoniaDir,
       CveMunicipioDir, NombreMunicipioDir, CveLocalidadDir, NombreLocalidadDir,
       CveEstadoDir, NombreEstadoDir, FechaCreacionDir, UsuarioCreacionDir
FROM dbo.tbldirecciones
WHERE RTRIM(LTRIM(CodigoPostalDir)) = @cp
ORDER BY NombreColoniaDir;";

            try
            {
                await using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync(ct);

                using var cmd = new SqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text
                };

                // Parametriza la consulta (evita SQL injection y cuida char(5))
                cmd.Parameters.Add("@cp", SqlDbType.VarChar, 5).Value = cpNorm;

                using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

                var list = new List<DireccionDto>();
                while (await reader.ReadAsync(ct))
                {
                    var item = new DireccionDto
                    {
                        IdDir = reader.GetInt32(0),
                        // Trim para quitar espacios de char(5)
                        CodigoPostalDir = reader.GetString(1).Trim(),
                        CveColoniaDir = reader.IsDBNull(2) ? null : reader.GetString(2),
                        NombreColoniaDir = reader.IsDBNull(3) ? null : reader.GetString(3),
                        CveMunicipioDir = reader.IsDBNull(4) ? null : reader.GetString(4),
                        NombreMunicipioDir = reader.IsDBNull(5) ? null : reader.GetString(5),
                        CveLocalidadDir = reader.IsDBNull(6) ? null : reader.GetString(6),
                        NombreLocalidadDir = reader.IsDBNull(7) ? null : reader.GetString(7),
                        CveEstadoDir = reader.GetString(8),
                        NombreEstadoDir = reader.GetString(9),
                        FechaCreacionDir = reader.GetDateTime(10),
                        UsuarioCreacionDir = reader.IsDBNull(11) ? null : reader.GetString(11)
                    };

                    list.Add(item);
                }

                return Ok(list);
            }
            catch (SqlException ex)
            {
                // Error de base de datos
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Error de base de datos",
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Error no controlado
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Error inesperado",
                    Detail = ex.Message
                });
            }
        }



        [HttpGet("cargas")]
        [ProducesResponseType(typeof(IEnumerable<CargaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCargas(CancellationToken ct)
        {
            const string sql = @"SELECT idCarga, carDescripcion FROM dbo.tblCargas ORDER BY idCarga;";
            try
            {
                await using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync(ct);

                using var cmd = new SqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text
                };

                using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

                var list = new List<CargaDto>();
                while (await reader.ReadAsync(ct))
                {
                    var item = new CargaDto
                    {
                        idCarga = reader.GetInt32(0),
                        carDescripcion = reader.IsDBNull(1) ? null : reader.GetString(1)
                    };
                    list.Add(item);
                }

                return Ok(list);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Error de base de datos",
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Error inesperado",
                    Detail = ex.Message
                });
            }
        }


    }

}
