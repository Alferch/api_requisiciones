namespace RequisicionesApi.Models.Condiciones
{
    public class CondAdicEncabezado
    {

        public required string ReqIdClave { get; set; }
    public required string IdCondicion { get; set; }
        public required string ProvIdProv { get ; set ; }
        public required string Proceso { get; set; }
    public int Posicion { get; set; }
    public decimal Importe { get; set; }

    }
}

 