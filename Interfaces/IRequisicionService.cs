using RequisicionesApi.Dtos;
using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface IRequisicionService
    {
        Task<string> CrearAsync(RequisicionDto model);
        Task<RequisicionDto?> ObtenerRequisicionAsync(string id, string soc);
        Task<List<RequisicionDto>> ObtenerTodasAsync(string soc, string idusuaro);
        Task<List<EvaluacionReq>> ObtenerEvalAsync(string soc, string requisicion, string opcion);

        Task<ProductoAdjudicadoDTO?> ObtenerAdjudicacionAsync(string reqIdClave, string proveedorId, string sociedad);

        //Task<List<ProductoAdjudicadoDTO>> ObtenerProductosAdjudicadosAsync(string reqIdClave, string proveedorId, string sociedad);
        Task<bool> ActualizarRequisicionAsync(RequisicionDto model);
        Task<bool> EliminarRequisicionAsync(string id);

        Task<IReadOnlyList<ReqCerradaResumenDto>> ListarCerradasAsync(CancellationToken ct = default);
        Task<IReadOnlyList<ReqCerradaDetalleDto>> ListarCerradaDetallesAsync(string reqIdClave, CancellationToken ct = default);

        Task<IEnumerable<RequisicionCerradaDetalleDto>> ObtenerCerradasDetallesAsync(string idSociedad,string reqId);


        Task<GuardarDetalleProvResultadoBatch> GuardarDetalleProvAsync(
            string soc,
            AutProvReqCerradaDetalleProvDtoList request,
            CancellationToken ct = default);

        Task<int> CancelarAsync(int usrIdSoc, string reqIdClave, CancellationToken ct = default);

        Task<bool> ActualizarReqProvGanAsync(string idReq, string idprov, string soc);

        /// <summary>
        /// Obtiene el timeline filtrando por sociedad y estados.
        /// Acepta lista de estados y también items con CSV (el servicio los normaliza).
        /// </summary>
        Task<IReadOnlyList<RequisicionTimelineDto>> GetTimelineAsync(
            string usrIdSoc,
            IEnumerable<string> estados,
            CancellationToken cancellationToken = default
        );

        Task<List<RequisicionDetUsuAut>> ObtenerRequisiciones(string reqIdClave);

        Task<IReadOnlyList<RequisicionPendienteDto>> GetPendientesAsync(string usuario, string usrIdSoc, string idRol, CancellationToken ct);

        Task<List<RequisicionDetalleProv>> GetRequisicionesProvAsync(string clave, int idSoc);

    }


}

