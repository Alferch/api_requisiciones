using RequisicionesApi.Dtos;
using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface IDashboardService
    {

        Task<DashboardPivotDto> GetDashboardPivotAsync(CancellationToken ct);
        Task<IReadOnlyList<ConteoResultadoDto>> GetConteosAsync(CancellationToken ct);

        /// <summary>
        /// Obtiene el resumen paginado. Si filtroResultado no es null, aplica WHERE Resultado = @resultado
        /// </summary>
        Task<PagedResult<RequisicionResumenDto>> GetResumenAsync(
            int page, int pageSize, string? filtroResultado, CancellationToken ct);

    }
}
