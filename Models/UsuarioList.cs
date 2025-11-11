namespace RequisicionesApi.Models
{
    public class UsuarioList
    {
        public string usrIdClave { get; set; }
        public string usrIdSoc { get; set; } = string.Empty;
        public string socNombre { get; set; } = string.Empty;
        public string usrNombre { get; set; } = string.Empty;
        public string usrCeCo { get; set; } = string.Empty;
        public string usrPuesto { get; set; } = string.Empty;
        public string usrCorreo { get; set; } = string.Empty;
        public string usrRol { get; set; } = string.Empty;
        public string usrRolDes { get; set; } = string.Empty;
    }
}
