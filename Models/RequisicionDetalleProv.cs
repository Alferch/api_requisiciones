namespace RequisicionesApi.Models
{
    public class RequisicionDetalleProv
    {


        public string ReqdpMatNo { get; set; }          // Número de material
        public string ReqdpMatDes { get; set; }         // Descripción del material
        public string ReqpIdClave { get; set; }         // Clave de la requisición
        public int ReqppPosNo { get; set; }             // Número de posición
        public int ReqppIdSoc { get; set; }             // Sociedad
        public string ReqppProvId { get; set; }         // ID del proveedor
        public decimal ReqppPrecUnit { get; set; }      // Precio unitario
        public string ReqppMoneda { get; set; }         // Moneda
        public string ReqppUnidadMed { get; set; }      // Unidad de medida
        public string ReqppCargoExt { get; set; }       // Cargo externo
        public DateTime ReqppFecEntrega { get; set; }   // Fecha de entrega
        public string ReqppCondPago { get; set; }       // Condición de pago
        public string ReqppVendedor { get; set; }       // Vendedor


    }
}
