using RequisicionesApi.Utilidades;
using System.Text.Json.Serialization;



namespace RequisicionesApi.Dtos
{
    public class DetalleRequisicionDto
    {
        public string? reqIdClave { get; set; }
        public string? reqdIdSoc { get; set; }

        [JsonConverter(typeof(EmptyToNullIntConverter))]
        public int? reqidposNo { get; set; }
        public string reqdpMatNo { get; set; } = string.Empty;
        public string reqdpMatDes { get; set; } = string.Empty;
        public int reqdCantidad { get; set; }
        public string? reqdUnidadMed { get; set; }
        public string? reqdEspecAnexos { get; set; }
        public DateTime reqdFecEntrega { get; set; }
        public string? reqdCiudad { get; set; }
        public string? reqdMunicipio { get; set; }
        public string? reqdCuenta { get; set; }
        public string? reqdIdImp { get; set; }
        public string? reqdIdAreaC { get; set; }
        public string? reqdProvId { get; set; }
        public AnexoDto? Anexo { get; set; }
        // public AnexoDto? anexo { get; set; }

        /*
        public string reqIdClave { get; set; }

        [JsonConverter(typeof(EmptyToNullIntConverter))]
        public int? reqidposNo { get; set; }
        public string reqdpMatNo { get; set; }
        public string reqdpMatDes { get; set; }
        public int reqdCantidad { get; set; }
        public string? reqdUnidadMed { get; set; }
        public string? reqdEspecAnexos { get; set; }
        public DateTime reqdFecEntrega { get; set; }
        public string? reqdCiudad { get; set; }
        public string? reqdMunicipio { get; set; }
        public string? reqdCuenta { get; set; }
        public string? reqdIdImp { get; set; }
        public string? reqdIdAreaC { get; set; }
        public string? reqdProvId { get; set; }
        public AnexoDto? anexo { get; set; }
        */
    }
}
