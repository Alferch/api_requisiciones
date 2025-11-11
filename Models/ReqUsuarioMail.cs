namespace RequisicionesApi.Models
{
    public class ReqUsuarioMail
    {

        public string reqIdClave { get; set; } = default!;
        public string? reqDescripcion { get; set; } = string.Empty;
        public string usrIdClave { get; set; } = string.Empty;
        public string usrNombre { get; set; } = string.Empty;
        public string usrCorreo { get; set; } = string.Empty;

    }
}
