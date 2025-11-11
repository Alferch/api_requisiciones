namespace RequisicionesApi.Dtos
{
    public class ReqCerradaResumenDto
    {
        public string reqIdClave { get; set; } = "";
        public DateTime reqFecCreacion { get; set; }
        public string usrIdSoc { get; set; } = "";
        public string socNombre { get; set; } = "";
        public string usrIdClave { get; set; } = "";
        public string NombreCompleto { get; set; } = "";
        public int TotalItems { get; set; }
        public string reqFecVigencia { get; set; }

    }
}
