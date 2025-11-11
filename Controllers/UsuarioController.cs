using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RequisicionesApi.Models;

namespace RequisicionesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly string _connectionString;

        public UsuarioController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }


        [HttpGet("ListaUsr")]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuariosList()
        {
            var usuarios = new List<UsuarioList>();
            var query = @"SELECT u.usrIdClave,u.usrIdSoc, s.socNombre, concat(usrNombre,' ' ,u.usrApellidoP,' ' ,usrApellidoM) nombre,u.usrCeCo,
                     u.usrPuesto, u.usrCorreo, rol.usrIdRol,r.rolNombre
                             FROM dbo.tblUsuario u
                             inner join tblSociedad s on s.socIdSoc = u.usrIdSoc
  inner join dbo.tblUsrRolPer rol on rol.usrIdClave = u.usrIdClave
  inner join  dbo.tblRoles r on r.rolIdRol = rol.usrIdRol";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                usuarios.Add(new UsuarioList
                {
                    usrIdClave = reader["usrIdClave"]?.ToString() ?? "",
                    usrIdSoc = reader["usrIdSoc"]?.ToString() ?? "",
                    socNombre = reader["socNombre"]?.ToString() ?? "",
                    usrNombre = reader["nombre"]?.ToString() ?? "",
                     usrPuesto = reader["usrPuesto"]?.ToString() ?? "",
                    usrCorreo = reader["usrCorreo"]?.ToString() ?? "",
                    usrRol = reader["usrIdRol"]?.ToString() ?? "",
                    usrRolDes = reader["rolNombre"]?.ToString() ?? "",
                });
            }

            return Ok(usuarios);
        }


        // GET TODOS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            var usuarios = new List<Usuario>();
            var query = @"SELECT usrIdClave, usrIdSoc, usrApellidoP, usrApellidoM, usrCeCo, usrPuesto, usrCorreo, usrNombre, PasswordHash
                          FROM dbo.tblUsuario";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                usuarios.Add(new Usuario
                {
                    usrIdClave = reader["usrIdClave"]?.ToString() ?? "",
                    usrIdSoc = reader["usrIdSoc"]?.ToString() ?? "",
                    usrApellidoP = reader["usrApellidoP"]?.ToString() ?? "",
                    usrApellidoM = reader["usrApellidoM"]?.ToString() ?? "",
                    usrCeCo = reader["usrCeCo"]?.ToString() ?? "",
                    usrPuesto = reader["usrPuesto"]?.ToString() ?? "",
                    usrCorreo = reader["usrCorreo"]?.ToString() ?? "",
                    usrNombre = reader["usrNombre"]?.ToString() ?? "",
                    PasswordHash = reader["PasswordHash"]?.ToString() ?? ""
                });
            }

            return Ok(usuarios);
        }

        // GET POR ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(string id)
        {
            var query = @"SELECT usrIdClave, usrIdSoc, usrApellidoP, usrApellidoM, usrCeCo, usrPuesto, usrCorreo, usrNombre, PasswordHash
                          FROM dbo.tblUsuario
                          WHERE usrIdClave = @id";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var usuario = new Usuario
                {
                    usrIdClave = reader["usrIdClave"].ToString() ?? "",
                    usrIdSoc = reader["usrIdSoc"].ToString() ?? "",
                    usrApellidoP = reader["usrApellidoP"].ToString() ?? "",
                    usrApellidoM = reader["usrApellidoM"].ToString() ?? "",
                    usrCeCo = reader["usrCeCo"].ToString() ?? "",
                    usrPuesto = reader["usrPuesto"].ToString() ?? "",
                    usrCorreo = reader["usrCorreo"].ToString() ?? "",
                    usrNombre = reader["usrNombre"].ToString() ?? "",
                    PasswordHash = reader["PasswordHash"].ToString() ?? ""
                };
                return Ok(usuario);
            }

            return NotFound();
        }

        // POST
        [HttpPost]
        public async Task<IActionResult> CrearUsuario([FromBody] Usuario usuario)
        {
            var query = @"INSERT INTO dbo.tblUsuario (usrIdClave, usrIdSoc, usrApellidoP, usrApellidoM, usrCeCo, usrPuesto, usrCorreo, usrNombre, PasswordHash)
                          VALUES (@usrIdClave, @usrIdSoc, @usrApellidoP, @usrApellidoM, @usrCeCo, @usrPuesto, @usrCorreo, @usrNombre, @PasswordHash)";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@usrIdClave", usuario.usrIdClave);
            command.Parameters.AddWithValue("@usrIdSoc", usuario.usrIdSoc);
            command.Parameters.AddWithValue("@usrApellidoP", usuario.usrApellidoP);
            command.Parameters.AddWithValue("@usrApellidoM", usuario.usrApellidoM);
            command.Parameters.AddWithValue("@usrCeCo", usuario.usrCeCo);
            command.Parameters.AddWithValue("@usrPuesto", usuario.usrPuesto);
            command.Parameters.AddWithValue("@usrCorreo", usuario.usrCorreo);
            command.Parameters.AddWithValue("@usrNombre", usuario.usrNombre);
            command.Parameters.AddWithValue("@PasswordHash", usuario.PasswordHash);

            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();

            return result > 0 ? Ok("Usuario creado") : StatusCode(500, "Error al crear");
        }

        // PUT
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarUsuario(string id, [FromBody] Usuario usuario)
        {
            var query = @"UPDATE dbo.tblUsuario SET 
                            usrIdSoc = @usrIdSoc,
                            usrApellidoP = @usrApellidoP,
                            usrApellidoM = @usrApellidoM,
                            usrCeCo = @usrCeCo,
                            usrPuesto = @usrPuesto,
                            usrCorreo = @usrCorreo,
                            usrNombre = @usrNombre,
                            PasswordHash = @PasswordHash
                          WHERE usrIdClave = @usrIdClave";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@usrIdClave", id);
            command.Parameters.AddWithValue("@usrIdSoc", usuario.usrIdSoc);
            command.Parameters.AddWithValue("@usrApellidoP", usuario.usrApellidoP);
            command.Parameters.AddWithValue("@usrApellidoM", usuario.usrApellidoM);
            command.Parameters.AddWithValue("@usrCeCo", usuario.usrCeCo);
            command.Parameters.AddWithValue("@usrPuesto", usuario.usrPuesto);
            command.Parameters.AddWithValue("@usrCorreo", usuario.usrCorreo);
            command.Parameters.AddWithValue("@usrNombre", usuario.usrNombre);
            command.Parameters.AddWithValue("@PasswordHash", usuario.PasswordHash);

            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();

            return result > 0 ? Ok("Usuario actualizado") : NotFound("Usuario no encontrado");
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarUsuario(string id)
        {
            var query = @"DELETE FROM dbo.tblUsuario WHERE usrIdClave = @id";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await connection.OpenAsync();
            var result = await command.ExecuteNonQueryAsync();

            return result > 0 ? Ok("Usuario eliminado") : NotFound("No encontrado");
        }
    }

}
 