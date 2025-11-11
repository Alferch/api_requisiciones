
using MailKit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Ocsp;
using RequisicionesApi.Controllers;
using RequisicionesApi.Dtos;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;
using RequisicionesApi.Utilidades;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace RequisicionesApi.Services
{


    public class RequisicionService : IRequisicionService
    {
        private readonly string _connectionString;
        private readonly Interfaces.IMailService _mailService;
        private readonly ILogger<AdjudicacionController> _logger;

        public RequisicionService(IConfiguration configuration, Interfaces.IMailService mailService, ILogger<RequisicionService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
            //_logger = (ILogger<AdjudicacionController>?)(logger ?? throw new ArgumentNullException(nameof(logger)));

        }





        public async Task<List<RequisicionDetalleProv>> GetRequisicionesProvAsync(string clave, int idSoc)
        {
            var lista = new List<RequisicionDetalleProv>();

            string query = @"
            SELECT
                rd.reqdpMatNo,
                rd.reqdpMatDes,
                rp.reqpIdClave,
                rp.reqppPosNo,
                rp.reqppIdSoc,
                rp.reqppProvId,
                rp.reqppPrecUnit,
                rp.reqppMoneda,
                rp.reqppUnidadMed,
                rp.reqppCargoExt,
                rp.reqppFecEntrega,
                rp.reqppCondPago,
                rp.reqppVendedor
            FROM [dbo].[tblprovRequisiciones] rp
            LEFT OUTER JOIN [dbo].[tblDetRequisiciones] rd 
                ON rd.reqdIdClave = rp.reqpIdClave 
                AND rd.reqdIdSoc = rp.reqppIdSoc 
                AND rd.reqidposNo = rp.reqppPosNo
			inner join [dbo].[tblRequisiciones] r on r.reqIdClave = rd.reqdIdClave AND r.usrIdSoc = rd.reqdIdSoc
            WHERE rp.reqpIdClave = @Clave AND rp.reqppIdSoc = @IdSoc and rp.reqppProvId = r.reqProvGan";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Clave", clave);
                cmd.Parameters.AddWithValue("@IdSoc", idSoc);

                await conn.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var item = new RequisicionDetalleProv
                        {
                            ReqdpMatNo = reader["reqdpMatNo"] as string,
                            ReqdpMatDes = reader["reqdpMatDes"] as string,
                            ReqpIdClave = reader["reqpIdClave"] as string,
                            ReqppPosNo = reader["reqppPosNo"] != DBNull.Value ? Convert.ToInt32(reader["reqppPosNo"]) : 0,
                            ReqppIdSoc = reader["reqppIdSoc"] != DBNull.Value ? Convert.ToInt32(reader["reqppIdSoc"]) : 0,
                            ReqppProvId = reader["reqppProvId"] as string,
                            ReqppPrecUnit = reader["reqppPrecUnit"] != DBNull.Value ? Convert.ToDecimal(reader["reqppPrecUnit"]) : 0,
                            ReqppMoneda = reader["reqppMoneda"] as string,
                            ReqppUnidadMed = reader["reqppUnidadMed"] as string,
                            ReqppCargoExt = reader["reqppCargoExt"] as string,
                            ReqppFecEntrega = reader["reqppFecEntrega"] != DBNull.Value ? Convert.ToDateTime(reader["reqppFecEntrega"]) : DateTime.MinValue,
                            ReqppCondPago = reader["reqppCondPago"] as string,
                            ReqppVendedor = reader["reqppVendedor"] as string
                        };
                        lista.Add(item);
                    }
                }
            }

            return lista;
        }





        // =======================
        // 1) Resumen de cerradas
        // =======================
        public async Task<IReadOnlyList<ReqCerradaResumenDto>> ListarCerradasAsync(CancellationToken ct = default)
        {
            const string sql = @"
SELECT 
    r.[reqIdClave],
    r.[reqFecCreacion],
    r.[usrIdSoc],
    s.[socNombre],
    r.[usrIdClave],
    u.[usrNombre] + ' ' + u.[usrApellidoP] + COALESCE(' ' + u.[usrApellidoM], '') AS NombreCompleto,
    COUNT(d.[reqdIdClave]) AS TotalItems, CONVERT(VARCHAR(10),  r.[reqFecVigencia], 23) AS     reqFecVigencia 
FROM [dbo].[tblRequisiciones] r
INNER JOIN [dbo].[tblUsuario] u 
    ON u.[usrIdClave] = r.[usrIdClave]
INNER JOIN [dbo].[tblSociedad] s 
    ON s.[socIdSoc] = u.[usrIdSoc]
LEFT JOIN [dbo].[tblDetRequisiciones] d
    ON d.[reqdIdClave] = r.[reqIdClave]
   AND d.[reqdIdSoc]   = r.[usrIdSoc]
WHERE r.[reqFecFin] IS NOT NULL and reqFecVoBo is   null
GROUP BY 
    r.[reqIdClave],
    r.[reqFecCreacion],
    r.[usrIdSoc],
    s.[socNombre],
    r.[usrIdClave],
    u.[usrNombre],
    u.[usrApellidoP],
    u.[usrApellidoM], r.[reqFecVigencia];";

            var lista = new List<ReqCerradaResumenDto>();





            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();




            await using var cmd = new SqlCommand(sql, conn)
            {
                CommandType = CommandType.Text
            };

            await using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                lista.Add(new ReqCerradaResumenDto
                {
                    reqIdClave = rd.GetString(rd.GetOrdinal("reqIdClave")),
                    reqFecCreacion = rd.GetDateTime(rd.GetOrdinal("reqFecCreacion")),
                    usrIdSoc = rd.GetString(rd.GetOrdinal("usrIdSoc")),
                    socNombre = rd.GetString(rd.GetOrdinal("socNombre")),
                    usrIdClave = rd.GetString(rd.GetOrdinal("usrIdClave")),
                    NombreCompleto = rd.GetString(rd.GetOrdinal("NombreCompleto")),
                    TotalItems = rd.GetInt32(rd.GetOrdinal("TotalItems")),
                    reqFecVigencia = rd.GetString(rd.GetOrdinal("reqFecVigencia")),

                });
            }

            return lista;
        }

        // ===========================
        // 2) Detalles por requisición
        // ===========================
        public async Task<IReadOnlyList<ReqCerradaDetalleDto>> ListarCerradaDetallesAsync(string reqIdClave, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(reqIdClave))
                return Array.Empty<ReqCerradaDetalleDto>();

            const string sql = @"
SELECT 
    r.[reqIdClave], s.[socNombre],
    d.[reqdIdClave], d.[reqidposNo],
    d.[reqdpMatNo], d.[reqdpMatDes],
    d.[reqdCantidad], d.[reqdUnidadMed],
    d.[reqdFecEntrega],   d.[reqdCiudad],
    d.[reqdMunicipio],
    COALESCE(NULLIF(LTRIM(RTRIM(d.[reqdProvId])), ''), '00000') AS prov1,
    '000000' AS prov2,
    '000000' AS prov3,
    '000000' AS prov4,
    '000000' AS prov5
FROM [dbo].[tblRequisiciones] r
INNER JOIN [dbo].[tblUsuario] u 
    ON u.[usrIdClave] = r.[usrIdClave]
INNER JOIN [dbo].[tblSociedad] s 
    ON s.[socIdSoc] = u.[usrIdSoc]
LEFT JOIN [dbo].[tblDetRequisiciones] d
    ON d.[reqdIdClave] = r.[reqIdClave]
   AND d.[reqdIdSoc]   = r.[usrIdSoc]
   left outer  join [dbo].[tblProveedores] p on p.provIdProv = d.reqdProvId
WHERE r.[reqFecFin] IS NOT NULL
  AND r.[reqIdClave] = @reqIdClave;";

            var lista = new List<ReqCerradaDetalleDto>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();


            await using var cmd = new SqlCommand(sql, conn)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.Add(new SqlParameter("@reqIdClave", SqlDbType.NVarChar, 15) { Value = reqIdClave });

            await using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                lista.Add(new ReqCerradaDetalleDto
                {
                    reqIdClave = rd.GetString(rd.GetOrdinal("reqIdClave")),
                    socNombre = rd.GetString(rd.GetOrdinal("socNombre")),
                    reqdIdClave = rd.IsDBNull(rd.GetOrdinal("reqdIdClave")) ? "" : rd.GetString(rd.GetOrdinal("reqdIdClave")),
                    reqidposNo = rd.IsDBNull(rd.GetOrdinal("reqidposNo")) ? 0 : rd.GetInt32(rd.GetOrdinal("reqidposNo")),
                    reqdpMatNo = rd.IsDBNull(rd.GetOrdinal("reqdpMatNo")) ? "" : rd.GetString(rd.GetOrdinal("reqdpMatNo")),
                    reqdpMatDes = rd.IsDBNull(rd.GetOrdinal("reqdpMatDes")) ? "" : rd.GetString(rd.GetOrdinal("reqdpMatDes")),
                    reqdCantidad = rd.IsDBNull(rd.GetOrdinal("reqdCantidad")) ? 0 : rd.GetInt32(rd.GetOrdinal("reqdCantidad")),
                    reqdUnidadMed = rd.IsDBNull(rd.GetOrdinal("reqdUnidadMed")) ? null : rd.GetString(rd.GetOrdinal("reqdUnidadMed")),
                    reqdFecEntrega = rd.IsDBNull(rd.GetOrdinal("reqdFecEntrega")) ? DateTime.MinValue : rd.GetDateTime(rd.GetOrdinal("reqdFecEntrega")),
                    reqdCiudad = rd.IsDBNull(rd.GetOrdinal("reqdCiudad")) ? null : rd.GetString(rd.GetOrdinal("reqdCiudad")),
                    reqdMunicipio = rd.IsDBNull(rd.GetOrdinal("reqdMunicipio")) ? null : rd.GetString(rd.GetOrdinal("reqdMunicipio")),
                    // prov1..prov5 vienen fijos del SELECT
                    prov1 = rd.GetString(rd.GetOrdinal("prov1")),
                    prov2 = "000000",
                    prov3 = "000000",
                    prov4 = "000000",
                    prov5 = "000000"
                });
            }

            return lista;
        }



        public async Task<IEnumerable<RequisicionCerradaDetalleDto>> ObtenerCerradasDetallesAsync(string idSociedad, string reqId)
        {
            const string sql = @"
 ;WITH Base AS (
    SELECT
        r.reqIdClave,
        s.socNombre,
        d.reqdIdClave,
        d.reqdIdSoc,
        d.reqidposNo,
        d.reqdpMatNo,
        d.reqdpMatDes,
        d.reqdCantidad,
        d.reqdUnidadMed,
        d.reqdFecEntrega,
        d.reqdCiudad,
        d.reqdMunicipio,
        d.reqdAuth,
        d.reqdProvId   -- para el caso ""nuevo""
    FROM dbo.tblRequisiciones r
    INNER JOIN dbo.tblUsuario u
        ON u.usrIdClave = r.usrIdClave
    INNER JOIN dbo.tblSociedad s
        ON s.socIdSoc = u.usrIdSoc
    LEFT JOIN dbo.tblDetRequisiciones d
        ON d.reqdIdClave = r.reqIdClave
       AND d.reqdIdSoc   = r.usrIdSoc
    WHERE r.reqFecFin IS NOT NULL
      AND r.reqIdClave = @reqId 
      AND r.usrIdSoc   = @idSociedad
)
SELECT
    b.reqIdClave,
    b.socNombre,
    b.reqdIdClave,
    b.reqidposNo,
    b.reqdpMatNo,
    b.reqdpMatDes,
    b.reqdCantidad,
    b.reqdUnidadMed,
    b.reqdFecEntrega,
    b.reqdCiudad,
    b.reqdMunicipio,

    COALESCE(NULLIF(LTRIM(RTRIM(b.reqdIdClave)), ''), 'X') AS dpIdClave,

    -- Si hay al menos un proveedor en DetRequisicionesProv => usar pivote;
    -- si NO hay => usar d.reqdProvId saneado como prov1 (y 000000 en prov2..prov5)
    CASE
        WHEN dpa.hasProv > 0 THEN COALESCE(dpa.prov1, '000000')
        ELSE
            CASE
                WHEN b.reqdProvId IS NULL OR LTRIM(RTRIM(b.reqdProvId)) = ''
                     OR REPLACE(LTRIM(RTRIM(b.reqdProvId)), '0', '') = ''
                THEN '000000'
                ELSE LTRIM(RTRIM(b.reqdProvId))
            END
    END AS prov1,
    CASE WHEN dpa.hasProv > 0 THEN COALESCE(dpa.prov2, '000000') ELSE '000000' END AS prov2,
    CASE WHEN dpa.hasProv > 0 THEN COALESCE(dpa.prov3, '000000') ELSE '000000' END AS prov3,
    CASE WHEN dpa.hasProv > 0 THEN COALESCE(dpa.prov4, '000000') ELSE '000000' END AS prov4,
    CASE WHEN dpa.hasProv > 0 THEN COALESCE(dpa.prov5, '000000') ELSE '000000' END AS prov5,

    CASE WHEN dpa.hasProv > 0 THEN 'editar' ELSE 'nuevo' END AS accion,
    CONVERT(varchar(1), COALESCE(b.reqdAuth, 0)) AS authorize
FROM Base AS b
OUTER APPLY (
    SELECT
        MAX(CASE WHEN z.rn = 1 THEN z.reqdProvId END) AS prov1,
        MAX(CASE WHEN z.rn = 2 THEN z.reqdProvId END) AS prov2,
        MAX(CASE WHEN z.rn = 3 THEN z.reqdProvId END) AS prov3,
        MAX(CASE WHEN z.rn = 4 THEN z.reqdProvId END) AS prov4,
        MAX(CASE WHEN z.rn = 5 THEN z.reqdProvId END) AS prov5,
        COUNT(*) AS hasProv
    FROM (
        SELECT
            dp.reqdProvId,
            ROW_NUMBER() OVER (
                PARTITION BY dp.reqdIdClave, dp.reqdIdSoc, dp.reqidposNo
                ORDER BY dp.reqdConsec
            ) AS rn
        FROM dbo.tblDetRequisicionesProv AS dp
        WHERE dp.reqdIdClave = b.reqdIdClave
          AND dp.reqdIdSoc   = b.reqdIdSoc
          AND dp.reqidposNo  = b.reqidposNo
          AND NULLIF(LTRIM(RTRIM(dp.reqdProvId)), '') IS NOT NULL
          AND REPLACE(LTRIM(RTRIM(dp.reqdProvId)), '0', '') <> ''  -- descarta ""000000...""
    ) AS z
) AS dpa
ORDER BY b.reqdIdClave, b.reqidposNo;";

            var lista = new List<RequisicionCerradaDetalleDto>();


            using var conn = new SqlConnection(_connectionString);

            try
            {
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(new SqlParameter("@idSociedad", SqlDbType.NVarChar, 15) { Value = idSociedad });
                cmd.Parameters.Add(new SqlParameter("@reqId", SqlDbType.NVarChar, 15) { Value = reqId });

                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var dto = new RequisicionCerradaDetalleDto
                    {
                        reqIdClave = rd["reqIdClave"] as string ?? "",
                        socNombre = rd["socNombre"] as string ?? "",
                        reqdIdClave = rd["reqdIdClave"] as string ?? "",
                        reqidposNo = rd["reqidposNo"] as int? ?? Convert.ToInt32(rd["reqidposNo"]),
                        reqdpMatNo = rd["reqdpMatNo"] as string ?? "",
                        reqdpMatDes = (rd["reqdpMatDes"] as string ?? "").Replace("\r", "").Trim(),
                        reqdCantidad = rd["reqdCantidad"] as int? ?? Convert.ToInt32(rd["reqdCantidad"]),

                        // ⬇️ aquí estaba el problema: usar 'as string' (sin '?')
                        reqdUnidadMed = rd["reqdUnidadMed"] as string,
                        reqdCiudad = rd["reqdCiudad"] as string,
                        reqdMunicipio = rd["reqdMunicipio"] as string,

                        dpIdClave = rd["dpIdClave"] as string ?? "X",
                        prov1 = rd["prov1"] as string ?? "000000",
                        prov2 = rd["prov2"] as string ?? "000000",
                        prov3 = rd["prov3"] as string ?? "000000",
                        prov4 = rd["prov4"] as string ?? "000000",
                        prov5 = rd["prov5"] as string ?? "000000",

                        Accion = rd["Accion"] as string ?? "nuevo",
                        authorize = rd["authorize"]?.ToString() ?? "0",
                    };

                    lista.Add(dto);

                }
                return lista;
            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                if (conn?.State == ConnectionState.Open)
                {
                    await conn.CloseAsync();
                    //   _logger.LogInformation("Conexión SQL cerrada correctamente.");
                }
            }



        }


        public async Task<ProductoAdjudicadoDTO?> ObtenerAdjudicacionAsync(string reqIdClave, string proveedorId, string sociedad)
        {
            using var connection = new SqlConnection(_connectionString);


            var query = @"
        SELECT pro.provIdProv, pro.provNombre, pro.provCorreo, r.reqIdClave,
               dr.reqidposNo, dr.reqdpMatNo, dr.reqdpMatDes, dr.reqdUnidadMed, dr.reqdCantidad,
               rp.reqppPrecUnit, rp.reqppCondPago, rp.reqppFecEntrega, rp.reqppCargoExt, rp.reqppMoneda, r.reqNotifFecUsr
        FROM tblRequisiciones r
        INNER JOIN tblDetRequisiciones dr ON dr.reqdIdClave = r.reqIdClave AND dr.reqdIdSoc = r.usrIdSoc
        INNER JOIN tblprovRequisiciones rp ON rp.reqpIdClave = dr.reqdIdClave AND rp.reqppIdSoc = dr.reqdIdSoc
                                           AND dr.reqidposNo = rp.reqppPosNo
        INNER JOIN tblProveedores pro ON pro.provIdProv = rp.reqppProvId
        WHERE r.reqIdClave = @ReqIdClave AND r.usrIdSoc = @sociedad AND rp.reqppProvId = RIGHT(REPLICATE('0', 10) + CAST(@ProveedorId AS NVARCHAR(50)), 10)";


            //SqlConnection connection = null;
            ProductoAdjudicadoDTO? adjudicacion = null;

            try
            {
                //connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ReqIdClave", reqIdClave);
                command.Parameters.AddWithValue("@ProveedorId", proveedorId);
                command.Parameters.AddWithValue("@sociedad", sociedad);


                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (adjudicacion == null)
                    {
                        adjudicacion = new ProductoAdjudicadoDTO
                        {
                            IdProveedor = reader["provIdProv"]?.ToString()?.Trim(),
                            NombreProveedor = reader["provNombre"]?.ToString()?.Trim(),
                            CorreoProveedor = reader["provCorreo"]?.ToString()?.Trim(),
                            RequisicionId = reader["reqIdClave"]?.ToString()?.Trim(),
                            reqNotifFecUsr = reader["reqNotifFecUsr"]?.ToString()?.Trim()
                        };
                    }

                    adjudicacion.Productos.Add(new ProductoDTO
                    {
                        Posicion = reader["reqidposNo"]?.ToString()?.Trim(),
                        CodigoMaterial = reader["reqdpMatNo"]?.ToString()?.Trim(),
                        Descripcion = reader["reqdpMatDes"]?.ToString()?.Trim(),
                        Unidad = reader["reqdUnidadMed"]?.ToString()?.Trim(),
                        Cantidad = reader["reqdCantidad"]?.ToString()?.Trim(),
                        PrecioUnitario = reader["reqppPrecUnit"]?.ToString()?.Trim(),
                        CondicionPago = reader["reqppCondPago"]?.ToString()?.Trim(),
                        FechaEntrega = reader["reqppFecEntrega"] is DateTime f ? f.ToString("dd/MM/yyyy") : reader["reqppFecEntrega"]?.ToString()?.Trim(),
                        CargoExterno = reader["reqppCargoExt"]?.ToString()?.Trim(),
                        Moneda = reader["reqppMoneda"]?.ToString()?.Trim()
                    });
                }

                // _logger.LogInformation("Adjudicación obtenida para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", reqIdClave, proveedorId);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error al obtener adjudicación para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", reqIdClave, proveedorId);
                throw;
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                    //   _logger.LogInformation("Conexión SQL cerrada correctamente.");
                }
            }

            return adjudicacion;
        }




        //public async Task<List<ProductoAdjudicadoDTO>>  ObtenerProductosAdjudicadosAsync(string reqIdClave, string proveedorId, string sociedad)
        //{
        //    using var connection = new SqlConnection(_connectionString);
        //    var productos = new List<ProductoAdjudicadoDTO>();

        //    {
        //        var query = @"
        //SELECT pro.provIdProv, pro.provNombre, pro.provCorreo, r.reqIdClave,
        //       dr.reqidposNo, dr.reqdpMatNo, dr.reqdpMatDes, dr.reqdUnidadMed, dr.reqdCantidad,
        //       rp.reqppPrecUnit, rp.reqppCondPago, rp.reqppFecEntrega, rp.reqppCargoExt, rp.reqppMoneda
        //FROM tblRequisiciones r
        //INNER JOIN tblDetRequisiciones dr ON dr.reqdIdClave = r.reqIdClave AND dr.reqdIdSoc = r.usrIdSoc
        //INNER JOIN tblprovRequisiciones rp ON rp.reqpIdClave = dr.reqdIdClave AND rp.reqppIdSoc = dr.reqdIdSoc
        //                                   AND dr.reqidposNo = rp.reqppPosNo
        //INNER JOIN tblProveedores pro ON pro.provIdProv = rp.reqppProvId
        //WHERE r.reqIdClave = @ReqIdClave AND r.usrIdSoc = 1 AND rp.reqppProvId = @ProveedorId";

        //        SqlConnection connection = null;
        //        AdjudicacionProveedorDTO? adjudicacion = null;

        //        try
        //        {
        //            connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        //            await connection.OpenAsync();

        //            using var command = new SqlCommand(query, connection);
        //            command.Parameters.AddWithValue("@ReqIdClave", reqIdClave);
        //            command.Parameters.AddWithValue("@ProveedorId", proveedorId);

        //            using var reader = await command.ExecuteReaderAsync();

        //            while (await reader.ReadAsync())
        //            {
        //                if (adjudicacion == null)
        //                {
        //                    adjudicacion = new AdjudicacionProveedorDTO
        //                    {
        //                        IdProveedor = reader["provIdProv"]?.ToString()?.Trim(),
        //                        NombreProveedor = reader["provNombre"]?.ToString()?.Trim(),
        //                        CorreoProveedor = reader["provCorreo"]?.ToString()?.Trim(),
        //                        RequisicionId = reader["reqIdClave"]?.ToString()?.Trim()
        //                    };
        //                }

        //                adjudicacion.Productos.Add(new ProductoDTO
        //                {
        //                    Posicion = reader["reqidposNo"]?.ToString()?.Trim(),
        //                    CodigoMaterial = reader["reqdpMatNo"]?.ToString()?.Trim(),
        //                    Descripcion = reader["reqdpMatDes"]?.ToString()?.Trim(),
        //                    Unidad = reader["reqdUnidadMed"]?.ToString()?.Trim(),
        //                    Cantidad = reader["reqdCantidad"]?.ToString()?.Trim(),
        //                    PrecioUnitario = reader["reqppPrecUnit"]?.ToString()?.Trim(),
        //                    CondicionPago = reader["reqppCondPago"]?.ToString()?.Trim(),
        //                    FechaEntrega = reader["reqppFecEntrega"] is DateTime f ? f.ToString("dd/MM/yyyy") : reader["reqppFecEntrega"]?.ToString()?.Trim(),
        //                    CargoExterno = reader["reqppCargoExt"]?.ToString()?.Trim(),
        //                    Moneda = reader["reqppMoneda"]?.ToString()?.Trim()
        //                });
        //            }

        //            _logger.LogInformation("Adjudicación obtenida para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", reqIdClave, proveedorId);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error al obtener adjudicación para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", reqIdClave, proveedorId);
        //            throw;
        //        }
        //        finally
        //        {
        //            if (connection?.State == ConnectionState.Open)
        //            {
        //                await connection.CloseAsync();
        //                _logger.LogInformation("Conexión SQL cerrada correctamente.");
        //            }
        //        }

        //        return adjudicacion;






        //        var query = @"
        //    SELECT dr.reqidposNo, dr.reqdpMatNo, dr.reqdpMatDes, dr.reqdUnidadMed, dr.reqdCantidad,
        //           rp.reqppPrecUnit, rp.reqppCondPago, rp.reqppFecEntrega, rp.reqppCargoExt, rp.reqppMoneda
        //    FROM tblRequisiciones r
        //    INNER JOIN tblDetRequisiciones dr ON dr.reqdIdClave = r.reqIdClave AND dr.reqdIdSoc = r.usrIdSoc
        //    INNER JOIN tblprovRequisiciones rp ON rp.reqpIdClave = dr.reqdIdClave AND rp.reqppIdSoc = dr.reqdIdSoc
        //                                       AND dr.reqidposNo = rp.reqppPosNo
        //    WHERE r.reqIdClave = @ReqIdClave AND r.usrIdSoc = @sociedad AND rp.reqppProvId = @ProveedorId";



        //    try
        //    {

        //        await connection.OpenAsync();

        //        using var command = new SqlCommand(query, connection);
        //        command.Parameters.AddWithValue("@ReqIdClave", reqIdClave);
        //        command.Parameters.AddWithValue("@ProveedorId", proveedorId);
        //        command.Parameters.AddWithValue("@sociedad", sociedad);

        //        using var reader = await command.ExecuteReaderAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            var producto = new ProductoAdjudicadoDTO
        //            {


        //                  Posicion = reader["reqidposNo"]?.ToString()?.Trim(),
        //                CodigoMaterial = reader["reqdpMatNo"]?.ToString()?.Trim(),
        //                Descripcion = reader["reqdpMatDes"]?.ToString()?.Trim(),
        //                Unidad = reader["reqdUnidadMed"]?.ToString()?.Trim(),
        //                Cantidad = reader["reqdCantidad"]?.ToString()?.Trim(),
        //                PrecioUnitario = reader["reqppPrecUnit"]?.ToString()?.Trim(),
        //                CondicionPago = reader["reqppCondPago"]?.ToString()?.Trim(),
        //                FechaEntrega = reader["reqppFecEntrega"] is DateTime fecha ? fecha.ToString("dd/MM/yyyy") : reader["reqppFecEntrega"]?.ToString()?.Trim(),
        //                CargoExterno = reader["reqppCargoExt"]?.ToString()?.Trim(),
        //                Moneda = reader["reqppMoneda"]?.ToString()?.Trim()

        //            };

        //            productos.Add(producto);
        //        }





        ////_logger.LogInformation("Consulta ejecutada correctamente para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", reqIdClave, proveedorId);
        //    }
        //    catch (Exception ex)
        //    {
        //  //      _logger.LogError(ex, "Error al obtener productos adjudicados para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", reqIdClave, proveedorId);
        //        throw;
        //    }
        //    finally
        //    {
        //        if (connection?.State == ConnectionState.Open)
        //        {
        //            await connection.CloseAsync();
        //    //        _logger.LogInformation("Conexión SQL cerrada correctamente.");
        //        }
        //    }

        //    return productos;


        //    //using var connection = new SqlConnection(_connectionString);
        //    //using var command = new SqlCommand(query, connection);
        //    //command.Parameters.AddWithValue("@ReqIdClave", reqIdClave);
        //    //command.Parameters.AddWithValue("@ProveedorId", proveedorId);
        //    //command.Parameters.AddWithValue("@sociedad", sociedad);

        //    //await connection.OpenAsync();
        //    //using var reader = await command.ExecuteReaderAsync();

        //    //while (await reader.ReadAsync())
        //    //{
        //    //    productos.Add(new ProductoAdjudicadoDTO
        //    //    {
        //    //        Posicion = reader.GetInt32(0),
        //    //        CodigoMaterial = reader.GetString(1),
        //    //        Descripcion = reader.GetString(2),
        //    //        Unidad = reader.GetString(3),
        //    //        Cantidad = reader.GetDecimal(4),
        //    //        PrecioUnitario = reader.GetDecimal(5),
        //    //        CondicionPago = reader.GetString(6),
        //    //        FechaEntrega = reader.GetDateTime(7),
        //    //        CargoExterno = reader.GetDecimal(8),
        //    //        Moneda = reader.GetString(9)
        //    //    });
        //    //}

        //    //_logger.LogInformation("Consulta ejecutada para ReqIdClave: {reqIdClave}, ProveedorId: {proveedorId}", reqIdClave, proveedorId);
        //    //return productos;
        //}


        public async Task<string> CrearAsync(RequisicionDto model)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            string strfin = "";
            string strfinfield = "";
            try
            {
                // Generar la clave si no viene
                var cmdMax = new SqlCommand(@"
            SELECT MAX(CAST(reqIdClave AS INT)) 
            FROM tblRequisiciones 
            WHERE usrIdSoc = @usrIdSoc", conn, tran);

                cmdMax.Parameters.AddWithValue("@usrIdSoc", model.usrIdSoc);

                object? result = await cmdMax.ExecuteScalarAsync();

                int maxNumero = 0;
                if (result != DBNull.Value && result != null)
                    maxNumero = Convert.ToInt32(result);

                int siguienteNumero = maxNumero + 1;

                // Generar nueva clave como número de 10 caracteres relleno con ceros a la izquierda
                string nuevaClave = siguienteNumero.ToString().PadLeft(10, '0');
                model.reqIdClave = nuevaClave;

                if (model.reqIdAcc == "4")
                {
                    strfin = "@fecFin, @hrFin,";
                    strfinfield = "reqFecFin, reqHrFin,";
                }

                // Insertar encabezado
                var cmd = new SqlCommand(@"INSERT INTO tblRequisiciones (reqIdClave, reqFecCreacion, reqHrCreacion," + strfinfield +
                 " usrIdClave, usrIdSoc, reqDescripcion) VALUES (@clave, @fec, @hr," + strfin + " @usr, @soc, @desc)", conn, tran);


                cmd.Parameters.AddWithValue("@clave", nuevaClave);
                cmd.Parameters.AddWithValue("@fec", model.reqFecCreacion);
                cmd.Parameters.AddWithValue("@hr", model.reqHrCreacion);
                DateTime fechaActual = DateTime.Now;
                if (model.reqIdAcc == "4")
                {
                    cmd.Parameters.AddWithValue("@fecFin", (object?)model.reqFecFin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@hrFin", (object?)model.reqHrFin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@fecvigencia", fechaActual);
                }

                // cmd.Parameters.AddWithValue("@fecMod", (object?)model.reqFecMod ?? DBNull.Value);
                // cmd.Parameters.AddWithValue("@hrMod", (object?)model.reqHrMod ?? DBNull.Value);
                // cmd.Parameters.AddWithValue("@fecVoBo", (object?)model.reqFecVoBo ?? DBNull.Value);
                // cmd.Parameters.AddWithValue("@hrVoBo", (object?)model.reqHrVoBo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@usr", model.usrIdClave);
                cmd.Parameters.AddWithValue("@soc", model.usrIdSoc);
                cmd.Parameters.AddWithValue("@desc", model.reqDescripcion);



                //    var cmd = new SqlCommand(@"
                //INSERT INTO tblRequisiciones (
                //    reqIdClave, reqFecCreacion, reqHrCreacion, reqFecMod, reqHrMod, reqFecFin, reqHrFin,
                //    reqFecVoBo, reqHrVoBo, usrIdClave, usrIdSoc)
                //VALUES (@clave, @fec, @hr, @fecMod, @hrMod, @fecFin, @hrFin, @fecVoBo, @hrVoBo, @usr, @soc)", conn, tran);


                await cmd.ExecuteNonQueryAsync();


                int i = 1;
                // Insertar detalles y anexos
                //foreach (var det in model.detalles)
                foreach (var det in model.detalles ?? Enumerable.Empty<DetalleRequisicionDto>())
                {

                    int reqidposNo = i * 10;

                    var detCmd = new SqlCommand(@"
                INSERT INTO tblDetRequisiciones (
                    reqdIdClave, reqidposNo, reqdpMatNo, reqdpMatDes, reqdCantidad,
                    reqdUnidadMed, reqdEspecAnexos, reqdFecEntrega, reqdCiudad, reqdMunicipio,
                    reqdCuenta, reqdIdImp, reqdIdAreaC, reqdProvId,reqdIdSoc)
                VALUES (@id, @pos, @mat, @des, @cant, @uni, @esp, @fec, @ciu, @mun, @cta, @imp, @area, @prov,@socd)", conn, tran);

                    detCmd.Parameters.AddWithValue("@id", nuevaClave);
                    detCmd.Parameters.AddWithValue("@pos", reqidposNo);
                    detCmd.Parameters.AddWithValue("@mat", det.reqdpMatNo);
                    detCmd.Parameters.AddWithValue("@des", det.reqdpMatDes);
                    detCmd.Parameters.AddWithValue("@cant", det.reqdCantidad);
                    detCmd.Parameters.AddWithValue("@uni", (object?)det.reqdUnidadMed ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@esp", (object?)det.reqdEspecAnexos ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@fec", det.reqdFecEntrega);
                    detCmd.Parameters.AddWithValue("@ciu", (object?)det.reqdCiudad ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@mun", (object?)det.reqdMunicipio ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@cta", (object?)det.reqdCuenta ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@imp", (object?)det.reqdIdImp ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@area", (object?)det.reqdIdAreaC ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@prov", (object?)det.reqdProvId ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@socd", (object?)model.usrIdSoc ?? DBNull.Value);

                    await detCmd.ExecuteNonQueryAsync();

                    // Insertar anexo si existe
                    if (det.Anexo != null && !string.IsNullOrWhiteSpace(det.Anexo.contenidoBase64))
                    {
                        byte[] anexoBytes = Convert.FromBase64String(det.Anexo.contenidoBase64);

                        var anexoCmd = new SqlCommand(@"
                    INSERT INTO tblAneRequisiciones (reqAIdClave, reqAIdSoc,reqAidposNo, reqAnexo)
                    VALUES (@clave, @soc1, @pos, @anexo)", conn, tran);

                        anexoCmd.Parameters.AddWithValue("@clave", nuevaClave);
                        anexoCmd.Parameters.AddWithValue("@soc1", model.usrIdSoc);
                        anexoCmd.Parameters.AddWithValue("@pos", reqidposNo);
                        anexoCmd.Parameters.AddWithValue("@anexo", anexoBytes);

                        await anexoCmd.ExecuteNonQueryAsync();
                    }
                    i++;
                }


                if (model.reqIdAcc == "4")
                {
                    var cmdval = new SqlCommand(@" ;WITH Totales AS ( SELECT r2.reqdIdImp,SUM(TRY_CONVERT(decimal(19,4), mm2.mmatPrecioMM)) AS total_requisicion
                                FROM tblDetRequisiciones AS r2  INNER JOIN tblMaestroMaterial AS mm2 ON mm2.mmatIdClave = r2.reqdpMatNo
                                WHERE r2.reqdIdClave = @idreq
                                GROUP BY r2.reqdIdImp)
                            SELECT   m.autNiveles_Aut FROM tblAuthMatrix AS m
                            INNER JOIN tblDetRequisiciones AS r ON r.reqdIdImp = m.autdIdImp
                            INNER JOIN tblMaestroMaterial AS mm ON mm.mmatIdClave = r.reqdpMatNo
                            INNER JOIN Totales AS t ON t.reqdIdImp = r.reqdIdImp
                            WHERE r.reqdIdClave = @idreq
                              AND t.total_requisicion BETWEEN
                                    TRY_CONVERT(decimal(19,4), m.autImpMin)
                                AND TRY_CONVERT(decimal(19,4), m.autImpMax);", conn, tran);

                    cmdval.Parameters.AddWithValue("@idreq", nuevaClave);

                    object? resultVal = await cmdval.ExecuteScalarAsync();

                    string nivelAut = "L1";
                    if (resultVal != DBNull.Value && resultVal != null)
                        nivelAut = resultVal.ToString();

                   var cmdUp = new SqlCommand(@"
                        UPDATE tblRequisiciones
                        SET 
                            reqNiveles_aut = @nivelaut  
                             WHERE reqIdClave = @id and usrIdSoc = @usrIdSoc", conn, tran);



                    cmdUp.Parameters.AddWithValue("@id", nuevaClave);
                    cmdUp.Parameters.AddWithValue("@usrIdSoc", model.usrIdSoc);
                    cmdUp.Parameters.AddWithValue("@nivelaut", nivelAut);

                    var rows1 = await cmdUp.ExecuteNonQueryAsync();
                }
                tran.Commit();


                return nuevaClave;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // 1 nuevo, 2 edicion, 3cerrar, 4 nf
        public async Task<bool> ActualizarRequisicionAsync(RequisicionDto model)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            string strfin = "";
            string strfinfield = "";



            try
            {

                if (model.reqIdAcc == "3")
                {
                    strfin = ",reqFecFin = @fecFin, reqHrFin = @hrFin";


                }
                // Actualizar encabezado (solo campos modificables)
                var cmd = new SqlCommand(@"
            UPDATE tblRequisiciones
            SET 
                reqFecMod = @fecMod, 
                reqHrMod = @hrMod " + strfin + " WHERE reqIdClave = @id and usrIdSoc = @usrIdSoc", conn, tran);



                cmd.Parameters.AddWithValue("@id", model.reqIdClave);
                cmd.Parameters.AddWithValue("@fecMod", (object?)model.reqFecMod ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@hrMod", (object?)model.reqHrMod ?? DBNull.Value);


                if (model.reqIdAcc == "3")
                {
                    cmd.Parameters.AddWithValue("@fecFin", (object?)model.reqFecFin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@hrFin", (object?)model.reqHrFin ?? DBNull.Value);
                }
                // cmd.Parameters.AddWithValue("@fecVoBo", (object?)model.reqFecVoBo ?? DBNull.Value);
                // cmd.Parameters.AddWithValue("@hrVoBo", (object?)model.reqHrVoBo ?? DBNull.Value);
                // cmd.Parameters.AddWithValue("@usrIdClave", model.usrIdClave);
                cmd.Parameters.AddWithValue("@usrIdSoc", model.usrIdSoc);

                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    tran.Rollback();
                    return false; // No existe la requisición para actualizar
                }

                // Borrar detalles y anexos existentes para esta requisición
                var delAnexosCmd = new SqlCommand("DELETE FROM tblAneRequisiciones WHERE reqAIdClave = @id and reqAIdSoc = @soc1 ", conn, tran);
                delAnexosCmd.Parameters.AddWithValue("@id", model.reqIdClave);
                delAnexosCmd.Parameters.AddWithValue("@soc1", model.usrIdSoc);
                await delAnexosCmd.ExecuteNonQueryAsync();

                var delDetallesCmd = new SqlCommand("DELETE FROM tblDetRequisiciones WHERE reqdIdClave = @id and reqdIdSoc = @soc3", conn, tran);
                delDetallesCmd.Parameters.AddWithValue("@id", model.reqIdClave);
                delDetallesCmd.Parameters.AddWithValue("@soc3", model.usrIdSoc);
                await delDetallesCmd.ExecuteNonQueryAsync();

                // Insertar detalles y anexos nuevos
                foreach (var det in model.detalles)
                {




                    var detCmd = new SqlCommand(@"
                INSERT INTO tblDetRequisiciones (
                    reqdIdClave, reqidposNo, reqdpMatNo, reqdpMatDes, reqdCantidad,
                    reqdUnidadMed, reqdEspecAnexos, reqdFecEntrega, reqdCiudad, reqdMunicipio,
                    reqdCuenta, reqdIdImp, reqdIdAreaC, reqdProvId,reqdIdSoc)
                VALUES (@id, @pos, @mat, @des, @cant, @uni, @esp, @fec, @ciu, @mun, @cta, @imp, @area, @prov,@socd)", conn, tran);

                    detCmd.Parameters.AddWithValue("@id", model.reqIdClave);
                    detCmd.Parameters.AddWithValue("@pos", det.reqidposNo);
                    detCmd.Parameters.AddWithValue("@mat", det.reqdpMatNo);
                    detCmd.Parameters.AddWithValue("@des", det.reqdpMatDes);
                    detCmd.Parameters.AddWithValue("@cant", det.reqdCantidad);
                    detCmd.Parameters.AddWithValue("@uni", (object?)det.reqdUnidadMed ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@esp", (object?)det.reqdEspecAnexos ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@fec", det.reqdFecEntrega);
                    detCmd.Parameters.AddWithValue("@ciu", (object?)det.reqdCiudad ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@mun", (object?)det.reqdMunicipio ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@cta", (object?)det.reqdCuenta ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@imp", (object?)det.reqdIdImp ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@area", (object?)det.reqdIdAreaC ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@prov", (object?)det.reqdProvId ?? DBNull.Value);
                    detCmd.Parameters.AddWithValue("@socd", (object?)model.usrIdSoc ?? DBNull.Value);

                    await detCmd.ExecuteNonQueryAsync();

                    if (det.Anexo != null && !string.IsNullOrWhiteSpace(det.Anexo.contenidoBase64))
                    {
                        byte[] anexoBytes = Convert.FromBase64String(det.Anexo.contenidoBase64);

                        var anexoCmd = new SqlCommand(@"
                    INSERT INTO tblAneRequisiciones (reqAIdClave, reqAIdSoc, reqAidposNo, reqAnexo)
                    VALUES (@clave, @soc2, @pos1, @anexo)", conn, tran);

                        anexoCmd.Parameters.AddWithValue("@clave", det.Anexo.reqIdClave);
                        anexoCmd.Parameters.AddWithValue("@soc2", det.Anexo.reqAIdSoc);
                        anexoCmd.Parameters.AddWithValue("@pos1", det.Anexo.reqidposNo);
                        anexoCmd.Parameters.AddWithValue("@anexo", anexoBytes);

                        await anexoCmd.ExecuteNonQueryAsync();
                    }
                }


                if  (model.reqIdAcc == "3")
                    {
                    var cmdval = new SqlCommand(@" ;WITH Totales AS ( SELECT r2.reqdIdImp,SUM(TRY_CONVERT(decimal(19,4), mm2.mmatPrecioMM)) AS total_requisicion
                                FROM tblDetRequisiciones AS r2  INNER JOIN tblMaestroMaterial AS mm2 ON mm2.mmatIdClave = r2.reqdpMatNo
                                WHERE r2.reqdIdClave = @idreq
                                GROUP BY r2.reqdIdImp)
                            SELECT   m.autNiveles_Aut FROM tblAuthMatrix AS m
                            INNER JOIN tblDetRequisiciones AS r ON r.reqdIdImp = m.autdIdImp
                            INNER JOIN tblMaestroMaterial AS mm ON mm.mmatIdClave = r.reqdpMatNo
                            INNER JOIN Totales AS t ON t.reqdIdImp = r.reqdIdImp
                            WHERE r.reqdIdClave = @idreq
                              AND t.total_requisicion BETWEEN
                                    TRY_CONVERT(decimal(19,4), m.autImpMin)
                                AND TRY_CONVERT(decimal(19,4), m.autImpMax);", conn, tran);

                    cmdval.Parameters.AddWithValue("@idreq", model.reqIdClave);

                    object? resultVal = await cmdval.ExecuteScalarAsync();

                    string nivelAut = "L1";
                    if (resultVal != DBNull.Value && resultVal != null)
                        nivelAut = resultVal.ToString();

                    var cmdUp = new SqlCommand(@"
                        UPDATE tblRequisiciones
                        SET 
                            reqNiveles_aut = @nivelaut  
                             WHERE reqIdClave = @id and usrIdSoc = @usrIdSoc", conn, tran);



                    cmdUp.Parameters.AddWithValue("@id", model.reqIdClave);
                    cmdUp.Parameters.AddWithValue("@usrIdSoc", model.usrIdSoc);
                    cmdUp.Parameters.AddWithValue("@nivelaut", nivelAut);

                    var rows1 = await cmdUp.ExecuteNonQueryAsync();
                }


                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }


 

        public async Task<List<RequisicionDetUsuAut>> ObtenerRequisiciones(string reqIdClave)
        {
            var lista = new List<RequisicionDetUsuAut>();

            string query = @"
        SELECT req.reqIdClave, req.reqdIdImp, req.usrIdSoc,
               u.usrCorreo, u.usrNombre, u.usrApellidoP, u.usrApellidoM, ui.usrNivel
        FROM (
            SELECT DISTINCT r.reqIdClave, reqdIdImp, r.usrIdSoc
            FROM [dbo].[tblRequisiciones] r
            INNER JOIN [dbo].[tblDetRequisiciones] dr
                ON r.reqIdClave = dr.reqdIdClave
                AND r.usrIdSoc = dr.reqdIdSoc
        ) req
        INNER JOIN [dbo].[tblUsrImputacion] ui
            ON ui.usrIdImp = req.reqdIdImp
            AND ui.usrIdRol = 'R06'
            AND (ui.usrNivel IS NOT NULL AND ui.usrNivel <> '')
        INNER JOIN [dbo].[tblUsuario] u
            ON u.usrIdClave = ui.usrIdClave AND u.usrIdSoc = req.usrIdSoc
        WHERE req.reqIdClave = @ReqIdClave and ui.usrNivel in (select reqLevelCode from  [dbo].[tblFlujoAut] where reqIdClave =  @ReqIdClave )";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ReqIdClave", reqIdClave);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new RequisicionDetUsuAut
                        {
                            ReqIdClave = reader["reqIdClave"].ToString(),
                            ReqdIdImp = reader["reqdIdImp"].ToString(),
                            UsrIdSoc = reader["usrIdSoc"].ToString(),
                            UsrCorreo = reader["usrCorreo"].ToString(),
                            UsrNombre = reader["usrNombre"].ToString(),
                            UsrApellidoP = reader["usrApellidoP"].ToString(),
                            UsrApellidoM = reader["usrApellidoM"].ToString(),
                            UsrNivel = reader["usrNivel"].ToString()
                        });
                    }
                }
            }

            return lista;
        }
 


        public async Task<bool> ActualizarReqProvGanAsync(string idReq, string idprov, string soc  )
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();

            try
            {


                // Actualizar encabezado (solo campos modificables)
                var cmd = new SqlCommand(@"
            UPDATE tblRequisiciones
            SET 
                reqNotifFecUsr =  CURRENT_TIMESTAMP,

                reqProvGan = @prov WHERE reqIdClave = @id and usrIdSoc = @usrIdSoc", conn, tran);

                cmd.Parameters.AddWithValue("@id", idReq);
                cmd.Parameters.AddWithValue("@usrIdSoc", soc);  
                cmd.Parameters.AddWithValue("@prov", idprov);

                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    tran.Rollback();
                    return false; // No existe la requisición para actualizar
                }

                //await cn.OpenAsync(ct);
                var cmd1 = new SqlCommand("dbo.sp_GenerarFlujoAut", conn, tran) { CommandType = CommandType.StoredProcedure };
                cmd1.Parameters.AddWithValue("@reqIdClave", idReq);
                cmd1.Parameters.AddWithValue("@usrIdSoc", soc);
                cmd1.Parameters.AddWithValue("@reqpprov", idprov);

                await cmd1.ExecuteNonQueryAsync();



                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }



        public async Task<RequisicionDto?> ObtenerRequisicionAsync(string id, string soc)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Obtener encabezado de requisición
            var cmd = new SqlCommand("SELECT * FROM tblRequisiciones WHERE reqIdClave = @id and usrIdSoc = @soc", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@soc", soc);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            RequisicionDto? model = null;

            if (await reader.ReadAsync())
            {
                model = new RequisicionDto
                {
                    reqIdClave = reader["reqIdClave"].ToString()!,
                    reqFecCreacion = Convert.ToDateTime(reader["reqFecCreacion"]),
                    reqHrCreacion = Convert.ToDateTime(reader["reqHrCreacion"]),
                    reqFecMod = reader["reqFecMod"] as DateTime?,
                    reqHrMod = reader["reqHrMod"] as DateTime?,
                    reqFecFin = reader["reqFecFin"] as DateTime?,
                    reqHrFin = reader["reqHrFin"] as DateTime?,
                    reqFecVoBo = reader["reqFecVoBo"] as DateTime?,
                    reqHrVoBo = reader["reqHrVoBo"] as DateTime?,
                    usrIdClave = reader["usrIdClave"].ToString()!,
                    usrIdSoc = reader["usrIdSoc"].ToString()!,
                    reqDescripcion = reader["reqDescripcion"].ToString()!,
                    detalles = new List<DetalleRequisicionDto>()
                };
            }
            reader.Close();

            if (model == null)
                return null;

            // Obtener detalles
            var detCmd = new SqlCommand("SELECT * FROM tblDetRequisiciones WHERE reqdIdClave = @id and reqdIdSoc = @soc ORDER BY reqidposNo", conn);
            detCmd.Parameters.AddWithValue("@id", id);
            detCmd.Parameters.AddWithValue("@soc", soc);

            using var detReader = await detCmd.ExecuteReaderAsync();
            var detalles = new List<DetalleRequisicionDto>();

            while (await detReader.ReadAsync())
            {
                detalles.Add(new DetalleRequisicionDto
                {
                    reqIdClave = id,
                    reqdIdSoc = detReader["reqdIdSoc"].ToString()!,
                    reqidposNo = Convert.ToInt32(detReader["reqidposNo"]),
                    reqdpMatNo = detReader["reqdpMatNo"].ToString()!,
                    reqdpMatDes = detReader["reqdpMatDes"].ToString()!,
                    reqdCantidad = Convert.ToInt32(detReader["reqdCantidad"]),
                    reqdUnidadMed = detReader["reqdUnidadMed"]?.ToString(),
                    reqdEspecAnexos = detReader["reqdEspecAnexos"]?.ToString(),
                    reqdFecEntrega = Convert.ToDateTime(detReader["reqdFecEntrega"]),
                    reqdCiudad = detReader["reqdCiudad"]?.ToString(),
                    reqdMunicipio = detReader["reqdMunicipio"]?.ToString(),
                    reqdCuenta = detReader["reqdCuenta"]?.ToString(),
                    reqdIdImp = detReader["reqdIdImp"]?.ToString(),
                    reqdIdAreaC = detReader["reqdIdAreaC"]?.ToString(),
                    reqdProvId = detReader["reqdProvId"]?.ToString(),

                    Anexo = null // Lo llenaremos después
                });
            }
            detReader.Close();

            // Obtener anexos por detalle
            var anexoCmd = new SqlCommand("SELECT reqAidposNo, reqAnexo, reqAIdSoc FROM tblAneRequisiciones WHERE reqAIdClave = @id and reqAIdSoc = @soc", conn);
            anexoCmd.Parameters.AddWithValue("@id", id);
            anexoCmd.Parameters.AddWithValue("@soc", soc);

            using var anexoReader = await anexoCmd.ExecuteReaderAsync();
            var anexosDict = new Dictionary<int, byte[]>();

            while (await anexoReader.ReadAsync())
            {
                int posNo = Convert.ToInt32(anexoReader["reqAidposNo"]);
                string? idsoc = anexoReader["reqAIdSoc"]?.ToString();
                byte[] anexoBytes = (byte[])anexoReader["reqAnexo"];
                anexosDict[posNo] = anexoBytes;
            }
            anexoReader.Close();

            // Asignar anexos a detalles
            foreach (var det in detalles)
            {
                //    if (anexosDict.TryGetValue(det.reqidposNo, out var anexoBytes))
                if (det.reqidposNo.HasValue && anexosDict.TryGetValue(det.reqidposNo.Value, out var anexoBytes))

                {
                    det.Anexo = new AnexoDto
                    {
                        reqIdClave = det.reqIdClave,
                        reqAIdSoc = det.reqdIdSoc,
                        reqidposNo = det.reqidposNo,
                        contenidoBase64 = Convert.ToBase64String(anexoBytes)
                    };
                }
            }

            model.detalles = detalles;

            return model;
        }

        public async Task<List<RequisicionDto>> ObtenerTodasAsync(string soc, string idusuario)
        {
            var lista = new List<RequisicionDto>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
    SELECT reqIdClave, reqFecCreacion, reqHrCreacion, usrIdClave, usrIdSoc, reqFecFin, reqHrFin
    FROM tblRequisiciones
    WHERE usrIdSoc = @soc AND usrIdClave = @idusuario
    ORDER BY reqFecCreacion DESC;", conn);

            // Mejor que AddWithValue: fija tipos (ajusta SqlDbType y tamaños a tu esquema real)
            cmd.Parameters.Add("@soc", SqlDbType.VarChar, 10).Value = soc;
            cmd.Parameters.Add("@idusuario", SqlDbType.VarChar, 50).Value = idusuario;

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows) return new List<RequisicionDto>(); // evita null si prefieres

            // Obtén los ordinales una sola vez (mejor rendimiento)
            int oReqIdClave = reader.GetOrdinal("reqIdClave");
            int oReqFecCreacion = reader.GetOrdinal("reqFecCreacion");
            int oReqHrCreacion = reader.GetOrdinal("reqHrCreacion"); // si es time
            int oUsrIdClave = reader.GetOrdinal("usrIdClave");
            int oUsrIdSoc = reader.GetOrdinal("usrIdSoc");
            int oReqFecFin = reader.GetOrdinal("reqFecFin");
            int oReqHrFin = reader.GetOrdinal("reqHrFin");

//            var lista = new List<RequisicionDto>();

            while (await reader.ReadAsync())
            {
                lista.Add(new RequisicionDto
                {
                    reqIdClave = reader.IsDBNull(oReqIdClave) ? "" : reader.GetString(oReqIdClave),
                    reqFecCreacion = reader.GetDateTime(oReqFecCreacion),

                    // Si reqHrCreacion es time NULLABLE en SQL:
                    reqHrCreacion = reader.GetDateTime(oReqFecCreacion),

                    usrIdClave = reader.IsDBNull(oUsrIdClave) ? "" : reader.GetString(oUsrIdClave),
                    usrIdSoc = reader.IsDBNull(oUsrIdSoc) ? "" : reader.GetString(oUsrIdSoc),

                    // Manejo seguro de NULL:
                    reqFecFin = reader.IsDBNull(oReqFecFin)
                                     ? (DateTime?)null
                                     : reader.GetDateTime(oReqFecFin),

                    reqHrFin = reader.IsDBNull(oReqHrFin)
                                     ? (DateTime?)null
                                     : reader.GetDateTime(oReqHrFin),

                    detalles = new List<DetalleRequisicionDto>() // vacío para listado
                });
            }

            return lista;

            // Seleccionamos solo datos principales para la lista
            //            var cmd = new SqlCommand(@"
            //        SELECT reqIdClave, reqFecCreacion, reqHrCreacion, usrIdClave, usrIdSoc,reqFecFin,reqHrFin
            //        FROM tblRequisiciones WHERE usrIdSoc = @soc and usrIdClave = @idusuario 
            //        ORDER BY reqFecCreacion DESC", conn);
            //            cmd.Parameters.AddWithValue("@soc", soc);
            //            cmd.Parameters.AddWithValue("@idusuario", idusuario);
            //            using var reader = await cmd.ExecuteReaderAsync();

            //            if (!reader.HasRows)
            //                return null;

            //            while (await reader.ReadAsync())
            //            {
            //                lista.Add(new RequisicionDto
            //                {
            //                    reqIdClave = reader["reqIdClave"].ToString()!,
            //                    reqFecCreacion = Convert.ToDateTime(reader["reqFecCreacion"]),
            //                    reqHrCreacion = Convert.ToDateTime(reader["reqHrCreacion"]),
            //                    usrIdClave = reader["usrIdClave"].ToString()!,
            //                    usrIdSoc = reader["usrIdSoc"].ToString()!,
            //                    reqFecFin = reader.IsDBNull(reqFecFin)
            //                         ? (DateTime?)null
            //                         : reader.GetDateTime(oReqFecFin),
            //,
            //                    reqHrFin = Convert.ToDateTime(reader["reqHrFin"])!,
            //                    detalles = new List<DetalleRequisicionDto>() // vacío para listado
            //                });
            //            }
            //            if (lista == null)
            //                return null;
            //            return lista;
        }

        public async Task<bool> EliminarRequisicionAsync(string id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();

            try
            {
                // Borrar anexos
                var cmdDeleteAne = new SqlCommand("DELETE FROM tblAneRequisiciones WHERE reqAIdClave = @id", conn, tran);
                cmdDeleteAne.Parameters.AddWithValue("@id", id);
                await cmdDeleteAne.ExecuteNonQueryAsync();

                // Borrar detalles
                var cmdDeleteDet = new SqlCommand("DELETE FROM tblDetRequisiciones WHERE reqdIdClave = @id", conn, tran);
                cmdDeleteDet.Parameters.AddWithValue("@id", id);
                await cmdDeleteDet.ExecuteNonQueryAsync();

                // Borrar encabezado
                var cmdDeleteReq = new SqlCommand("DELETE FROM tblRequisiciones WHERE reqIdClave = @id", conn, tran);
                cmdDeleteReq.Parameters.AddWithValue("@id", id);
                var rows = await cmdDeleteReq.ExecuteNonQueryAsync();

                await tran.CommitAsync();

                return rows > 0;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        async Task<List<EvaluacionReq>> IRequisicionService.ObtenerEvalAsync(string soc, string requisicion, string opcion)
        {
            var lista = new List<EvaluacionReq>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Seleccionamos solo datos principales para la lista
            var cmd = new SqlCommand(@"


SELECT req.reqIdClave,req.usrIdSoc,
    prov.provIdProv,
    prov.provNombre,
    CASE 
        WHEN req.reqNotifFecUsr IS NULL THEN '0' 
        ELSE '1' 
    END AS notificado,
    -- Suma de cada criterio individual
    SUM(
        CASE prov.ProvClasificacion
            WHEN 'B' THEN 1.0
            WHEN 'R' THEN 0.75
            WHEN 'M' THEN 0.6
            WHEN 'P' THEN 0.0
            ELSE 0.0
        END * (criterios.ponderacion_calidad / 100.0)
    ) AS suma_calidad,

    SUM(
        (subprov.min_precio / provr.reqppPrecUnit) * (criterios.ponderacion_precio / 100.0)
    ) AS suma_precio,

    SUM(
        CASE 
            WHEN DATEDIFF(DAY, reqd.reqdFecEntrega, provr.reqppFecEntrega) <= 0 THEN 1.0
            ELSE 
                CASE 
                    WHEN 1.0 - (DATEDIFF(DAY, reqd.reqdFecEntrega, provr.reqppFecEntrega) / 10.0) < 0 THEN 0.0
                    ELSE 1.0 - (DATEDIFF(DAY, reqd.reqdFecEntrega, provr.reqppFecEntrega) / 10.0)
                END
        END * (criterios.ponderacion_entrega / 100.0)
    ) AS suma_entrega,

    SUM(
        CASE 
            WHEN provr.reqppCargoExt = 0 THEN 1.0
            ELSE 1.0 - (provr.reqppCargoExt / 1000.0)
        END * (criterios.ponderacion_costo / 100.0)
    ) AS suma_costo_adicional,

    SUM(
        CASE 
            WHEN TRY_CAST(provr.reqppCondPago AS float) > 0 THEN 
                CAST(provcc.condpago / TRY_CAST(provr.reqppCondPago AS float) AS decimal(10,4))
            ELSE 0.0
        END * (criterios.ponderacion_pago / 100.0)
    ) AS suma_pago,

    SUM(1.0 * (criterios.ponderacion_atencion / 100.0)) AS suma_atencion,

    SUM(
        CASE 
            WHEN hp.hisIdMaterial IS NOT NULL THEN 1.0
            WHEN mm.mmatIdClave IS NOT NULL THEN 1.0
            ELSE 0.0
        END * (criterios.ponderacion_historial / 100.0)
    ) AS suma_historial,

    -- Suma total del score
    ROUND(SUM(
        (subprov.min_precio / provr.reqppPrecUnit) * (criterios.ponderacion_precio / 100.0) +
        CASE prov.ProvClasificacion
            WHEN 'B' THEN 1.0
            WHEN 'R' THEN 0.75
            WHEN 'M' THEN 0.6
            WHEN 'P' THEN 0.0
            ELSE 0.0
        END * (criterios.ponderacion_calidad / 100.0) +
        CASE 
            WHEN DATEDIFF(DAY, reqd.reqdFecEntrega, provr.reqppFecEntrega) <= 0 THEN 1.0
            ELSE 
                CASE 
                    WHEN 1.0 - (DATEDIFF(DAY, reqd.reqdFecEntrega, provr.reqppFecEntrega) / 10.0) < 0 THEN 0.0
                    ELSE 1.0 - (DATEDIFF(DAY, reqd.reqdFecEntrega, provr.reqppFecEntrega) / 10.0)
                END
        END * (criterios.ponderacion_entrega / 100.0) +
        CASE 
            WHEN provr.reqppCargoExt = 0 THEN 1.0
            ELSE 1.0 - (provr.reqppCargoExt / 1000.0)
        END * (criterios.ponderacion_costo / 100.0) +
        CASE 
            WHEN TRY_CAST(provr.reqppCondPago AS float) > 0 THEN 
                CAST(provcc.condpago / TRY_CAST(provr.reqppCondPago AS float) AS decimal(10,4))
            ELSE 0.0
        END * (criterios.ponderacion_pago / 100.0) +
        1.0 * (criterios.ponderacion_atencion / 100.0) +
        CASE 
            WHEN hp.hisIdMaterial IS NOT NULL THEN 1.0
            WHEN mm.mmatIdClave IS NOT NULL THEN 1.0
            ELSE 0.0
        END * (criterios.ponderacion_historial / 100.0)
    ), 4) AS score_total_proveedor

FROM [dbo].[tblRequisiciones] req 
INNER JOIN [dbo].[tblDetRequisiciones] reqd  ON reqd.reqdIdClave = req.reqIdClave AND reqd.reqdIdSoc = req.usrIdSoc
INNER JOIN [dbo].[tblSociedad] soc ON soc.socIdSoc = req.usrIdSoc
INNER JOIN [dbo].[tblDetRequisicionesProv] detrpro ON detrpro.reqdIdClave = reqd.reqdIdClave AND detrpro.reqdIdSoc = reqd.reqdIdSoc AND detrpro.reqidposNo = reqd.reqidposNo
INNER JOIN [dbo].[tblProveedores] prov ON prov.provIdProv = detrpro.reqdProvId AND prov.provIdSoc = reqd.reqdIdSoc
LEFT JOIN [dbo].[tblMaestroMaterial] mm ON mm.mmatIdClave = reqd.reqdpMatNo
LEFT JOIN [dbo].[tblHisPedidos] hp ON hp.hisIdMaterial = reqd.reqdpMatNo
INNER JOIN [dbo].[tblprovRequisiciones] provr ON provr.reqpIdClave = detrpro.reqdIdClave AND provr.reqppIdSoc = detrpro.reqdIdSoc AND provr.reqppPosNo = detrpro.reqidposNo AND provr.reqppProvId = prov.provIdProv
INNER JOIN (
    SELECT reqpIdClave, reqppIdSoc, reqppPosNo, MIN(reqppPrecUnit) AS min_precio
    FROM [dbo].[tblprovRequisiciones]
    GROUP BY reqpIdClave, reqppIdSoc, reqppPosNo
) subprov ON subprov.reqpIdClave = provr.reqpIdClave AND subprov.reqppIdSoc = provr.reqppIdSoc AND subprov.reqppPosNo = provr.reqppPosNo
INNER JOIN (
    SELECT reqpIdClave, reqppIdSoc, reqppPosNo, MIN(reqppCondPagoNum) AS condpago
    FROM (
        SELECT reqpIdClave, reqppIdSoc, reqppPosNo, TRY_CAST(reqppCondPago AS float) AS reqppCondPagoNum
        FROM [dbo].[tblprovRequisiciones]
    ) AS Converted
    GROUP BY reqpIdClave, reqppIdSoc, reqppPosNo
) provcc ON provcc.reqpIdClave = provr.reqpIdClave AND provcc.reqppIdSoc = provr.reqppIdSoc AND provcc.reqppPosNo = provr.reqppPosNo
INNER JOIN (
    SELECT criIdSoc,
           MAX(CASE WHEN criId = 100 THEN criPonderacion END) AS ponderacion_calidad,
           MAX(CASE WHEN criId = 105 THEN criPonderacion END) AS ponderacion_precio,
           MAX(CASE WHEN criId = 110 THEN criPonderacion END) AS ponderacion_entrega,
           MAX(CASE WHEN criId = 115 THEN criPonderacion END) AS ponderacion_costo,
           MAX(CASE WHEN criId = 120 THEN criPonderacion END) AS ponderacion_pago,
           MAX(CASE WHEN criId = 125 THEN criPonderacion END) AS ponderacion_atencion,
           MAX(CASE WHEN criId = 130 THEN criPonderacion END) AS ponderacion_historial
    FROM dbo.tblCriterio
    WHERE criId IN (100,105,110,115,120,125,130)
    GROUP BY criIdSoc
) criterios ON criterios.criIdSoc = soc.socIdSoc

WHERE req.reqIdClave = @requisicion AND req.usrIdSoc = @soc
and req.reqFecVoBo is not null
GROUP BY req.reqIdClave, req.usrIdSoc, prov.provIdProv, prov.provNombre,req.reqNotifFecUsr,
         criterios.ponderacion_calidad, criterios.ponderacion_precio,
         criterios.ponderacion_entrega, criterios.ponderacion_costo,
         criterios.ponderacion_pago, criterios.ponderacion_atencion,
         criterios.ponderacion_historial

ORDER BY score_total_proveedor DESC  

", conn);
            cmd.Parameters.AddWithValue("@soc", soc);
            cmd.Parameters.AddWithValue("@requisicion", requisicion);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return null;

            while (await reader.ReadAsync())
            {
                lista.Add(new EvaluacionReq
                {
                    ReqIdClave = reader["reqIdClave"].ToString()!,
                    UsrIdSoc = reader.IsDBNull(reader.GetOrdinal("usrIdSoc")) ? 0 : Convert.ToInt32(reader["usrIdSoc"]),
                     reqNotifFecUsr = reader["notificado"].ToString()!,
 //                   reader["reqNotifFecUsr"].ToString()!,
                    //ReqIdPosNo = reader.IsDBNull(reader.GetOrdinal("reqidposNo")) ? 0 : Convert.ToInt32(reader["reqidposNo"]),
                    ReqppProvId = reader.IsDBNull(reader.GetOrdinal("provIdProv")) ? 0 : Convert.ToInt32(reader["provIdProv"]),
                    ReqppProvNombre = reader["provNombre"].ToString()!,
                    Calidad = reader.IsDBNull(reader.GetOrdinal("suma_calidad")) ? 0 : Convert.ToDouble(reader["suma_calidad"]),
                    PrecioMenor = reader.IsDBNull(reader.GetOrdinal("suma_precio")) ? 0 : Convert.ToDouble(reader["suma_precio"]),
                    Entrega = reader.IsDBNull(reader.GetOrdinal("suma_entrega")) ? 0 : Convert.ToDouble(reader["suma_entrega"]),
                    CostoAdicional = reader.IsDBNull(reader.GetOrdinal("suma_costo_adicional")) ? 0 : Convert.ToDouble(reader["suma_costo_adicional"]),
                    CPago = reader.IsDBNull(reader.GetOrdinal("suma_pago")) ? 0 : Convert.ToDouble(reader["suma_pago"]),
                    Atencion = reader.IsDBNull(reader.GetOrdinal("suma_atencion")) ? 0 : Convert.ToDouble(reader["suma_atencion"]),
                    Historial = reader.IsDBNull(reader.GetOrdinal("suma_historial")) ? 0 : Convert.ToDouble(reader["suma_historial"]),
                    ScoreTotal = reader.IsDBNull(reader.GetOrdinal("score_total_proveedor")) ? 0 : Convert.ToDouble(reader["score_total_proveedor"])
                });
            }
            if (lista == null)
                return null;
            return lista;

        }




        public async Task<GuardarDetalleProvResultadoBatch> GuardarDetalleProvAsync(
              string soc,
              AutProvReqCerradaDetalleProvDtoList request,
              CancellationToken ct = default)
        {
            //var cs = _config.GetConnectionString("DefaultConnection")
            //    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' no configurada.");





            // Helpers de truncado
            static string Trunc(string? s, int max)
            {
                if (string.IsNullOrEmpty(s)) return string.Empty;
                s = s.Trim();
                return s.Length <= max ? s : s.Substring(0, max);
            }
            static string? TruncOrNull(string? s, int max)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;
                s = s.Trim();
                return s.Length <= max ? s : s.Substring(0, max);
            }
            //string? ProvValido(string? s10)
            //{
            //    if (string.IsNullOrWhiteSpace(s10)) return null;
            //    var t = Trunc(s10, 10);
            //    return t == "000000" ? null : t;
            //}
            // ⬇️ Devuelve null si es placeholder hecho solo de ceros (soporta "000000", "0000000000", etc.)
            static string? ProvValido(string? s10)
            {
                if (string.IsNullOrWhiteSpace(s10)) return null;
                s10 = s10.Trim();
                var t = s10.Length <= 10 ? s10 : s10.Substring(0, 10);
                return t.All(ch => ch == '0') ? null : t;
            }



            int detallesUpsert = 0;
            int provsInsert = 0;
            int vobos = 0;

            //await using var cn = new SqlConnection(cs);
            //await cn.OpenAsync(ct);


            List<AutProvPosicionList> litaProveedores = TransformarPorProveedor(request.Modelos);

            //obtiene datos del usuario 
            ReqUsuarioMail usuariomail = new ReqUsuarioMail();
            usuariomail = await ObtenerUsuarioMail(request.Modelos[0].reqdIdClave);


            var emails = EmailTemplateBuilder.BuildCotizacionPapeleriaHtmlPerProveedor(
                            data: litaProveedores,     // tu List<AutProvPosicionList>
                            empresa: "Tu Empresa S.A. de C.V.",
                            area: "Abastecimientos",
                            remitenteNombre: usuariomail.usrNombre,
                            remitenteCargo: "Coordinador de Compras",
                            remitenteTelefono: "+52 55 0000 0000",
                            remitenteCorreo: usuariomail.usrCorreo
                        );
            
            // Enviar cada paquete (ej. con MailKit, SMTP o tu API):
            foreach (var pkg in emails)
            {
                // pkg.Prov     -> proveedor
                // pkg.Subject  -> asunto
                // pkg.Html     -> cuerpo HTML listo

                //        public async Task<MailResponse> EnviarCorreoInvitacionAsync(string prov, string subject, string html, string tocorreo)
                string provMail = await ObtenerMailProv(pkg.Prov);
                if (_mailService == null)
                    throw new InvalidOperationException("IMailService no fue inyectado: _mailService == null");

                _logger?.LogInformation("Voy a enviar correo a {to} prov:{prov}", "alferch@gmail.com", pkg.Prov);


                var resultado = await _mailService.EnviarCorreoInvitacionAsync(pkg.Prov, pkg.Subject, pkg.Html, provMail);


            }

            using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync();

            List<AutProvReqCerradaDetalleProvDto> modelos;


            var accion = (request.accion ?? string.Empty).Trim();


            var isGuardar =
                accion.Equals("Guardar", StringComparison.OrdinalIgnoreCase) ||
                accion.Equals("nuevo", StringComparison.OrdinalIgnoreCase);


            modelos = request.Modelos;
            // Cualquier error SQL aborta la transacción
            await using (var cmdAbort = new SqlCommand("SET XACT_ABORT ON;", cn))
                await cmdAbort.ExecuteNonQueryAsync(ct);

            // 🔒 TODO-O-NADA: una transacción para todo el lote
            await using var tx = cn.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                foreach (var dto in modelos)
                {
                    // Normalización y truncado
                    var reqIdClave = Trunc(dto.reqIdClave, 15);
                    var reqdIdClave = Trunc(dto.reqdIdClave, 15);
                    var reqdIdSoc = Trunc(soc, 4);
                    var reqdpMatNo = Trunc(dto.reqdpMatNo, 18);
                    var reqdpMatDes = Trunc(dto.reqdpMatDes, 100);
                    var reqdUnidadMed = TruncOrNull(dto.reqdUnidadMed, 6);
                    var reqdCiudad = TruncOrNull(dto.reqdCiudad, 50);
                    var reqdMunicipio = TruncOrNull(dto.reqdMunicipio, 50);
                    var authorize = dto.authorize;


                    //                   var isGuardar = (dto.accion ?? "").Trim().Equals("Guardar", StringComparison.OrdinalIgnoreCase);
                    //
                    // -------- 1) UPSERT: tblDetRequisiciones
                    const string sqlUpsertDet = @"
IF EXISTS (SELECT 1
             FROM dbo.tblDetRequisiciones WITH (UPDLOCK, HOLDLOCK)
            WHERE reqdIdClave = @reqdIdClave
              AND reqdIdSoc   = @reqdIdSoc
              AND reqidposNo  = @reqidposNo)
BEGIN
    UPDATE dbo.tblDetRequisiciones
       SET reqdpMatNo     = @reqdpMatNo,
           reqdpMatDes    = @reqdpMatDes,
           reqdCantidad   = @reqdCantidad,
           reqdUnidadMed  = @reqdUnidadMed,
           reqdFecEntrega = @reqdFecEntrega,
           reqdCiudad     = @reqdCiudad,
           reqdMunicipio  = @reqdMunicipio,
           reqdAuth       = @reqdAuth
     WHERE reqdIdClave = @reqdIdClave
       AND reqdIdSoc   = @reqdIdSoc
       AND reqidposNo  = @reqidposNo;
END
ELSE
BEGIN
    INSERT INTO dbo.tblDetRequisiciones
        (reqdIdClave, reqdIdSoc, reqidposNo, reqdpMatNo, reqdpMatDes,
         reqdCantidad, reqdUnidadMed, reqdFecEntrega, reqdCiudad, reqdMunicipio, reqdAuth, reqdProvId)
    VALUES
        (@reqdIdClave, @reqdIdSoc, @reqidposNo, @reqdpMatNo, @reqdpMatDes,
         @reqdCantidad, @reqdUnidadMed, @reqdFecEntrega, @reqdCiudad, @reqdMunicipio, @reqdAuth, NULL);
END";
                    await using (var cmdDet = new SqlCommand(sqlUpsertDet, cn, tx))
                    {
                        cmdDet.Parameters.Add("@reqdIdClave", SqlDbType.NVarChar, 15).Value = reqdIdClave;
                        cmdDet.Parameters.Add("@reqdIdSoc", SqlDbType.NVarChar, 4).Value = reqdIdSoc;
                        cmdDet.Parameters.Add("@reqidposNo", SqlDbType.Int).Value = dto.reqidposNo;

                        cmdDet.Parameters.Add("@reqdpMatNo", SqlDbType.NVarChar, 18).Value = reqdpMatNo;
                        cmdDet.Parameters.Add("@reqdpMatDes", SqlDbType.NVarChar, 100).Value = reqdpMatDes;
                        cmdDet.Parameters.Add("@reqdCantidad", SqlDbType.Int).Value = dto.reqdCantidad;
                        cmdDet.Parameters.Add("@reqdUnidadMed", SqlDbType.NVarChar, 6).Value = (object?)reqdUnidadMed ?? DBNull.Value;
                        cmdDet.Parameters.Add("@reqdFecEntrega", SqlDbType.SmallDateTime).Value = dto.reqdFecEntrega;
                        cmdDet.Parameters.Add("@reqdCiudad", SqlDbType.NVarChar, 50).Value = (object?)reqdCiudad ?? DBNull.Value;
                        cmdDet.Parameters.Add("@reqdMunicipio", SqlDbType.NVarChar, 50).Value = (object?)reqdMunicipio ?? DBNull.Value;

                        cmdDet.Parameters.Add("@reqdAuth", SqlDbType.Int).Value = authorize;

                        await cmdDet.ExecuteNonQueryAsync(ct);
                        detallesUpsert++;
                    }

                    // -------- 2) Reconstrucción de proveedores: DELETE + INSERT válidos
                    const string sqlDeleteProv = @"
DELETE FROM dbo.tblDetRequisicionesProv
 WHERE reqdIdClave = @reqdIdClave
   AND reqdIdSoc   = @reqdIdSoc
   AND reqidposNo  = @reqidposNo;";
                    await using (var cmdDel = new SqlCommand(sqlDeleteProv, cn, tx))
                    {
                        cmdDel.Parameters.Add("@reqdIdClave", SqlDbType.NVarChar, 15).Value = reqdIdClave;
                        cmdDel.Parameters.Add("@reqdIdSoc", SqlDbType.NVarChar, 4).Value = reqdIdSoc;
                        cmdDel.Parameters.Add("@reqidposNo", SqlDbType.Int).Value = dto.reqidposNo;
                        await cmdDel.ExecuteNonQueryAsync(ct);
                    }

                    var provs = new List<(string provId, int consec)>(5);
                    void AddProv(string? id, int consec)
                    {
                        var v = ProvValido(id);
                        if (v != null) provs.Add((v, consec));
                    }
                    AddProv(dto.prov1, 1);
                    AddProv(dto.prov2, 2);
                    AddProv(dto.prov3, 3);
                    AddProv(dto.prov4, 4);
                    AddProv(dto.prov5, 5);

                    if (provs.Count > 0)
                    {
                        const string sqlInsertProv = @"
INSERT INTO dbo.tblDetRequisicionesProv
    (reqdIdClave, reqdIdSoc, reqidposNo, reqdProvId, reqdAuth, reqdConsec)
VALUES
    (@reqdIdClave, @reqdIdSoc, @reqidposNo, @reqdProvId, @reqdAuth, @reqdConsec);";
                        foreach (var (provId, consec) in provs)
                        {
                            if (authorize == 1)
                            {
                                await using var cmdIns = new SqlCommand(sqlInsertProv, cn, tx);
                                cmdIns.Parameters.Add("@reqdIdClave", SqlDbType.NVarChar, 15).Value = reqdIdClave;
                                cmdIns.Parameters.Add("@reqdIdSoc", SqlDbType.NVarChar, 4).Value = reqdIdSoc;
                                cmdIns.Parameters.Add("@reqidposNo", SqlDbType.Int).Value = dto.reqidposNo;
                                cmdIns.Parameters.Add("@reqdProvId", SqlDbType.NVarChar, 10).Value = provId;
                                cmdIns.Parameters.Add("@reqdAuth", SqlDbType.Int).Value = authorize;
                                cmdIns.Parameters.Add("@reqdConsec", SqlDbType.Int).Value = consec;

                                await cmdIns.ExecuteNonQueryAsync(ct);
                                provsInsert++;
                            }
                        }
                    }

                    // -------- 3) VoBo si accion != "Guardar"
                    if (!isGuardar)
                    {
                        const string sqlVoBo = @"
UPDATE dbo.tblRequisiciones
   SET reqFecVoBo = CAST(GETDATE() AS smalldatetime),
       reqHrVoBo  = CAST(GETDATE() AS smalldatetime),
       reqFecVigencia = CAST( @vigencia AS DATETIME)
 WHERE reqIdClave = @reqIdClave;";
                        await using var cmdVoBo = new SqlCommand(sqlVoBo, cn, tx);
                        cmdVoBo.Parameters.Add("@reqIdClave", SqlDbType.NVarChar, 15).Value = reqIdClave;
                        cmdVoBo.Parameters.Add("@vigencia", SqlDbType.NVarChar, 15).Value = request.vigencia;
                        var rows = await cmdVoBo.ExecuteNonQueryAsync(ct);

                        if (rows == 0)
                        {
                            // Rollback total por política "todo o nada"
                            throw new InvalidOperationException(
                                $"No existe encabezado reqIdClave='{reqIdClave}' en tblRequisiciones para aplicar VoBo.");
                        }

                        vobos++;
                    }
                    else {
                        const string sqlVoBo = @"
UPDATE dbo.tblRequisiciones
   SET reqFecVigencia = CAST( @vigencia AS DATETIME)
 WHERE reqIdClave = @reqIdClave;";
                        await using var cmdVoBo = new SqlCommand(sqlVoBo, cn, tx);
                        cmdVoBo.Parameters.Add("@reqIdClave", SqlDbType.NVarChar, 15).Value = reqIdClave;
                        cmdVoBo.Parameters.Add("@vigencia", SqlDbType.NVarChar, 15).Value = request.vigencia;
                        var rows = await cmdVoBo.ExecuteNonQueryAsync(ct);

                        if (rows == 0)
                        {
                            // Rollback total por política "todo o nada"
                            throw new InvalidOperationException(
                                $"No existe encabezado reqIdClave='{reqIdClave}' en tblRequisiciones para aplicar VoBo.");
                        }

                      //  vobos++;

                    }
                }

                await tx.CommitAsync(ct);





                return new GuardarDetalleProvResultadoBatch
                {
                    Ok = true,
                    Procesados = modelos.Count,
                    DetallesUpserted = detallesUpsert,
                    ProveedoresInsertados = provsInsert,
                    VobosAplicados = vobos
                };
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }


        async Task<ReqUsuarioMail>  ObtenerUsuarioMail(string requisicion)
        {
            var lista = new ReqUsuarioMail();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Seleccionamos solo datos principales para la lista
            var cmd = new SqlCommand(@"
                        select r.reqIdClave,r.reqDescripcion,
                        r.usrIdClave, u.usrNombre + ' ' + u.usrApellidoP  + ' ' +  usrApellidoM as usrNombre,
                        u.usrCorreo
                        from dbo.tblRequisiciones r
                        inner join dbo.tblUsuario u on u.usrIdClave = r.usrIdClave 
                        where r.reqIdClave =   @requisicion", conn);


            cmd.Parameters.Add("@requisicion", SqlDbType.NVarChar, 10).Value = requisicion;

            //cmd.Parameters.AddWithValue("@requisicion", requisicion);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return null;


            while (await reader.ReadAsync())
            {
                 lista = new ReqUsuarioMail
                {
                   reqIdClave = reader["reqIdClave"].ToString()!,
                   reqDescripcion = reader["reqDescripcion"].ToString()!,
                   usrIdClave = reader["usrIdClave"].ToString()!,
                   usrNombre = reader["usrNombre"].ToString()!,
                   usrCorreo = reader["usrCorreo"].ToString()!,
 
                };
            }
            if (lista == null)
                return null;
            return lista;

        }


        async Task<string> ObtenerMailProv(string idprov)
        {
            string prov = "" ;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Seleccionamos solo datos principales para la lista
            var cmd = new SqlCommand(@"select provCorreo from   tblProveedores where provIdProv = @idprov", conn);

            cmd.Parameters.AddWithValue("@idprov", idprov);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return null;


            while (await reader.ReadAsync())
            {
                prov = reader["provCorreo"].ToString()!;
            }
            if (prov == null)
                return null;
            return prov;

        }


        public static List<AutProvPosicionList> TransformarPorProveedor(List<AutProvReqCerradaDetalleProvDto> articulos)
        {
            var resultado = new Dictionary<string, AutProvPosicionList>();

            foreach (var articulo in articulos)
            {
                var proveedores = new[] { articulo.prov1, articulo.prov2, articulo.prov3, articulo.prov4, articulo.prov5 };
                if (articulo.authorize == 1)
                {
                    foreach (var proveedor in proveedores)
                    {
                        if (!string.IsNullOrWhiteSpace(proveedor) && proveedor != "000000")
                        {
                            if (!resultado.ContainsKey(proveedor))
                            {
                                resultado[proveedor] = new AutProvPosicionList
                                {
                                    reqIdClave = articulo.reqIdClave,
                                    reqdIdSoc = articulo.reqdIdSoc,
                                    reqdIdClave = articulo.reqdIdClave,
                                    prov = proveedor,
                                    Modelos = new List<AutProvRequisicionNot>()
                                };
                            }

                            resultado[proveedor].Modelos.Add(new AutProvRequisicionNot
                            {
                                reqidposNo = articulo.reqidposNo,
                                reqdpMatNo = articulo.reqdpMatNo,
                                reqdpMatDes = articulo.reqdpMatDes,
                                reqdCantidad = articulo.reqdCantidad,
                                reqdUnidadMed = articulo.reqdUnidadMed,
                                reqdFecEntrega = articulo.reqdFecEntrega,
                                reqdCiudad = articulo.reqdCiudad,
                                reqdMunicipio = articulo.reqdMunicipio
                            });
                        }
                    }
                }
            }

            return resultado.Values.ToList();
        }

        public async Task<int> CancelarAsync(int usrIdSoc, string reqIdClave, CancellationToken ct = default)
        {
            const string sql = @"
                    UPDATE dbo.tblRequisiciones
                       SET reqFecCanc = CAST(GETDATE() AS smalldatetime),
                           reqFecFin  = NULL,
                           reqHrFin   = NULL,
                          ,reqFecVoBo   = NULL,
                          ,reqHrVoBo   = NULL
                     WHERE reqIdClave = @reqIdClave
                       AND usrIdSoc   = @usrIdSoc;";

            await using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add("@reqIdClave", SqlDbType.NVarChar, 15).Value = reqIdClave;
            cmd.Parameters.Add("@usrIdSoc", SqlDbType.Int).Value = usrIdSoc;

            return await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<IReadOnlyList<RequisicionTimelineDto>> GetTimelineAsync(
     string usrIdSoc,
     IEnumerable<string> estados,
     CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(usrIdSoc))
                throw new ArgumentException("usrIdSoc es requerido", nameof(usrIdSoc));

            // Normaliza: soporta CSV y múltiples parámetros
            var estadosNorm = new List<string>();
            foreach (var e in estados ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(e)) continue;

                if (e.Contains(',', StringComparison.Ordinal))
                {
                    estadosNorm.AddRange(
                        e.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    );
                }
                else
                {
                    estadosNorm.Add(e.Trim());
                }
            }
            estadosNorm = estadosNorm.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

            if (estadosNorm.Count == 0)
                throw new ArgumentException("Debe indicar al menos un estado", nameof(estados));

            // CSV seguro para STRING_SPLIT
            var estadosCsv = string.Join(",", estadosNorm);

            const string SQL = @"
SELECT
    reqIdClave,
    usrIdSoc,
    reqDescripcion,
    reqFecCreacion,
    reqFecFin,
    reqFecVoBo,
    reqNotFecProvGan,
    reqFecCanc,
    EstadoActual,
    UltimaFechaCerrada,
    DiasDesdeUltimaCerrada,
    Dur_Creacion_Captura_d,
    Dur_Captura_VoBo_d,
    Dur_VoBo_Notif_d
FROM dbo.vw_RequisicionesTimeline
WHERE usrIdSoc = @usrIdSoc
  AND EstadoActual IN (SELECT value FROM STRING_SPLIT(@estadosCsv, ','))
ORDER BY UltimaFechaCerrada DESC, reqIdClave;";

            var list = new List<RequisicionTimelineDto>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = new SqlCommand(SQL, conn)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.Add(new SqlParameter("@usrIdSoc", SqlDbType.NVarChar, 50) { Value = usrIdSoc });
            cmd.Parameters.Add(new SqlParameter("@estadosCsv", SqlDbType.NVarChar, -1) { Value = estadosCsv });

            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await rdr.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                DateTime? _d(string col) => rdr[col] == DBNull.Value ? (DateTime?)null : (DateTime)rdr[col];
                int? _i(string col) => rdr[col] == DBNull.Value ? (int?)null : (int)rdr[col];

                list.Add(new RequisicionTimelineDto
                {
                    reqIdClave = rdr["reqIdClave"].ToString()!,
                    usrIdSoc = rdr["usrIdSoc"].ToString()!,
                    reqDescripcion = rdr["reqDescripcion"] == DBNull.Value ? null : rdr["reqDescripcion"].ToString(),

                    reqFecCreacion = _d("reqFecCreacion"),
                    reqFecFin = _d("reqFecFin"),
                    reqFecVoBo = _d("reqFecVoBo"),
                    reqNotFecProvGan = _d("reqNotFecProvGan"),
                    reqFecCanc = _d("reqFecCanc"),

                    EstadoActual = rdr["EstadoActual"].ToString()!,
                    UltimaFechaCerrada = (DateTime)rdr["UltimaFechaCerrada"],
                    DiasDesdeUltimaCerrada = (int)rdr["DiasDesdeUltimaCerrada"],

                    Dur_Creacion_Captura_d = _i("Dur_Creacion_Captura_d"),
                    Dur_Captura_VoBo_d = _i("Dur_Captura_VoBo_d"),
                    Dur_VoBo_Notif_d = _i("Dur_VoBo_Notif_d"),
                });
            }

            return list;
        }




        public async Task<IReadOnlyList<RequisicionPendienteDto>> GetPendientesAsync(
    string usuario,
    string usrIdSoc,
    string idRol = "R06",
    CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(usuario);
            ArgumentException.ThrowIfNullOrWhiteSpace(usrIdSoc);
            ArgumentException.ThrowIfNullOrWhiteSpace(idRol);


            string SqlPendientesMinNivel = @"
SELECT DISTINCT
    r.reqIdClave,
    r.usrIdSoc,
    r.usrIdClave       AS SolicitanteId,
    r.reqDescripcion,
    fTop.reqLevelCode  AS NivelPendiente,
    fTop.reqCreado     AS DesdeUtc
FROM dbo.tblRequisiciones AS r
CROSS APPLY (
    SELECT TOP (1) f.reqLevelCode, f.reqCreado
    FROM dbo.tblFlujoAut AS f
    WHERE f.reqIdClave = r.reqIdClave
      AND f.reqEstado  = 'PENDIENTE'
    ORDER BY TRY_CAST(f.reqLevelCode AS INT) ASC, f.reqCreado ASC
) AS fTop
WHERE
    r.usrIdSoc = @usrIdSoc
    AND (r.reqFecCanc IS NULL) -- omite canceladas; elimina esta línea si quieres incluirlas
    AND EXISTS (
        SELECT 1
        FROM dbo.tblUsrRolPer AS rp
        WHERE rp.usrIdClave = @usuario
          AND rp.usrIdRol   = @idRol
    )
    AND EXISTS (
        SELECT 1
        FROM dbo.tblDetRequisiciones AS d
        JOIN dbo.tblUsrImputacion   AS ui
              ON ui.usrIdImp   = d.reqdIdImp
             AND ui.usrIdClave = @usuario
             AND ui.usrIdRol   = @idRol
             AND LTRIM(RTRIM(ui.usrNivel)) = fTop.reqLevelCode
        WHERE d.reqdIdClave = r.reqIdClave
          AND d.reqdIdSoc   = r.usrIdSoc
          AND d.reqdIdImp  IS NOT NULL
    )
ORDER BY fTop.reqCreado ASC, r.reqIdClave ASC;";


            var list = new List<RequisicionPendienteDto>(64);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = new SqlCommand(SqlPendientesMinNivel, conn)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 60
            };

            cmd.Parameters.Add(new SqlParameter("@usuario", SqlDbType.NVarChar, 10) { Value = usuario });
            cmd.Parameters.Add(new SqlParameter("@usrIdSoc", SqlDbType.NVarChar, 4) { Value = usrIdSoc });
            cmd.Parameters.Add(new SqlParameter("@idRol", SqlDbType.NVarChar, 4) { Value = idRol });

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken)
                                              .ConfigureAwait(false);

            var ordReqIdClave = reader.GetOrdinal("reqIdClave");
            var ordUsrIdSoc = reader.GetOrdinal("usrIdSoc");
            var ordSolicitante = reader.GetOrdinal("SolicitanteId");
            var ordDesc = reader.GetOrdinal("reqDescripcion");
            var ordNivel = reader.GetOrdinal("NivelPendiente");
            var ordDesdeUtc = reader.GetOrdinal("DesdeUtc");

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var dto = new RequisicionPendienteDto
                {
                    ReqIdClave = reader.GetString(ordReqIdClave),
                    UsrIdSoc = reader.GetString(ordUsrIdSoc),
                    SolicitanteId = reader.GetString(ordSolicitante),
                    ReqDescripcion = reader.IsDBNull(ordDesc) ? null : reader.GetString(ordDesc),
                    NivelPendiente = reader.GetString(ordNivel),
                    DesdeUtc = reader.GetDateTime(ordDesdeUtc), // datetime2(0) en UTC por DF
                };
                list.Add(dto);
            }

            return list;
        }

    }
}