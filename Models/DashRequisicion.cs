namespace RequisicionesApi.Models
{
    public class DashRequisicion
    {

        public string ReqIdClave { get; set; }
        public string ReqDescripcion { get; set; }
        public DateTime ReqFecCreacion { get; set; }
        public DateTime ReqFecVigencia { get; set; }
        public string UsrIdClave { get; set; }

    }
}
