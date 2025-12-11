using System.ComponentModel.DataAnnotations;

namespace RequisicionesApi.Dtos
{
    public class ExcelUploadDto
    {

        [Required]
        public IFormFile file { get; set; }

        [Required]
        [Range(1, 3,  ErrorMessage = "El parámetro 'opcion' debe ser 1 o 2.")]
        public int opcion { get; set; }

    }
}
