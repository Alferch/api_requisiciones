using Microsoft.Data.SqlClient;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;
using System.Data;

namespace RequisicionesApi.Services
{

    public class SqlTimelineRepository : ITimelineRepository
    {
        private readonly string _connString;

        public SqlTimelineRepository(IConfiguration config)
        {
            _connString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'SqlServer' not found.");

 

        }

        public async Task<IReadOnlyList<TimelineEventDto>> GetAsync(TimelineQuery query, CancellationToken ct = default)
        {
            var where = new List<string>();
            var cmdText = @"
SELECT NumeroRequisicion, DescripcionRequisicion, Hito, Estado, FechaEvento
FROM dbo.vw_TimelineRequisiciones
WHERE 1=1
";
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(query.Numero))
            {
                if (query.UseLike)
                {
                    where.Add("NumeroRequisicion LIKE @numeroPattern");
                    parameters.Add(new SqlParameter("@numeroPattern", SqlDbType.NVarChar, 50) { Value = $"%{query.Numero}%" });
                }
                else
                {
                    where.Add("NumeroRequisicion = @numero");
                    parameters.Add(new SqlParameter("@numero", SqlDbType.NVarChar, 50) { Value = query.Numero });
                }
            }

            if (query.Desde.HasValue)
            {
                where.Add("(FechaEvento IS NULL OR FechaEvento >= @desde)");
                parameters.Add(new SqlParameter("@desde", SqlDbType.DateTime2) { Value = query.Desde.Value });
            }

            if (query.Hasta.HasValue)
            {
                where.Add("(FechaEvento IS NULL OR FechaEvento <= @hasta)");
                parameters.Add(new SqlParameter("@hasta", SqlDbType.DateTime2) { Value = query.Hasta.Value });
            }

            if (!string.IsNullOrWhiteSpace(query.Hito))
            {
                where.Add("Hito = @hito");
                parameters.Add(new SqlParameter("@hito", SqlDbType.NVarChar, 50) { Value = query.Hito });
            }

            if (!string.IsNullOrWhiteSpace(query.Estado))
            {
                where.Add("Estado = @estado");
                parameters.Add(new SqlParameter("@estado", SqlDbType.NVarChar, 100) { Value = query.Estado });
            }

            if (where.Count > 0)
                cmdText += " AND " + string.Join(" AND ", where);

            string orderBy = (string.Equals(query.Order, "fecha", StringComparison.OrdinalIgnoreCase))
                ? " ORDER BY NumeroRequisicion, FechaEvento"
                : @" ORDER BY NumeroRequisicion,
                    CASE Hito
                        WHEN 'Captura' THEN 0
                        WHEN 'FinCaptura' THEN 1
                        WHEN 'Aceptada' THEN 2
                        WHEN 'Cancelada' THEN 3
                        WHEN 'NotificadoProveedor' THEN 4
                        WHEN 'Vigencia' THEN 5
                        WHEN 'L1' THEN 101
                        WHEN 'L2' THEN 102
                        WHEN 'L3' THEN 103
                        WHEN 'L4' THEN 104
                        WHEN 'L5' THEN 105
                        ELSE 999
                    END,
                    FechaEvento";

            cmdText += orderBy;

            var list = new List<TimelineEventDto>();
            await using var conn = new SqlConnection(_connString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(cmdText, conn);
            if (parameters.Count > 0) cmd.Parameters.AddRange(parameters.ToArray());

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection, ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new TimelineEventDto
                {
                    NumeroRequisicion = reader.GetString(reader.GetOrdinal("NumeroRequisicion")),
                    DescripcionRequisicion = reader.GetString(reader.GetOrdinal("DescripcionRequisicion")),
                    Hito = reader.GetString(reader.GetOrdinal("Hito")),
                    Estado = reader.GetString(reader.GetOrdinal("Estado")),
                    FechaEvento = reader.IsDBNull(reader.GetOrdinal("FechaEvento"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("FechaEvento"))
                });
            }

            return list;
        }

        public async Task<IReadOnlyList<TimelineEventDto>> GetByNumeroAsync(string numero, CancellationToken ct = default)
        {
            const string cmdText = @"
SELECT NumeroRequisicion, DescripcionRequisicion, Hito, Estado, FechaEvento
FROM dbo.vw_TimelineRequisiciones
WHERE NumeroRequisicion = @numero
ORDER BY
    CASE Hito
        WHEN 'Captura' THEN 0
        WHEN 'FinCaptura' THEN 1
        WHEN 'Aceptada' THEN 2
        WHEN 'Cancelada' THEN 3
        WHEN 'NotificadoProveedor' THEN 4
        WHEN 'Vigencia' THEN 5
        WHEN 'L1' THEN 101
        WHEN 'L2' THEN 102
        WHEN 'L3' THEN 103
        WHEN 'L4' THEN 104
        WHEN 'L5' THEN 105
        ELSE 999
    END,
    FechaEvento;";

            var list = new List<TimelineEventDto>();
            await using var conn = new SqlConnection(_connString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(cmdText, conn);
            cmd.Parameters.Add(new SqlParameter("@numero", SqlDbType.NVarChar, 50) { Value = numero });

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection, ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new TimelineEventDto
                {
                    NumeroRequisicion = reader.GetString(reader.GetOrdinal("NumeroRequisicion")),
                    DescripcionRequisicion = reader.GetString(reader.GetOrdinal("DescripcionRequisicion")),
                    Hito = reader.GetString(reader.GetOrdinal("Hito")),
                    Estado = reader.GetString(reader.GetOrdinal("Estado")),
                    FechaEvento = reader.IsDBNull(reader.GetOrdinal("FechaEvento"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("FechaEvento"))
                });
            }

            return list;
        }
    }
}
