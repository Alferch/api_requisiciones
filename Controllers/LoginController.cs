using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using RequisicionesApi.Entidades;
using RequisicionesApi.Models;
using RequisicionesApi.Utilidades;
using System.Data;
//using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RequisicionesApi.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _config;

        public LoginController(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection GetConnection() =>
            new SqlConnection(_config.GetConnectionString("DefaultConnection"));



        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = new LoginResponse();
            var roles = new Dictionary<string, RolPermiso>();

            string passwordHash = PasswordHelper.HashPassword(request.Password);

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand(@"
SELECT u.usrIdClave,u.PasswordHash
      ,u.usrIdSoc, so.socNombre      ,concat(u.usrNombre,' ',u.usrApellidoP,' ',u.usrApellidoM ) nombre
      ,u.usrPuesto ,u.usrCorreo,ur.usrIdRol,r.rolnombre,ur.usrPerAlta
      ,ur.usrPercambio,ur.usrPerdel,ur.usrPerupdate,ur.usrPerVoBo,I.usrIdImp, ur.UsrPerLic, ur.UsrPerAut, ur.UsrPerReq,ur.UsrPerCompara
      ,ur.UsrPerLibera
  FROM [dbo].[tblUsuario] u
  inner join tblSociedad so on so.socidSoc = u.usrIdSoc
  inner join tblUsrRolPer ur on u.usrIdClave = ur.usrIdClave
  inner join tblRoles r on r.rolIdRol = ur.usrIdRol
  left outer JOIN   tblUsrImputacion I ON I.usrIdClave = ur.usrIdClave and I.usrIdRol = ur.usrIdRol
WHERE u.usrCorreo = @Mail AND u.PasswordHash = @Password
    ", conn)
            {
                CommandType = CommandType.Text
            };

            cmd.Parameters.AddWithValue("@Mail", request.Mail);
            cmd.Parameters.AddWithValue("@Password", passwordHash);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (string.IsNullOrEmpty(response.UsrIdClave))
                {
                    response.UsrIdClave = reader["usrIdClave"].ToString();
                    response.UsrIdSoc = reader["usrIdSoc"].ToString();
                    response.socNombre = reader["socNombre"].ToString();
                    response.Nombre = reader["nombre"].ToString();
                    response.UsrPuesto = reader["usrPuesto"].ToString();
                    response.UsrCorreo = reader["usrCorreo"].ToString();
                }

                var rolId = reader["usrIdRol"].ToString();
                if (!roles.TryGetValue(rolId, out var rol))
                {
                    rol = new RolPermiso
                    {
                        UsrIdRol = rolId,
                        RolNombre = reader["rolnombre"].ToString(),
                        Permisos = new Permisos
                        {
                            UsrPerAlta = reader["usrPerAlta"].ToString(),
                            UsrPercambio = reader["usrPercambio"].ToString(),
                            UsrPerdel = reader["usrPerdel"].ToString(),
                            UsrPerupdate = reader["usrPerupdate"].ToString(),
                            UsrPerVoBo = reader["usrPerVoBo"].ToString(),
                            UsrPerLic = reader["UsrPerLic"].ToString(),
                            UsrPerAut = reader["UsrPerAut"].ToString(),
                            UsrPerRec = reader["UsrPerReq"].ToString(),
                            UsrPerCompara = reader["UsrPerCompara"].ToString(),
                            UsrPerLibera = reader["UsrPerLibera"].ToString()
                        },
                        Imputaciones = new List<Imputacion>()
                    };
                    roles[rolId] = rol;
                }

                rol.Imputaciones.Add(new Imputacion
                {
                    Valor = reader["usrIdImp"].ToString()
                });
            }
            var token = GenerateJwtToken("1", request.Mail, "2");
            response.token = token;
            response.RolPermiso = roles.Values.ToList();

            return string.IsNullOrEmpty(response.UsrIdClave) ? Unauthorized() : Ok(response);
        }



        //[HttpPost]
        //public IActionResult Login([FromBody] LoginRequest request)
        //{
        //    using var conn = GetConnection();
        //    conn.Open();

        //     string mail = "";
        //    string passwordHash = PasswordHelper.HashPassword("12345");
        //     string rol = "Admin";
        //    string idUsuario = "";
        //    string sociedad = "";

        //    var cmd = new SqlCommand("SELECT usrIdClave, PasswordHash, usrPuesto as rol,  usridsoc FROM tblUsuario WHERE UsrCorreo = @Mail", conn);
        //    cmd.Parameters.AddWithValue("@Mail", request.Mail);

        //    using var reader = cmd.ExecuteReader();

        //    if (reader.Read())
        //    {
        //        idUsuario =  reader["usrIdClave"].ToString();
        //        passwordHash = reader["PasswordHash"].ToString();
        //        rol = reader["Rol"].ToString();
        //        sociedad = reader["usridsoc"].ToString();

        //        if (PasswordHelper.Verify(request.Password, passwordHash!))
        //        {
        //            var token = GenerateJwtToken(idUsuario, request.Mail, rol!);
        //            return Ok(new LoginResponse
        //            {
        //                IdUsuario = idUsuario,
        //                Token = token,
        //                Estatus = "OK",
        //                Rol = rol!,
        //                sociedad = sociedad
        //            });
        //        }
        //    }

        //    return Unauthorized(new { mensaje = "Credenciales inválidas" });
        //}

        private string GenerateJwtToken(String idUsuario, string mail, string rol)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, idUsuario.ToString()),
            new Claim(ClaimTypes.Email, mail),
            new Claim(ClaimTypes.Role, rol)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}
