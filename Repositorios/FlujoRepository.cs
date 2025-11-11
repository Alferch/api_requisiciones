using Microsoft.Data.SqlClient;
using RequisicionesApi.Entidades;
using RequisicionesApi.Models;
using RequisicionesApi.Models.Autorizacion;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace RequisicionesApi.Repositorios
{
    public sealed class FlujoRepository
    {
         private readonly string _cs;
         public FlujoRepository(Microsoft.Extensions.Configuration.IConfiguration cfg)
            => _cs = cfg.GetConnectionString("DefaultConnection")!;

        //private readonly IConfiguration _cs;
       // public FlujoRepository(IConfiguration config) => _cs = config;


        // --- Lecturas base ---

        public async Task<string?> GetSociedadByReqAsync(string reqId)
        {
            const string sql = @"
SELECT TOP 1 CAST(usrIdSoc AS nvarchar(10))
FROM [dbo].[tblRequisiciones]
WHERE reqIdClave = @req";
            await using var cn = new SqlConnection(_cs);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@req", reqId);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj as string;
        }

        public async Task<List<string>> GetDistinctCCByReqAsync(string reqId)
        {
            const string sql = @"
SELECT DISTINCT CAST(reqdIdImp AS nvarchar(20)) AS CC
FROM  [dbo].[tblDetRequisiciones]
WHERE reqdIdClave = @req";
            var list = new List<string>();
            await using var cn = new SqlConnection(_cs);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@req", reqId);
            await cn.OpenAsync();
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                list.Add(rd.GetString(0));
            return list;
        }

        public async Task<string?> GetReqNivelesCsvAsync(string reqId)
        {
            const string sql = @"
SELECT reqNiveles_Aut
FROM  [dbo].[tblRequisiciones]
WHERE reqIdClave = @req";
            await using var cn = new SqlConnection(_cs);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@req", reqId);
            await cn.OpenAsync();
            var o = await cmd.ExecuteScalarAsync();
            return o as string;
        }

        // Opcional: solo para validar contra matriz (no sobreescribe)
        public async Task<string?> ResolverNivelesCsvAsync(string soc, string cc, string moneda, decimal importe)
        {
            const string sql = @"
SELECT TOP 1 autNiveles_Aut
FROM dbo.tblAuthMatrix
WHERE autIdSoc=@soc AND autdIdImp=@cc AND autMoneda=@mon
  AND @imp >= autImpMin AND @imp < autImpMax
ORDER BY autImpMin DESC";
            await using var cn = new SqlConnection(_cs);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@soc", soc);
            cmd.Parameters.AddWithValue("@cc", cc);
            cmd.Parameters.AddWithValue("@mon", moneda);
            cmd.Parameters.AddWithValue("@imp", importe);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj as string;
        }

        // --- Escrituras del flujo ---

        // Crea los pendientes (idempotente: elimina previos) y copia CSV en cabecera
        public async Task CrearPendientesAsync(string reqId, string nivelesCsv)
        {
            await using var cn = new SqlConnection(_cs);
            await cn.OpenAsync();
            await using var tx = cn.BeginTransaction();

            // Eliminar previos
            await using (var del = new SqlCommand(
                "DELETE FROM dbo.tblFlujoAut WHERE reqIdClave=@req", cn, tx))
            {
                del.Parameters.AddWithValue("@req", reqId);
                await del.ExecuteNonQueryAsync();
            }

            // Insertar nuevos PENDIENTE por cada nivel
            var niveles = nivelesCsv.Split(',')
                                    .Select(s => s.Trim())
                                    .Where(s => !string.IsNullOrWhiteSpace(s));
            await using (var ins = new SqlCommand(@"
INSERT INTO dbo.tblFlujoAut (reqIdClave, reqlevelcode, reqEstado, reqCreado)
VALUES (@req, @lvl, 'PENDIENTE', SYSUTCDATETIME())", cn, tx))
            {
                foreach (var lvl in niveles)
                {
                    ins.Parameters.Clear();
                    ins.Parameters.AddWithValue("@req", reqId);
                    ins.Parameters.AddWithValue("@lvl", lvl);
                    await ins.ExecuteNonQueryAsync();
                }
            }

            // Copia de niveles en cabecera (para trazabilidad)
            await using (var upd = new SqlCommand(@"
UPDATE  [dbo].[tblRequisiciones]
SET reqNiveles_Aut = @niv
WHERE reqIdClave = @req", cn, tx))
            {
                upd.Parameters.AddWithValue("@niv", nivelesCsv);
                upd.Parameters.AddWithValue("@req", reqId);
                await upd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        // Retorna el primer pendiente (orden lógico L1..L5)
        public async Task<string?> GetNivelActualAsync(string reqId)
        {
            const string sql = @"
SELECT TOP 1 reqlevelcode
FROM dbo.tblFlujoAut
WHERE reqIdClave=@req AND reqEstado='PENDIENTE'
ORDER BY CASE reqlevelcode
    WHEN 'L1' THEN 1 WHEN 'L2' THEN 2 WHEN 'L3' THEN 3 WHEN 'L4' THEN 4 WHEN 'L5' THEN 5 ELSE 99 END";
            await using var cn = new SqlConnection(_cs);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@req", reqId);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj as string;
        }

        // Verifica si el usuario tiene el nivel (usrNiveles_Aut contiene CSV)
        public async Task<bool> UsuarioTieneNivelAsync(string userId, string nivel)
        {
            const string sql = @"
SELECT TOP 1 1
FROM dbo.tblUsrImputacion
WHERE usrIdClave = @usuario
  AND usrIdRol = @idRol
  AND LTRIM(RTRIM(usrNivel)) = @nivel;";

             await using var conn = new SqlConnection(_cs);
            // await using var cn = new SqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.Add(new SqlParameter("@usuario", SqlDbType.NVarChar, 10) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@idRol", SqlDbType.NVarChar, 4) { Value = "R06"});
            cmd.Parameters.Add(new SqlParameter("@nivel", SqlDbType.NVarChar, 10) { Value = nivel });

            var res = await cmd.ExecuteScalarAsync();
            return res != null && res != DBNull.Value;


//            await using var rd = await cmd.ExecuteReaderAsync();
//            while (await rd.ReadAsync())
//            {


//                const string sql = @"
//SELECT TOP 1 reqNiveles_Aut
//FROM dbo.tblUsrImputacion
//WHERE  usrIdClave = @usuario = @u";
//            await using var cn = new SqlConnection(_cs);
//            await using var cmd = new SqlCommand(sql, cn);
//            cmd.Parameters.AddWithValue("@u", userId);
//            await cn.OpenAsync();
//            var obj = await cmd.ExecuteScalarAsync();
//            var csv = obj as string;
//            if (string.IsNullOrWhiteSpace(csv)) return false;
//            var set = csv.Split(',').Select(s => s.Trim())
//                         .ToHashSet(StringComparer.OrdinalIgnoreCase);
//            return set.Contains(nivel);
        }

        // Aprueba o rechaza el nivel actual
        public async Task MarcarNivelAsync(string reqId, string nivel, string userId, string estado, string? comentario)
        {
            const string sql = @"
UPDATE dbo.tblFlujoAut
SET reqEstado=@est, ReqFecAprob=SYSUTCDATETIME(), reqUsrAprobo=@usr, reqComentario=@com
WHERE reqIdClave=@req AND reqlevelcode=@lvl AND reqEstado='PENDIENTE'";
            await using var cn = new SqlConnection(_cs);

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@est", estado); // 'APROBADO' | 'RECHAZADO'
            cmd.Parameters.AddWithValue("@usr", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@com", (object?)comentario ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@req", reqId);
            cmd.Parameters.AddWithValue("@lvl", nivel);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
                throw new InvalidOperationException("Nivel no encontrado o ya decidido.");
        }

        public async Task<bool> ExistenPendientesAsync(string reqId)
        {
            const string sql = @"SELECT 1 FROM dbo.tblFlujoAut WHERE reqIdClave=@req AND reqEstado='PENDIENTE'";
            await using var cn = new SqlConnection(_cs);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@req", reqId);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj != null;
        }

        public async Task SelloVoBoFinalAsync(string reqId)
        {
            // Usamos tus campos de VoBo en cabecera
            const string sql = @"
UPDATE  [dbo].[tblRequisiciones]
SET reqFecVoBo = CONVERT(date, SYSUTCDATETIME()),
    reqHrVoBo  = CONVERT(time(0), SYSUTCDATETIME())
WHERE reqIdClave=@req";
            await using var cn = new SqlConnection(_cs);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@req", reqId);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SelloNotificadoProveedorAsync(string reqId)
        {
            const string sql = @"
UPDATE  [dbo].[tblRequisiciones]
SET reqNotFecProvGan = SYSUTCDATETIME()
WHERE reqIdClave=@req";
            await using var cn = new SqlConnection(_cs);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@req", reqId);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }


        // Listar requisiciones por autorizar para un usuario (filtra por usrCeCo y niveles del usuario)
        // Data/FlujoRepository.cs  (reemplaza TODO el método GetPendientesParaUsuarioAsync por este)
        public async Task<List<PendienteAutorizacionDto>> GetPendientesParaUsuarioAsync(string usrIdClave, string usrIdSoc)
        {
            // Un solo SQL que:
            // 1) Obtiene niveles del usuario desde tblUsuario (usrNiveles_Aut).
            // 2) Obtiene TODOS los centros de costo del usuario desde tblUsrImputacion (usrIdImp).
            // 3) Busca requisiciones con niveles PENDIENTE que coincidan con los niveles del usuario
            //    y que tengan al menos un detalle con un CC dentro del set de imputaciones del usuario.
            const string sql = @"
WITH UserData AS (
  SELECT TOP 1 
         u.usrIdClave, 
         CAST(u.usrIdSoc AS nvarchar(10)) AS usrIdSoc,
         ISNULL(u.usrNiveles_Aut, '') AS usrNiveles_Aut
  FROM  [dbo].[tblUsuario] u
  WHERE u.usrIdClave = @usr AND u.usrIdSoc = @soc
),
UserCC AS (
  SELECT DISTINCT CAST(ui.usrIdImp AS nvarchar(50)) AS usrIdImp
  FROM  [dbo].[tblUsrImputacion] ui
  WHERE ui.usrIdClave = @usr
),
UserLevels AS (
  SELECT TRIM(value) AS lvl
  FROM UserData CROSS APPLY STRING_SPLIT(UserData.usrNiveles_Aut, ',')
  WHERE TRIM(value) <> ''
),
PendUsuario AS (
  SELECT f.reqIdClave, f.reqlevelcode, MIN(f.reqCreado) AS desdeUtc
  FROM dbo.tblFlujoAut f
  JOIN [dbo].[tblRequisiciones] r
    ON r.reqIdClave = f.reqIdClave
  WHERE f.reqEstado = 'PENDIENTE'
    AND r.usrIdSoc = @soc
    AND EXISTS (
        SELECT 1
        FROM UserLevels UL
        WHERE UL.lvl = f.reqlevelcode
    )
    AND EXISTS (
        SELECT 1
        FROM  [dbo].[tblDetRequisiciones] d
        JOIN UserCC uc
          ON uc.usrIdImp = CAST(d.reqdIdImp AS nvarchar(50))
        WHERE d.reqdIdClave = r.reqIdClave
    )
  GROUP BY f.reqIdClave, f.reqlevelcode
),
Agg AS (
  SELECT 
    r.reqIdClave,
    ISNULL(r.reqDescripcion,'') AS reqDescripcion,
    CAST(r.usrIdSoc AS nvarchar(10)) AS Sociedad,
    -- Para mostrar, tomamos un CC del usuario que esté en el detalle de la req
    (SELECT TOP 1 CAST(d.reqdIdImp AS nvarchar(50))
     FROM  [dbo].[tblDetRequisiciones] d
     JOIN UserCC uc ON uc.usrIdImp = CAST(d.reqdIdImp AS nvarchar(50))
     WHERE d.reqdIdClave = r.reqIdClave) AS CentroCosto,
    r.usrIdClave AS SolicitanteId,
    STRING_AGG(p.reqlevelcode, ',') WITHIN GROUP (ORDER BY p.reqlevelcode) AS nivelesCsv,
    MIN(p.desdeUtc) AS desdeUtc
  FROM PendUsuario p
  JOIN  [dbo].[tblRequisiciones] r
    ON r.reqIdClave = p.reqIdClave
  GROUP BY r.reqIdClave, r.reqDescripcion, r.usrIdSoc, r.usrIdClave
)
SELECT reqIdClave, reqDescripcion, Sociedad, CentroCosto, nivelesCsv, desdeUtc, SolicitanteId
FROM Agg
ORDER BY desdeUtc ASC";

            var result = new List<PendienteAutorizacionDto>();

            await using var cn = new SqlConnection(_cs);
            await cn.OpenAsync();

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@usr", usrIdClave);
            cmd.Parameters.AddWithValue("@soc", usrIdSoc);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                var reqId = rd.GetString(0);
                var reqDesc = rd.GetString(1);
                var soc = rd.GetString(2);
                var cc = rd.IsDBNull(3) ? "" : rd.GetString(3);
                var nivelesMatch = (rd.IsDBNull(4) ? "" : rd.GetString(4))
                                   .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var desde = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5);
                var solicitante = rd.IsDBNull(6) ? "" : rd.GetString(6);

                result.Add(new PendienteAutorizacionDto(
                    ReqId: reqId,
                    ReqDescripcion: reqDesc,
                    Sociedad: soc,
                    CentroCosto: cc,
                    NivelesPendientesUsuario: nivelesMatch,
                    DesdeUtc: desde,
                    SolicitanteId: solicitante
                ));
            }

            return result;
        }

        // Cancelar: marca todos los PENDIENTE como RECHAZADO (comentario), sella cancelación y fin
        public async Task CancelarAsync(string reqId, string userId, string? motivo)
        {
            await using var cn = new SqlConnection(_cs);
            await cn.OpenAsync();
            await using var tx = cn.BeginTransaction();

            // 1) PENDIENTE -> RECHAZADO (Cancelado)
            await using (var updPend = new SqlCommand(@"
UPDATE dbo.tblFlujoAut
SET reqEstado='RECHAZADO',
    ReqFecAprob=SYSUTCDATETIME(),
    reqUsrAprobo=@usr,
    reqComentario=ISNULL(@motivo, N'Cancelado')
WHERE reqIdClave=@req AND reqEstado='PENDIENTE'", cn, tx))
            {
                updPend.Parameters.AddWithValue("@req", reqId);
                updPend.Parameters.AddWithValue("@usr", userId);
                updPend.Parameters.AddWithValue("@motivo", (object?)motivo ?? DBNull.Value);
                await updPend.ExecuteNonQueryAsync();
            }

            // 2) Sellos en requisición
            await using (var updReq = new SqlCommand(@"
UPDATE [dbo].[tblRequisiciones]
SET reqFecCanc = CONVERT(date, SYSUTCDATETIME()),
    reqFecFin  = COALESCE(reqFecFin, CONVERT(date, SYSUTCDATETIME())),
    reqHrFin   = COALESCE(reqHrFin,  CONVERT(time(0), SYSUTCDATETIME()))
WHERE reqIdClave=@req", cn, tx))
            {
                updReq.Parameters.AddWithValue("@req", reqId);
                await updReq.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task<bool> FueCanceladaAsync(string reqId)
        {
            const string sql = @"
SELECT reqFecCanc
FROM  [dbo].[tblRequisiciones]
WHERE reqIdClave=@req";
            await using var cn = new SqlConnection(_cs);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@req", reqId);
            await cn.OpenAsync();
            var v = await cmd.ExecuteScalarAsync();
            return v != DBNull.Value && v != null;
        }

        // Estado completo del flujo (para UI / auditoría)
        public async Task<EstadoFlujoResponse> GetEstadoAsync(string reqId, string moneda, decimal? importe)
        {
            string sociedad = "";
            string csvReq = "";

            // Sociedad + CSV de niveles en cabecera
            const string sqlReq = @"
SELECT CAST(usrIdSoc AS nvarchar(10)) AS Soc, ISNULL(reqNiveles_Aut,'') AS Csv
FROM   [dbo].[tblRequisiciones]
WHERE reqIdClave=@req";
            await using (var cn = new SqlConnection(_cs))
            {
                await cn.OpenAsync();
                await using (var cmdReq = new SqlCommand(sqlReq, cn))
                {
                    cmdReq.Parameters.AddWithValue("@req", reqId);
                    await using var rd = await cmdReq.ExecuteReaderAsync();
                    if (await rd.ReadAsync())
                    {
                        sociedad = rd.GetString(0);
                        csvReq = rd.GetString(1);
                    }
                }
            }

            // Tomamos el primer CC solo para mostrar (el envío pudo haberlo especificado)
            string cc = "";
            const string sqlCc = @"
SELECT TOP 1 CAST(reqdIdImp AS nvarchar(20))
FROM  [dbo].[tblDetRequisiciones]
WHERE reqdIdClave=@req";
            await using (var cn2 = new SqlConnection(_cs))
            {
                await cn2.OpenAsync();
                await using var cmdCc = new SqlCommand(sqlCc, cn2);
                cmdCc.Parameters.AddWithValue("@req", reqId);
                var o = await cmdCc.ExecuteScalarAsync();
                cc = o as string ?? "";
            }

            var niveles = new List<NivelEstado>();
            const string sqlNiv = @"
SELECT reqlevelcode, reqEstado, ReqFecAprob, reqUsrAprobo
FROM dbo.tblFlujoAut
WHERE reqIdClave=@req
ORDER BY CASE reqlevelcode
    WHEN 'L1' THEN 1 WHEN 'L2' THEN 2 WHEN 'L3' THEN 3 WHEN 'L4' THEN 4 WHEN 'L5' THEN 5 ELSE 99 END";
            await using (var cn3 = new SqlConnection(_cs))
            {
                await cn3.OpenAsync();
                await using var cmd = new SqlCommand(sqlNiv, cn3);
                cmd.Parameters.AddWithValue("@req", reqId);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var lvl = rd.GetString(0);
                    var est = rd.GetString(1);
                    var f = rd.IsDBNull(2) ? (DateTime?)null : rd.GetDateTime(2);
                    var u = rd.IsDBNull(3) ? null : rd.GetString(3);
                    niveles.Add(new NivelEstado(lvl, est, f, u));
                }
            }

            bool done = niveles.Count > 0 && niveles.All(n => n.Estado != "PENDIENTE");
            return new EstadoFlujoResponse(
                ReqId: reqId,
                Sociedad: sociedad,
                CentroCosto: cc,
                Moneda: moneda ?? "",
                Importe: importe ?? 0,
                NivelesRequeridosCsv: csvReq ?? "",
                Niveles: niveles,
                Completado: done
            );



        }
    }
}
