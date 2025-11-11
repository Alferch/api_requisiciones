namespace RequisicionesApi.Models
{
    public class UsuarioDto
    {
        public string UsrIdClave { get; set; }
        public string UsrIdSoc { get; set; }
        public string SocNombre { get; set; } = "SOC";  // Solo para lectura
        public string UsrNombre { get; set; }
        public string UsrApellidoP { get; set; }
        public string UsrApellidoM { get; set; }
        public string UsrCorreo { get; set; }
        public string UsrPuesto { get; set; }
        public string PasswordHash { get; set; }
        public List<UsuarioRolPermisosDto> RolesPermisos { get; set; }
    }

}
