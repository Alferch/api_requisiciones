namespace RequisicionesApi.Models.Autorizacion
{
 

        // Envío a autorización: tomamos los niveles directamente de tblRequisiciones.reqNiveles_Aut
        // Puedes activar ValidarContraMatriz para comparar (solo comparar) contra tblAuthMatrix.
        public record EnviarAutorizacionRequest(
            string ReqId,
            bool ValidarContraMatriz = false,
            decimal? Importe = null,
            string? Moneda = null,
            string? CentroCosto = null
        );

        public record AprobarNivelRequest(
            string ReqId,
            string UserId,
            string? Comentario = null
        );

        public record CancelarRequest(
            string ReqId,
            string UserId,
            string? Motivo = null
        );

        public record NotificacionProveedorRequest(
            string ReqId,
            string ProveedorEmail
        );

        public record EstadoFlujoResponse(
            string ReqId,
            string Sociedad,
            string CentroCosto,
            string Moneda,
            decimal Importe,
            string NivelesRequeridosCsv,
            System.Collections.Generic.IReadOnlyList<NivelEstado> Niveles,
            bool Completado
        );

        public record NivelEstado(
            string LevelCode,          // "L1".."L5"
            string Estado,             // "PENDIENTE" | "APROBADO" | "RECHAZADO"
            DateTime? FechaDecisionUtc,
            string? UsuarioDecision
        );

    public record PendienteAutorizacionDto(
    string ReqId,
    string ReqDescripcion,
    string Sociedad,
    string CentroCosto,
    string[] NivelesPendientesUsuario,  // Lx que el usuario puede decidir y están PENDIENTE
    System.DateTime? DesdeUtc,          // fecha más antigua entre los pendientes del usuario
    string SolicitanteId                // usrIdClave (quien solicitó)
);
}
