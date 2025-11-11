using RequisicionesApi.Utilidades;
using System.Text.Json.Serialization;

namespace RequisicionesApi.Dtos
{
    public class AnexoDto
    {
        // public string reqIdClave { get; set; } = string.Empty; // Id de requisición
        // public int reqidposNo { get; set; } // posición del detalle
        // public string contenidoBase64 { get; set; } = string.Empty; // archivo anexo codificado en base64

        public string? reqIdClave { get; set; } = string.Empty;

        public string?  reqAIdSoc { get; set; } = string.Empty;


        [JsonConverter(typeof(EmptyToNullIntConverter))]
        public int? reqidposNo { get; set; }
        public string contenidoBase64 { get; set; } = null!;
    }
}
