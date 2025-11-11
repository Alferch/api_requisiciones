namespace RequisicionesApi.Models
{
    public class ProductoAdjudicadoDTO
    {

        public string IdProveedor { get; set; }
        public string NombreProveedor { get; set; }
        public string CorreoProveedor { get; set; }
        public string RequisicionId { get; set; }

        public string reqNotifFecUsr { get; set; }
        public List<ProductoDTO> Productos { get; set; } = new();
    }
}
