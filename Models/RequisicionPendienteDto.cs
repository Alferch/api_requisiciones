namespace RequisicionesApi.Models
{
    public class RequisicionPendienteDto
    {
        public string ReqIdClave { get; init; } = "";
        public string UsrIdSoc { get; init; } = "";
        public string SolicitanteId { get; init; } = "";
        public string? ReqDescripcion { get; init; }
        public string NivelPendiente { get; init; } = "";
        public DateTime DesdeUtc { get; init; }
    }
}
