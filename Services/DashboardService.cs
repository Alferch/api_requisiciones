using Microsoft.Data.SqlClient;
using RequisicionesApi.Dtos;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Services
{

    public sealed class DashboardService : IDashboardService
    {
        private readonly string _connStr;

        public DashboardService(IConfiguration config)
        {
           // _connStr = config.GetConnectionString("SqlDb")
           //           ?? throw new InvalidOperationException("ConnectionStrings:SqlDb not configured.");

            _connStr = config.GetConnectionString("DefaultConnection")!;
        }

        public async Task<DashboardPivotDto> GetDashboardPivotAsync(CancellationToken ct)
        {
            const string sql = @"
SELECT
    [CREADAS],
    [CERRADAS X COMPRADOR],
    [PROCESO DE AUTORIZACION],
    [ESTADO FLUJO PEDIDO],
    [CANCELADAS X VIGENCIA],
    [CANCELADAS],
    [TOTAL],
    [CREADAS %],
    [CERRADAS X COMPRADOR %],
    [PROCESO DE AUTORIZACION %],
    [ESTADO FLUJO PEDIDO %],
    [CANCELADAS X VIGENCIA %],
    [CANCELADAS %],
    [TOTAL %]
FROM dbo.vwRequisicionesDashboardPivot;";

            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return new DashboardPivotDto(); // sin datos

            return new DashboardPivotDto
            {
                Creadas = reader.GetInt32(reader.GetOrdinal("CREADAS")),
                CerradasXComprador = reader.GetInt32(reader.GetOrdinal("CERRADAS X COMPRADOR")),
                ProcesoDeAutorizacion = reader.GetInt32(reader.GetOrdinal("PROCESO DE AUTORIZACION")),
                EstadoFlujoPedido = reader.GetInt32(reader.GetOrdinal("ESTADO FLUJO PEDIDO")),
                CanceladasXVigencia = reader.GetInt32(reader.GetOrdinal("CANCELADAS X VIGENCIA")),
                Canceladas = reader.GetInt32(reader.GetOrdinal("CANCELADAS")),
                Total = reader.GetInt32(reader.GetOrdinal("TOTAL")),

                CreadasPct = reader.GetDecimal(reader.GetOrdinal("CREADAS %")),
                CerradasXCompradorPct = reader.GetDecimal(reader.GetOrdinal("CERRADAS X COMPRADOR %")),
                ProcesoDeAutorizacionPct = reader.GetDecimal(reader.GetOrdinal("PROCESO DE AUTORIZACION %")),
                EstadoFlujoPedidoPct = reader.GetDecimal(reader.GetOrdinal("ESTADO FLUJO PEDIDO %")),
                CanceladasXVigenciaPct = reader.GetDecimal(reader.GetOrdinal("CANCELADAS X VIGENCIA %")),
                CanceladasPct = reader.GetDecimal(reader.GetOrdinal("CANCELADAS %")),
                TotalPct = reader.GetDecimal(reader.GetOrdinal("TOTAL %"))
            };
        }

        public async Task<IReadOnlyList<ConteoResultadoDto>> GetConteosAsync(CancellationToken ct)
        {
            const string sql = @"
SELECT Resultado, Conteo
FROM dbo.vwRequisicionesConteo
ORDER BY CASE WHEN Resultado = 'TOTAL REQUISICIONES' THEN 1 ELSE 0 END, Resultado;";

            var list = new List<ConteoResultadoDto>();

            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new ConteoResultadoDto
                {
                    Resultado = reader.GetString(reader.GetOrdinal("Resultado")),
                    Conteo = reader.GetInt32(reader.GetOrdinal("Conteo"))
                });
            }

            return list;
        }

        public async Task<PagedResult<RequisicionResumenDto>> GetResumenAsync(
            int page, int pageSize, string? filtroResultado, CancellationToken ct)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 50;

            // COUNT
            var countSql = "SELECT COUNT(*) FROM dbo.vwRequisicionesResumenClasificada";
            if (!string.IsNullOrWhiteSpace(filtroResultado))
                countSql += " WHERE Resultado = @resultado";

            // PAGE
            var dataSql = @"
SELECT *
FROM dbo.vwRequisicionesResumenClasificada
/**where**/
ORDER BY reqIdClave
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

            var where = string.Empty;
            if (!string.IsNullOrWhiteSpace(filtroResultado))
                where = "WHERE Resultado = @resultado";

            dataSql = dataSql.Replace("/**where**/", where);

            int totalRows;
            await using (var conn = new SqlConnection(_connStr))
            {
                await conn.OpenAsync(ct);
                await using var countCmd = new SqlCommand(countSql, conn);
                if (!string.IsNullOrWhiteSpace(filtroResultado))
                    countCmd.Parameters.AddWithValue("@resultado", filtroResultado);

                totalRows = (int)await countCmd.ExecuteScalarAsync(ct);
            }

            var items = new List<RequisicionResumenDto>();
            await using (var conn = new SqlConnection(_connStr))
            {
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand(dataSql, conn);
                if (!string.IsNullOrWhiteSpace(filtroResultado))
                    cmd.Parameters.AddWithValue("@resultado", filtroResultado);

                cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    items.Add(MapResumen(reader));
                }
            }

            return new PagedResult<RequisicionResumenDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRows = totalRows,
                Items = items
            };
        }

        private static RequisicionResumenDto MapResumen(SqlDataReader reader)
        {
            string GetStringOrNull(string col) =>
                reader.IsDBNull(reader.GetOrdinal(col)) ? null! : reader.GetString(reader.GetOrdinal(col));

            DateTime? GetDateOrNull(string col) =>
                reader.IsDBNull(reader.GetOrdinal(col)) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal(col));

            return new RequisicionResumenDto
            {
                reqIdClave = reader.GetString(reader.GetOrdinal("reqIdClave")),
                reqDescripcion = GetStringOrNull("reqDescripcion"),
                reqFecFin = GetDateOrNull("reqFecFin"),
                reqFecVoBo = GetDateOrNull("reqFecVoBo"),
                reqNiveles_Aut = GetStringOrNull("reqNiveles_Aut"),
                reqFecVigencia = GetDateOrNull("reqFecVigencia"),
                reqprovgan = GetStringOrNull("reqprovgan"),

                EstatusVoBo = GetStringOrNull("EstatusVoBo"),
                EstadoCabecera = GetStringOrNull("EstadoCabecera"),
                EstadoFlujoPedido = GetStringOrNull("EstadoFlujoPedido"),
                EstadoVigencia = GetStringOrNull("EstadoVigencia"),
                FlujoResumen = GetStringOrNull("FlujoResumen"),
                Resultado = reader.GetString(reader.GetOrdinal("Resultado"))
            };
        }
    }
}