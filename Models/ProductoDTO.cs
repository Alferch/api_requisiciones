namespace RequisicionesApi.Models
{
    public class ProductoDTO
    {
        public string Posicion { get; set; }
        public string CodigoMaterial { get; set; }
        public string Descripcion { get; set; }
        public string Unidad { get; set; }
        public string Cantidad { get; set; }
        public string PrecioUnitario { get; set; }
        public string CondicionPago { get; set; }
        public string FechaEntrega { get; set; }
        public string CargoExterno { get; set; }
        public string Moneda { get; set; }
    }
}
