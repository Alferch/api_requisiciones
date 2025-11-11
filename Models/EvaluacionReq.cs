namespace RequisicionesApi.Models
{
    public class EvaluacionReq
    {
 
            public string ReqIdClave { get; set; }
            public int UsrIdSoc { get; set; }
            //public string SocNombre { get; set; }
            //public int ReqIdPosNo { get; set; }
        public int ReqppProvId { get; set; }
        public string ReqppProvNombre { get; set; }
        public string reqNotifFecUsr { get; set; }

        public double Calidad { get; set; }
            public double PrecioMenor { get; set; }
            public double Entrega { get; set; }
            public double CostoAdicional { get; set; }
            public double CPago { get; set; }
            public double Atencion { get; set; }
            public double Historial { get; set; }

            public double ScoreTotal { get; set; }
 

    }
}
