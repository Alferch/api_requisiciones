namespace RequisicionesApi.Dtos
{
    public class ReqCerradaDetalleDto
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

        // Placeholders fijos como en tu SELECT
        public string prov1 { get; set; } = "000000";
        public string prov2 { get; set; } = "000000";
        public string prov3 { get; set; } = "000000";
        public string prov4 { get; set; } = "000000";
        public string prov5 { get; set; } = "000000";
    }
}
