using RequisicionesApi.Utilidades;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RequisicionesApi.Dtos
{
    public class RequisicionDto
    {
        [StringLength(15)]
        public string? reqIdClave { get; set; } // No requerido en insert

        public DateTime reqFecCreacion { get; set; }
        public DateTime reqHrCreacion { get; set; }
        public DateTime? reqFecMod { get; set; }
        public DateTime? reqHrMod { get; set; }
        public DateTime? reqFecFin { get; set; }
        public DateTime? reqHrFin { get; set; }
        public DateTime? reqFecVoBo { get; set; }
        public DateTime? reqHrVoBo { get; set; }

        [Required]
        [StringLength(10)]
        public string usrIdClave { get; set; } = string.Empty;

        [Required]
        [StringLength(4)]
        public string usrIdSoc { get; set; } = string.Empty;

        public string reqIdAcc { get; set; } = string.Empty;


        [Required]
        [StringLength(100)]
        public string reqDescripcion { get; set; } = string.Empty;
        public List<DetalleRequisicionDto>? detalles { get; set; }
      //  public List<RequisicionAnexoDto> Anexos { get; set; } = new();
        //public List<RequisicionAnexoDto>? anexos { get; set; }
    }
}
