namespace RequisicionesApi.Models
{
    public class LoginResponse
    {
        public string UsrIdClave { get; set; }
        public string UsrIdSoc { get; set; }
        public string socNombre { get; set; }
        public string Nombre { get; set; }
        public string UsrPuesto { get; set; }
        public string UsrCorreo { get; set; }
        public string token { get; set; }
        public List<RolPermiso> RolPermiso { get; set; }
    }
}
