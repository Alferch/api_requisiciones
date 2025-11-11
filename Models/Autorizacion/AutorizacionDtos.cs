namespace RequisicionesApi.Models.Autorizacion
{
    public class AutorizacionDtos
    {

        public record GenerarFlujoRequest(string ReqIdClave, int UsrIdSoc);
        public record AccionRequest(string ReqIdClave, string Usuario, string Accion, string? Comentario);
        public record PendienteDto(string ReqIdClave, string ReqLevelCode, DateTime ReqCreadoUtc);
        public record TimelineDto(string ReqLevelCode, string Evento, DateTime EventoEnUtc, string? EventoPor, string? Comentario);
        public record EstadoNivelDto(string ReqLevelCode, string ReqEstado, DateTime ReqCreadoUtc, DateTime? ReqDecididoEnUtc, string? ReqDecididoPor, string? Comentario);


    }
}
