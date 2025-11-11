using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using RequisicionesApi.Models;

namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrupoMaterialController : ControllerBase
    {


        private readonly string _connectionString;

        public GrupoMaterialController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/GrupoMaterial
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GrupoMaterial>>> GetGrupos()
        {
            var grupos = new List<GrupoMaterial>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT mmatIdGrupoM, mmatDescripción FROM tblGrupoMaterial", conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                grupos.Add(new GrupoMaterial
                {
                    mmatIdGrupoM = reader["mmatIdGrupoM"].ToString(),
                    mmatDescripción = reader["mmatDescripción"].ToString()
                });
            }

            return grupos;
        }

        // GET: api/GrupoMaterial/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GrupoMaterial>> GetGrupo(string id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT mmatIdGrupoM, mmatDescripción FROM tblGrupoMaterial WHERE mmatIdGrupoM = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new GrupoMaterial
                {
                    mmatIdGrupoM = reader["mmatIdGrupoM"].ToString(),
                    mmatDescripción = reader["mmatDescripción"].ToString()
                };
            }

            return NotFound();
        }

        // POST: api/GrupoMaterial
        [HttpPost]
        public async Task<IActionResult> CreateGrupo([FromBody] GrupoMaterial grupo)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("INSERT INTO tblGrupoMaterial (mmatIdGrupoM, mmatDescripción) VALUES (@id, @desc)", conn);
            cmd.Parameters.AddWithValue("@id", grupo.mmatIdGrupoM);
            cmd.Parameters.AddWithValue("@desc", grupo.mmatDescripción);

            await conn.OpenAsync();
            try
            {
                await cmd.ExecuteNonQueryAsync();
                return CreatedAtAction(nameof(GetGrupo), new { id = grupo.mmatIdGrupoM }, grupo);
            }
            catch (SqlException ex) when (ex.Number == 2627) // PK violation
            {
                return Conflict("Ya existe un grupo con ese ID.");
            }
        }

        // PUT: api/GrupoMaterial/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGrupo(string id, [FromBody] GrupoMaterial grupo)
        {
            if (id != grupo.mmatIdGrupoM)
                return BadRequest("El ID no coincide.");

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("UPDATE tblGrupoMaterial SET mmatDescripción = @desc WHERE mmatIdGrupoM = @id", conn);
            cmd.Parameters.AddWithValue("@id", grupo.mmatIdGrupoM);
            cmd.Parameters.AddWithValue("@desc", grupo.mmatDescripción);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0 ? NoContent() : NotFound();
        }

        // DELETE: api/GrupoMaterial/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrupo(string id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("DELETE FROM tblGrupoMaterial WHERE mmatIdGrupoM = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0 ? NoContent() : NotFound();
        }
    }
}

