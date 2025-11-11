using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HisPedidosController : ControllerBase
    {

        private readonly IConfiguration _config;
        private readonly string _connStr;

        public HisPedidosController(IConfiguration config)
        {
            _config = config;
            _connStr = _config.GetConnectionString("DefaultConnection");
        }

        [HttpGet("Idmaterial/{idMaterial}")]
        public ActionResult<IEnumerable<string>> BuscarPorMaterial(string idMaterial)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand("SELECT DISTINCT hisProvId FROM tblHisPedidos WHERE hisIdMaterial = @mat", conn);
            cmd.Parameters.AddWithValue("@mat", idMaterial);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            var lista = new List<string>();
            while (rdr.Read())
                lista.Add(rdr["hisProvId"].ToString());

            // Cache headers
            Response.Headers.CacheControl = "public,max-age=300";
            Response.Headers.ETag = GenerarEtag(idMaterial);

            return lista.Any() ? Ok(lista) : NotFound("No hay proveedores asociados al material.");
        }

        [HttpGet("descripcion")]
        public ActionResult<IEnumerable<string>> BuscarPorDescripcion([FromQuery] string descripcion)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand("SELECT DISTINCT hisProvId FROM tblHisPedidos WHERE hisDescripción LIKE @desc", conn);
            cmd.Parameters.AddWithValue("@desc", $"%{descripcion}%");

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            var lista = new List<string>();
            while (rdr.Read())
                lista.Add(rdr["hisProvId"].ToString());

            Response.Headers.CacheControl = "public,max-age=300";
            Response.Headers.ETag = GenerarEtag(descripcion);

            return lista.Any() ? Ok(lista) : NotFound("No hay proveedores asociados a esa descripción.");
        }

        private string GenerarEtag(string valor)
        {
            var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(valor));
            return $"\"{Convert.ToHexString(hash)}\"";
        }

    }
}
