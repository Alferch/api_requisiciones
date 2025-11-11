using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Reflection.PortableExecutable;
//using System.Data.SqlClient;

[ApiController]
[Route("api/[controller]")]
public class MaestroMaterialController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly string _conn;

    public MaestroMaterialController(IConfiguration config)
    {
        _config = config;
        _conn = _config.GetConnectionString("DefaultConnection");
    }

    [HttpGet]
    public ActionResult<IEnumerable<MaestroMaterialModel>> GetAll()
    {
        var lista = new List<MaestroMaterialModel>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand("SELECT * FROM tblMaestroMaterial", conn);
        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(Mapear(reader));
        }
        return Ok(lista);
    }

    [HttpGet("buscar")]
    public ActionResult<IEnumerable<MaestroMaterialModel>> Buscar([FromQuery] string descripcion)
    {
        var lista = new List<MaestroMaterialModel>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand("SELECT * FROM tblMaestroMaterial WHERE mmatDescripción LIKE @desc", conn);
        cmd.Parameters.AddWithValue("@desc", $"%{descripcion}%");

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(Mapear(reader));
        }
        return Ok(lista);
    }

    [HttpGet("{clave}")]
    public ActionResult<MaestroMaterialModel> BuscarPorClave(string clave)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand("SELECT * FROM tblMaestroMaterial WHERE mmatIdClave = @clave", conn);
        cmd.Parameters.AddWithValue("@clave", clave);

        conn.Open();
        using var rdr = cmd.ExecuteReader();
        if (!rdr.Read())
            return NotFound($"No se encontró el registro con clave '{clave}'.");

        var model = Map(rdr);
        var eTag = GenerarETag(model);

        // Verifica si el cliente ya tiene la versión más reciente
        if (Request.Headers.TryGetValue("If-None-Match", out var clienteETag) && clienteETag == eTag)
        {
            Response.Headers.CacheControl = "public,max-age=300";
            Response.Headers.ETag = eTag;
            return StatusCode(304); // Not Modified
        }

        Response.Headers.CacheControl = "public,max-age=300";
        Response.Headers.ETag = eTag;
        return Ok(model);
    }

    private static string GenerarETag(MaestroMaterialModel model)
    {
        var raw = $"{model.mmatIdClave}-{model.mmatFechaultpedido:yyyyMMddHHmmss}";
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return $"\"{Convert.ToHexString(hash)}\"";
    }

    [HttpPost]
    public ActionResult<MaestroMaterialModel> Crear([FromBody] MaestroMaterialModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"INSERT INTO tblMaestroMaterial VALUES 
            (@clave,@soc,@comp,@desc,@tipo,@grupo,@unidad,@moneda,@precio,@exist,@ultped,@fechault,@estado,@especific)", conn);

        AgregarParametros(cmd, model);
        conn.Open();
        int insertados = cmd.ExecuteNonQuery();
        return insertados > 0 ? CreatedAtAction(nameof(GetAll), new { id = model.mmatIdClave }, model)
                              : StatusCode(500, "No se pudo insertar el registro.");
    }

    [HttpPut("{clave}")]
    public ActionResult Actualizar(string clave, [FromBody] MaestroMaterialModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"UPDATE tblMaestroMaterial SET 
            mmatIdSoc=@soc, mmatIdCompleto=@comp, mmatDescripción=@desc, mmatTipoM=@tipo,
            mmatIdGrupoM=@grupo, mmatUnidadMedida=@unidad, mmatMoneda=@moneda,
            mmatPrecioMM=@precio, mmatExistencia=@exist, mmatUltimopedido=@ultped,
            mmatFechaultpedido=@fechault, mmatEstado=@estado, mmatEspecificaciones=@especific 
            WHERE mmatIdClave=@clave", conn);

        AgregarParametros(cmd, model);
        cmd.Parameters.AddWithValue("@clave", clave);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0 ? Ok("Actualizado") : NotFound("No encontrado");
    }

    [HttpDelete("{clave}")]
    public ActionResult Eliminar(string clave)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand("DELETE FROM tblMaestroMaterial WHERE mmatIdClave=@clave", conn);
        cmd.Parameters.AddWithValue("@clave", clave);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0 ? Ok("Eliminado") : NotFound("No encontrado");
    }

    // Métodos auxiliares
    private static MaestroMaterialModel Mapear(SqlDataReader rdr) => new()
    {
        mmatIdClave = rdr["mmatIdClave"].ToString(),
        mmatIdSoc = rdr["mmatIdSoc"].ToString(),
        mmatIdCompleto = rdr["mmatIdCompleto"].ToString(),
        mmatDescripción = rdr["mmatDescripción"].ToString(),
        mmatTipoM = rdr["mmatTipoM"].ToString(),
        mmatIdGrupoM = rdr["mmatIdGrupoM"].ToString(),
        mmatUnidadMedida = rdr["mmatUnidadMedida"].ToString(),
        mmatMoneda = rdr["mmatMoneda"].ToString(),
        mmatPrecioMM = rdr["mmatPrecioMM"].ToString(),
        mmatExistencia = rdr["mmatExistencia"]?.ToString(),
        mmatUltimopedido = rdr["mmatUltimopedido"]?.ToString(),
        mmatFechaultpedido = Convert.ToDateTime(rdr["mmatFechaultpedido"]),
        mmatEstado = rdr["mmatEstado"].ToString(),
        mmatEspecificaciones = rdr["mmatEspecificaciones"]?.ToString()
    };

    private static void AgregarParametros(SqlCommand cmd, MaestroMaterialModel m)
    {
        cmd.Parameters.AddWithValue("@soc", m.mmatIdSoc);
        cmd.Parameters.AddWithValue("@comp", m.mmatIdCompleto);
        cmd.Parameters.AddWithValue("@desc", m.mmatDescripción);
        cmd.Parameters.AddWithValue("@tipo", m.mmatTipoM);
        cmd.Parameters.AddWithValue("@grupo", m.mmatIdGrupoM);
        cmd.Parameters.AddWithValue("@unidad", m.mmatUnidadMedida);
        cmd.Parameters.AddWithValue("@moneda", m.mmatMoneda);
        cmd.Parameters.AddWithValue("@precio", m.mmatPrecioMM);
        cmd.Parameters.AddWithValue("@exist", (object?)m.mmatExistencia ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ultped", (object?)m.mmatUltimopedido ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@fechault", m.mmatFechaultpedido);
        cmd.Parameters.AddWithValue("@estado", m.mmatEstado);
        cmd.Parameters.AddWithValue("@especific", (object?)m.mmatEspecificaciones ?? DBNull.Value);
    }

    private MaestroMaterialModel Map(SqlDataReader rdr)
    {
        return new MaestroMaterialModel
        {
            mmatIdClave = rdr["mmatIdClave"].ToString(),
            mmatIdSoc = rdr["mmatIdSoc"].ToString(),
            mmatIdCompleto = rdr["mmatIdCompleto"].ToString(),
            mmatDescripción = rdr["mmatDescripción"].ToString(),
            mmatTipoM = rdr["mmatTipoM"].ToString(),
            mmatIdGrupoM = rdr["mmatIdGrupoM"].ToString(),
            mmatUnidadMedida = rdr["mmatUnidadMedida"].ToString(),
            mmatMoneda = rdr["mmatMoneda"].ToString(),
            mmatPrecioMM = rdr["mmatPrecioMM"].ToString(),
            mmatExistencia = rdr["mmatExistencia"]?.ToString(),
            mmatUltimopedido = rdr["mmatUltimopedido"]?.ToString(),
            mmatFechaultpedido = Convert.ToDateTime(rdr["mmatFechaultpedido"]),
            mmatEstado = rdr["mmatEstado"].ToString(),
            mmatEspecificaciones = rdr["mmatEspecificaciones"]?.ToString()
        };
    }
}