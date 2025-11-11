namespace RequisicionesApi.Models
{
    public class RequisicionTimelineDto
    {
        public string reqIdClave { get; set; } = default!;
        public string usrIdSoc { get; set; } = default!;
        public string? reqDescripcion { get; set; }

        public DateTime? reqFecCreacion { get; set; }
        public DateTime? reqFecFin { get; set; }
        public DateTime? reqFecVoBo { get; set; }
        public DateTime? reqNotFecProvGan { get; set; }
        public DateTime? reqFecCanc { get; set; }

        public string EstadoActual { get; set; } = default!;
        public DateTime UltimaFechaCerrada { get; set; }
        public int DiasDesdeUltimaCerrada { get; set; }

        public int? Dur_Creacion_Captura_d { get; set; }
        public int? Dur_Captura_VoBo_d { get; set; }
        public int? Dur_VoBo_Notif_d { get; set; }
    }
}
