using System.ComponentModel.DataAnnotations;

namespace RequisicionesApi.Models
{
    public class ProveedorRepository
    {

            [Key]
            [StringLength(10)]
            public string ProvId { get; set; }

        [Required]
        [StringLength(10)]
        public string ProvSocId { get; set; }

        [Required]
        [StringLength(10)]
        public string provIdGrupoM { get; set; }


            [Required]
            [StringLength(100)]
            public string ProvNombre { get; set; }

            [Required]
            [StringLength(20)]
            public string ProvRFC { get; set; }

            [Required]
            [StringLength(100)]
            public string ProvVendedor { get; set; }

            [Required]
            [StringLength(15)]
            public string ProvTelefono { get; set; }

            [Required]
            [StringLength(50)]
            public string ProvCorreo { get; set; }

            [StringLength(1)]
            public string ProvIdioma { get; set; }

            [StringLength(1)]
            public string ProvClasificacion { get; set; }
        }
    }

