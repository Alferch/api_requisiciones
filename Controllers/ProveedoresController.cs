using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RequisicionesApi.Models;
using RequisicionesApi.Repositorios;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RequisicionesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   //  [Authorize]
    public class ProveedoresController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connStr;

        public ProveedoresController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connStr = _configuration.GetConnectionString("DefaultConnection");
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }


        [HttpGet("filtrar")]
        public async Task<IActionResult> GetProveedores([FromQuery] ProveedoresQueryDto query)
        {
            var proveedores = new List<ProveedorModel>();
            int total = 0;

            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            // Total count
            using (var countCmd = new SqlCommand(@"
            SELECT COUNT(*) FROM tblProveedores
            WHERE (@Id IS NULL OR provIdProv LIKE '%' + @Id + '%')
              AND (@Nombre IS NULL OR provNombre LIKE '%' + @Nombre + '%')
              AND (@RFC IS NULL OR provRFC LIKE '%' + @RFC + '%')", connection))
            {
                countCmd.Parameters.AddWithValue("@Id", (object?)query.Id ?? DBNull.Value);
                countCmd.Parameters.AddWithValue("@Nombre", (object?)query.Nombre ?? DBNull.Value);
                countCmd.Parameters.AddWithValue("@RFC", (object?)query.RFC ?? DBNull.Value);
                total = (int)await countCmd.ExecuteScalarAsync();
            }

            // Paged data
            using (var cmd = new SqlCommand(@"
            SELECT *
            FROM tblProveedores
            WHERE (@Id IS NULL OR provIdProv LIKE '%' + @Id + '%')
              AND (@Nombre IS NULL OR provNombre LIKE '%' + @Nombre + '%')
              AND (@RFC IS NULL OR provRFC LIKE '%' + @RFC + '%')
            ORDER BY provIdProv
            OFFSET @Offset ROWS FETCH NEXT @Tamano ROWS ONLY;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", (object?)query.Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Nombre", (object?)query.Nombre ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RFC", (object?)query.RFC ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Offset", (query.Pagina - 1) * query.Tamano);
                cmd.Parameters.AddWithValue("@Tamano", query.Tamano);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    proveedores.Add(new ProveedorModel
                    {
                        ProvIdSoc = reader["provIdSoc"].ToString()!,
                        provIdGrupoM = reader["provIdGrupoM"].ToString()!,
                        ProvIdProv = reader["provIdProv"].ToString()!,
                        ProvNombre = reader["provNombre"].ToString()!,
                        ProvRFC = reader["provRFC"].ToString()!,
                        ProvNomVendedor = reader["provNomVendedor"].ToString()!,
                        ProvTelefono = reader["provTeléfono"].ToString()!,
                        ProvCorreo = reader["provCorreo"].ToString()!,
                        ProvIdioma = reader["provIdioma"]?.ToString(),
                        ProvClasificacion = reader["ProvClasificacion"]?.ToString(),
                    });
                }
            }

            return Ok(new
            {
                total,
                pagina = query.Pagina,
                tamano = query.Tamano,
                datos = proveedores
            });
        }


        [HttpGet]
         public ActionResult<IEnumerable<ProveedorModel>> GetAll()
        {
            var result = new List<ProveedorModel>();
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("SELECT prov.provIdSoc,soc.socNombre,prov.provIdGrupoM ,gm.mmatDescripción,prov.provIdProv" +
                    " ,prov.provNombre ,prov.provRFC      ,prov.provNomVendedor      ,prov.provTeléfono      ,prov.provCorreo ," +
                    "prov.provIdioma     ,prov.ProvClasificacion  FROM dbo.tblProveedores prov" +
                    " inner join dbo.tblSociedad soc on soc.socIdSoc = prov.provIdSoc" +
                    " inner join dbo.tblGrupoMaterial gm on gm.mmatIdGrupoM = prov.provIdGrupoM", conn);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new ProveedorModel
                    {
                        ProvIdSoc = reader["provIdSoc"].ToString(),
                        ProvsocNombre = reader["socNombre"].ToString(),
                        provIdGrupoM = reader["provIdGrupoM"].ToString(),
                        provGrpoNombre = reader["mmatDescripción"].ToString(),
                        ProvIdProv = reader["provIdProv"].ToString(),
                        ProvNombre = reader["provNombre"].ToString(),
                        ProvNomVendedor = reader["provNombre"].ToString(),
                        ProvRFC = reader["provRFC"].ToString(),
                        ProvTelefono = reader["provTeléfono"].ToString(),
                        ProvCorreo = reader["provCorreo"].ToString(),
                        ProvIdioma = reader["provIdioma"]?.ToString(),
                        ProvClasificacion = reader["ProvClasificacion"]?.ToString()
                    });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al recuperar proveedores: {ex.Message}");
            }
        }





        [HttpGet("{id}")]
        public ActionResult<IEnumerable<ProveedorModel>> GetById(string id)
        {
            var result = new List<ProveedorModel>();
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("SELECT  CONVERT(nvarchar, prov.provIdSoc) provIdSoc   ,soc.socNombre, CONVERT(nvarchar, prov.provIdGrupoM) provIdGrupoM  ,gm.mmatDescripción, " +
                    " CONVERT(nvarchar, prov.provIdProv) provIdProv, prov.provNombre, prov.provRFC, prov.provNomVendedor, CONVERT(nvarchar, prov.provTeléfono) provTeléfono, prov.provCorreo, " +
                    "prov.provIdioma, prov.ProvClasificacion  FROM dbo.tblProveedores prov " +
                    " inner join dbo.tblSociedad soc on soc.socIdSoc = prov.provIdSoc " +
                    " inner join dbo.tblGrupoMaterial gm on gm.mmatIdGrupoM = prov.provIdGrupoM  where provIdProv = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new ProveedorModel
                    {

                        ProvIdSoc = reader["provIdSoc"].ToString()!,
                        ProvsocNombre = reader["socNombre"].ToString()!,
                        provIdGrupoM = reader["provIdGrupoM"].ToString()!,
                        provGrpoNombre = reader["mmatDescripción"].ToString()!,
                        ProvIdProv = reader["provIdProv"].ToString()!,
                        ProvNombre = reader["provNombre"].ToString()!,
                        ProvRFC = reader["provRFC"].ToString()!,
                        ProvNomVendedor = reader["provNomVendedor"].ToString()!,
                        ProvTelefono = reader["provTeléfono"].ToString()!,
                        ProvCorreo = reader["provCorreo"].ToString()!,
                        ProvIdioma = reader["provIdioma"]?.ToString(),
                        ProvClasificacion = reader["ProvClasificacion"]?.ToString(),

                    });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al recuperar proveedores: {ex.Message}");
            }
        }



        [HttpPost]
        public ActionResult Create([FromBody] ProveedorModel prov)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {

                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                INSERT INTO tblProveedores (provIdProv ,provIdSoc  , provIdGrupoM ,  provNombre , provRFC , provNomVendedor , provTeléfono , provCorreo , provIdioma , ProvClasificacion)
                VALUES
                (@id,@socId,@socIdGpo,@nombre,@rfc,@vendedor,@telefono,@correo,@idioma,@clasif)", conn);


                cmd.Parameters.AddWithValue("@id", prov.ProvIdProv);
                cmd.Parameters.AddWithValue("@socId", prov.ProvIdSoc);
                cmd.Parameters.AddWithValue("@socIdGpo", prov.provIdGrupoM);
                cmd.Parameters.AddWithValue("@nombre", prov.ProvNombre);
                cmd.Parameters.AddWithValue("@rfc", prov.ProvRFC);
                cmd.Parameters.AddWithValue("@vendedor", prov.ProvNomVendedor);
                cmd.Parameters.AddWithValue("@telefono", prov.ProvTelefono);
                cmd.Parameters.AddWithValue("@correo", prov.ProvCorreo);
                cmd.Parameters.AddWithValue("@idioma", (object?)prov.ProvIdioma ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@clasif", (object?)prov.ProvClasificacion ?? DBNull.Value);

                conn.Open();
                int rows = cmd.ExecuteNonQuery();

                return rows > 0 ? CreatedAtAction(nameof(GetAll), new { id = prov.ProvIdProv }, prov)
                                : StatusCode(500, "Error al insertar proveedor.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Excepción: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Models.ProveedorRepository prov)
        {



            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            var cmd = new SqlCommand(@"UPDATE tblProveedores SET 
            provIdSoc = @socId,
            provIdGrupoM = @grupopro,
            provNombre = @nombre,
            provRFC = @rfc,
            provNomVendedor = @vendedor,
            provTeléfono = @telefono, 
            provCorreo = @correo,
            provIdioma = @idioma,
            ProvClasificacion = @clasif
            WHERE provIdProv = @id", conn);
 



            cmd.Parameters.AddWithValue("@id", prov.ProvId);
            cmd.Parameters.AddWithValue("@socId", prov.ProvSocId.ToString());
            cmd.Parameters.AddWithValue("@grupopro", prov.provIdGrupoM);
            cmd.Parameters.AddWithValue("@nombre", prov.ProvNombre.ToString());
            cmd.Parameters.AddWithValue("@rfc", prov.ProvRFC.ToString());
            cmd.Parameters.AddWithValue("@vendedor", prov.ProvVendedor.ToString());
            cmd.Parameters.AddWithValue("@telefono", prov.ProvTelefono.ToString());
            cmd.Parameters.AddWithValue("@correo", prov.ProvCorreo.ToString());
            cmd.Parameters.AddWithValue("@idioma", prov.ProvIdioma?.ToString() ?? "");
            cmd.Parameters.AddWithValue("@clasif", prov.ProvClasificacion?.ToString() ?? "");





            conn.Open();
            int rows = cmd.ExecuteNonQuery();
            return rows > 0 ? Ok("Proveedor Actualizado correctamente") : NotFound("Proveedor no encontrado");
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = new SqlCommand("DELETE FROM tblProveedores WHERE ProvIdProv=@ProvIdProv", conn);
            cmd.Parameters.AddWithValue("@ProvIdProv", id);
            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? Ok("Proveedor eliminado correctamente") : NotFound();
        }
    }



}
