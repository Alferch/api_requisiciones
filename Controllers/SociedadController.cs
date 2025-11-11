using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RequisicionesApi.Models;
using System.Data;
namespace RequisicionesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SociedadController : ControllerBase
    {
        private readonly string _connectionString;

        public SociedadController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            var result = new List<object>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT socIdSoc, socNombre FROM tblSociedad", conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    SocIdSoc = reader["socIdSoc"].ToString(),
                    SocNombre = reader["socNombre"].ToString()
                });
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById(string id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT socIdSoc, socNombre FROM tblSociedad WHERE socIdSoc = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new
                {
                    SocIdSoc = reader["socIdSoc"].ToString(),
                    SocNombre = reader["socNombre"].ToString()
                });
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] SociedadCreateDto dto)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("INSERT INTO tblSociedad (socIdSoc, socNombre) VALUES (@id, @nombre)", conn);
            cmd.Parameters.AddWithValue("@id", dto.SocIdSoc);
            cmd.Parameters.AddWithValue("@nombre", dto.SocNombre);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return CreatedAtAction(nameof(GetById), new { id = dto.SocIdSoc }, dto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string id, [FromBody] SociedadUpdateDto dto)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("UPDATE tblSociedad SET socNombre = @nombre WHERE socIdSoc = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@nombre", dto.SocNombre);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0 ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("DELETE FROM tblSociedad WHERE socIdSoc = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0 ? NoContent() : NotFound();
        }
    }
}
