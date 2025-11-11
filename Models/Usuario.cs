namespace RequisicionesApi.Models
{
    public class Usuario
    {

        public string usrIdClave { get; set; } = string.Empty;
        public string usrIdSoc { get; set; } = string.Empty;
        public string usrApellidoP { get; set; } = string.Empty;
        public string usrApellidoM { get; set; } = string.Empty;
        public string usrCeCo { get; set; } = string.Empty;
        public string usrPuesto { get; set; } = string.Empty;
        public string usrCorreo { get; set; } = string.Empty;
        public string usrNombre { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

    }
}
