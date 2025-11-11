namespace RequisicionesApi.Dtos
{
    public class RequisicionCerradaDetalleDto
    {
        public string reqIdClave { get; set; } = "";
        public string socNombre { get; set; } = "";
        public string reqdIdClave { get; set; } = "";
        public int reqidposNo { get; set; }
        public string reqdpMatNo { get; set; } = "";
        public string reqdpMatDes { get; set; } = "";
        public int reqdCantidad { get; set; }
        public string? reqdUnidadMed { get; set; }
        public DateTime reqdFecEntrega { get; set; }
        public string? reqdCiudad { get; set; }
        public string? reqdMunicipio { get; set; }

        public string dpIdClave { get; set; } = "";
        public string prov1 { get; set; } = "";
        public string prov2 { get; set; } = "";
        public string prov3 { get; set; } = "";
        public string prov4 { get; set; } = "";
        public string prov5 { get; set; } = "";
        public string authorize { get; set; } = "";

        public string Accion { get; set; } = "";   // "editar" | "nuevo"
    }
}
