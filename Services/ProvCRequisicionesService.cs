// Auto-generated file with updated naming
using Microsoft.Data.SqlClient;
using RequisicionesApi.Models;
using System.Data;
using System.Text;
using System.Transactions;


namespace RequisicionesApi.Services
{
    public class ProvCRequisicionesService
    {
        private readonly string _connectionString;

        public ProvCRequisicionesService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<ProvCRequisicion> ObtenerRequisicionesPendientes(string correo)
        {
            var lista = new List<ProvCRequisicion>();
            string sociedad = "";
            string proveedor = "";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var query = @" SELECT provIdSoc   ,provIdProv  FROM dbo.tblProveedores  where provCorreo = @Correo";
                using (var cmd1 = new SqlCommand(query, conn))
                {
                    cmd1.Parameters.AddWithValue("@correo", correo);
                    using (var reader1 = cmd1.ExecuteReader())
                    {
                        while (reader1.Read())
                        {
                            sociedad = reader1["provIdSoc"].ToString();
                            proveedor = reader1["provIdProv"].ToString();

                        }
                    }
                }
         



            query = @" SELECT
    r.reqIdClave AS IdRequisicion,
    r.reqFecMod AS fechaAviso,
    r.reqFecCreacion AS FechaCreacion,
    r.reqDescripcion AS Descripcion,
    COUNT(dr.reqidposNo) AS Productos,
    rp.reqdProvId
FROM [dbo].[tblRequisiciones] r
INNER JOIN [dbo].[tblDetRequisiciones] dr 
    ON dr.reqdIdClave = r.reqIdClave 
    AND dr.reqdIdSoc = r.usrIdSoc
INNER JOIN [dbo].[tblDetRequisicionesProv] rp 
    ON rp.reqdIdClave = dr.reqdIdClave 
    AND rp.reqdIdSoc = dr.reqdIdSoc
    AND rp.reqidposNo = dr.reqidposNo
WHERE rp.reqdProvId = @proveedorId 
   AND r.reqNotFecProvGan IS NULL
   AND r.reqFecCanc IS NULL
GROUP BY 
    r.reqIdClave, 
    r.reqFecMod, 
    r.reqFecCreacion,
    r.reqDescripcion, 
    rp.reqdProvId
ORDER BY r.reqFecCreacion DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@proveedorId", proveedor);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ProvCRequisicion
                            {
                                IdRequisicion = reader["IdRequisicion"].ToString(),
                                FechaAviso = reader["fechaAviso"].ToString(),
                                FechaCreacion = reader["FechaCreacion"].ToString(),
                                Descripcion = reader["Descripcion"].ToString(),
                                Productos = (int)reader["Productos"],
                                ProveedorId = reader["reqdProvId"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public IEnumerable<ProvCRequisicionDetalle> ObtenerDetalleRequisicion(string idRequisicion, string proveedorId)
        {
            var lista = new List<ProvCRequisicionDetalle>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var query = @"
SELECT
    r.reqIdClave AS IdRequisicion,
    r.reqFecMod AS fechaAviso,
    r.reqDescripcion AS Descripcion,
    dr.reqidposNo,
    dr.reqdpMatNo,
    dr.reqdpMatDes,
    dr.reqdUnidadMed,
    dr.reqdFecEntrega,
    dr.reqdCiudad,
    dr.reqdMunicipio,
    rp.reqdProvId,
    ar.reqAnexo, dr.reqdCantidad, dr.reqdIdSoc
FROM [dbo].[tblRequisiciones] r
INNER JOIN [dbo].[tblDetRequisiciones] dr 
    ON dr.reqdIdClave = r.reqIdClave 
    AND dr.reqdIdSoc = r.usrIdSoc
INNER JOIN [dbo].[tblDetRequisicionesProv] rp 
    ON rp.reqdIdClave = dr.reqdIdClave 
    AND rp.reqdIdSoc = dr.reqdIdSoc
    AND rp.reqidposNo = dr.reqidposNo
LEFT JOIN [dbo].[tblAneRequisiciones] ar 
    ON ar.reqAIdClave = dr.reqdIdClave 
    AND ar.reqAIdSoc = dr.reqdIdSoc 
    AND ar.reqAidposNo = dr.reqidposNo
WHERE rp.reqdProvId = @proveedorId 
  AND rp.reqdIdClave = @idRequisicion";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@proveedorId", proveedorId);
                    cmd.Parameters.AddWithValue("@idRequisicion", idRequisicion);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ProvCRequisicionDetalle
                            {
                                IdRequisicion = reader["IdRequisicion"].ToString(),
                                Posicion = (int)reader["reqidposNo"],
                                MaterialNo = reader["reqdpMatNo"].ToString(),
                                MaterialDesc = reader["reqdpMatDes"].ToString(),
                                UnidadMed = reader["reqdUnidadMed"].ToString(),
                                FechaEntrega = reader["reqdFecEntrega"].ToString(),
                                Ciudad = reader["reqdCiudad"].ToString(),
                                Municipio = reader["reqdMunicipio"].ToString(),
                                ProveedorId = reader["reqdProvId"].ToString(),
                                Anexo = reader["reqAnexo"].ToString(),
                                Cantidad = (int)reader["reqdCantidad"],
                                sociedad = reader["reqdIdSoc"].ToString(),
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public bool InsertarRequisicionesProveedor(List<ProvCInsertRequisicionRequest> requests)
        {
            if (requests == null || requests.Count == 0) return true; // nada que hacer

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1) TVP de llaves a borrar
                var keys = new DataTable();
                keys.Columns.Add("reqpIdClave", typeof(string));
                keys.Columns.Add("reqppProvId", typeof(string));
                keys.Columns.Add("reqppIdSoc", typeof(string));

                // Evitar llaves repetidas
                var seen = new HashSet<(string, string, string)>();
                foreach (var r in requests)
                {
                    var k = (r.IdClave, r.ProvId, r.IdSoc);
                    if (seen.Add(k))
                        keys.Rows.Add(r.IdClave, r.ProvId, r.IdSoc);
                }

                // 2) DELETE por join con el TVP (corregido: usar @Keys directo)
                const string deleteSql = @"
DELETE T
FROM [dbo].[tblprovRequisiciones] AS T
INNER JOIN @Keys AS K
  ON K.reqpIdClave = T.reqpIdClave
 AND K.reqppProvId = T.reqppProvId
 AND K.reqppIdSoc  = T.reqppIdSoc;";

                using (var delCmd = new SqlCommand(deleteSql, conn, tx))
                {
                    var p = delCmd.Parameters.AddWithValue("@Keys", keys);
                    p.SqlDbType = SqlDbType.Structured;
                    p.TypeName = "dbo.TVP_RequisicionesKeys";
                    delCmd.ExecuteNonQuery();
                }

                // 3) INSERT fila a fila (puedes cambiar por SqlBulkCopy si quieres máximo rendimiento)
                const string insertSql = @"
INSERT INTO [dbo].[tblprovRequisiciones]
(
    [reqpIdClave],
    [reqppPosNo],
    [reqppIdSoc],
    [reqppProvId],
    [reqppPrecUnit],
    [reqppMoneda],
    [reqppUnidadMed],
    [reqppCargoExt],
    [reqppFecEntrega],
    [reqppCondPago],
    [reqppVendedor],
    [reqppCantidad]
)
VALUES
(
    @reqpIdClave,
    @reqppPosNo,
    @reqppIdSoc,
    @reqppProvId,
    @reqppPrecUnit,
    @reqppMoneda,
    @reqppUnidadMed,
    @reqppCargoExt,
    @reqppFecEntrega,
    @reqppCondPago,
    @reqppVendedor,
    @reqppCantidad

);";

                using var insertCmd = new SqlCommand(insertSql, conn, tx);
                insertCmd.Parameters.Add("@reqpIdClave", SqlDbType.NVarChar, 15);
                insertCmd.Parameters.Add("@reqppPosNo", SqlDbType.Int);
                insertCmd.Parameters.Add("@reqppIdSoc", SqlDbType.NVarChar, 4);
                insertCmd.Parameters.Add("@reqppProvId", SqlDbType.NVarChar, 10);

                var pPrec = insertCmd.Parameters.Add("@reqppPrecUnit", SqlDbType.Decimal);
                pPrec.Precision = 18; pPrec.Scale = 4;

                insertCmd.Parameters.Add("@reqppMoneda", SqlDbType.NVarChar, 3);
                insertCmd.Parameters.Add("@reqppUnidadMed", SqlDbType.NVarChar, 6);

                var pCargo = insertCmd.Parameters.Add("@reqppCargoExt", SqlDbType.Decimal);
                pCargo.Precision = 18; pCargo.Scale = 4;

                insertCmd.Parameters.Add("@reqppFecEntrega", SqlDbType.DateTime2);
                insertCmd.Parameters.Add("@reqppCondPago", SqlDbType.NVarChar, 50);
                insertCmd.Parameters.Add("@reqppVendedor", SqlDbType.NVarChar, 100);
                insertCmd.Parameters.Add("@reqppCantidad", SqlDbType.Int);

                foreach (var r in requests)
                {
                    insertCmd.Parameters["@reqpIdClave"].Value = r.IdClave;
                    insertCmd.Parameters["@reqppPosNo"].Value = r.PosNo;
                    insertCmd.Parameters["@reqppIdSoc"].Value = r.IdSoc;
                    insertCmd.Parameters["@reqppProvId"].Value = r.ProvId;
                    insertCmd.Parameters["@reqppPrecUnit"].Value = r.PrecioUnit;
                    insertCmd.Parameters["@reqppMoneda"].Value = r.Moneda;
                    insertCmd.Parameters["@reqppUnidadMed"].Value = r.UnidadMed;

                    insertCmd.Parameters["@reqppCargoExt"].Value = r.CargoExtra;

                    insertCmd.Parameters["@reqppFecEntrega"].Value = r.FechaEntrega;
 

                    insertCmd.Parameters["@reqppCondPago"].Value =
                        string.IsNullOrWhiteSpace(r.CondPago) ? DBNull.Value : r.CondPago;

                    insertCmd.Parameters["@reqppVendedor"].Value =
                        string.IsNullOrWhiteSpace(r.Vendedor) ? DBNull.Value : r.Vendedor;

                    insertCmd.Parameters["@reqppCantidad"].Value = r.Cantidad;

                    insertCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { /* ignore rollback errors */ }

                string BuildContext(Exception e)
                {
                    if (e is SqlException sql)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("SQL Error:");
                        sb.AppendLine($"  Number: {sql.Number}");
                        sb.AppendLine($"  State: {sql.State}");
                        sb.AppendLine($"  Class: {sql.Class}");
                        sb.AppendLine($"  Procedure: {sql.Procedure}");
                        sb.AppendLine($"  LineNumber: {sql.LineNumber}");
                        if (sql.Errors is { Count: > 0 })
                        {
                            foreach (SqlError er in sql.Errors)
                                sb.AppendLine($"  -> {er.Message} (Num={er.Number}, Line={er.LineNumber})");
                        }
                        return sb.ToString();
                    }
                    return e.Message;
                }

                var context = $"Error InsertarRequisicionesProveedor. Lote: {requests?.Count ?? 0}. {BuildContext(ex)}";
                throw new InvalidOperationException(context, ex);
            }

        }
        public bool InsertarRequisicionesProveedor1x1(List<ProvCInsertRequisicionRequest> requests)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var request in requests)
                        {
                            var query = @"
INSERT INTO [dbo].[tblprovRequisiciones]
           ([reqpIdClave]
           ,[reqppPosNo]
           ,[reqppIdSoc]
           ,[reqppProvId]
           ,[reqppPrecUnit]
           ,[reqppMoneda]
           ,[reqppUnidadMed]
           ,[reqppCargoExt]
           ,[reqppFecEntrega]
           ,[reqppCondPago]
           ,[reqppVendedor],
           ,[reqppCantidad])
     VALUES
           (@reqpIdClave
           ,@reqppPosNo
           ,@reqppIdSoc
           ,@reqppProvId
           ,@reqppPrecUnit
           ,@reqppMoneda
           ,@reqppUnidadMed
           ,@reqppCargoExt
           ,@reqppFecEntrega
           ,@reqppCondPago
           ,@reqppVendedor,@reqppCantidad)";

                            using (var cmd = new SqlCommand(query, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@reqpIdClave", request.IdClave);
                                cmd.Parameters.AddWithValue("@reqppPosNo", request.PosNo);
                                cmd.Parameters.AddWithValue("@reqppIdSoc", request.IdSoc);
                                cmd.Parameters.AddWithValue("@reqppProvId", request.ProvId);
                                cmd.Parameters.AddWithValue("@reqppPrecUnit", request.PrecioUnit);
                                cmd.Parameters.AddWithValue("@reqppMoneda", request.Moneda);
                                cmd.Parameters.AddWithValue("@reqppUnidadMed", request.UnidadMed);
                                cmd.Parameters.AddWithValue("@reqppCargoExt", request.CargoExtra);
                                cmd.Parameters.AddWithValue("@reqppFecEntrega", request.FechaEntrega);
                                cmd.Parameters.AddWithValue("@reqppCondPago", request.CondPago);
                                cmd.Parameters.AddWithValue("@reqppVendedor", request.Vendedor);
                                cmd.Parameters.AddWithValue("@reqppCantidad", request.Cantidad);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }

                    catch (Exception ex)
                    {
                        try { transaction.Rollback(); } catch { /* ignore rollback errors */ }

                        string BuildContext(Exception e)
                        {
                            if (e is SqlException sql)
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine("SQL Error:");
                                sb.AppendLine($"  Number: {sql.Number}");
                                sb.AppendLine($"  State: {sql.State}");
                                sb.AppendLine($"  Class: {sql.Class}");
                                sb.AppendLine($"  Procedure: {sql.Procedure}");
                                sb.AppendLine($"  LineNumber: {sql.LineNumber}");
                                if (sql.Errors is { Count: > 0 })
                                {
                                    foreach (SqlError er in sql.Errors)
                                        sb.AppendLine($"  -> {er.Message} (Num={er.Number}, Line={er.LineNumber})");
                                }
                                return sb.ToString();
                            }
                            return e.Message;
                        }

                        var context = $"Error InsertarRequisicionesProveedor. Lote: {requests?.Count ?? 0}. {BuildContext(ex)}";
                        throw new InvalidOperationException(context, ex);
                    }

                }
            }
        }



        //        public bool InsertarRequisicionProveedor(ProvCInsertRequisicionRequest request)
        //        {
        //            using (var conn = new SqlConnection(_connectionString))
        //            {
        //                conn.Open();
        //                var query = @"
        //INSERT INTO [dbo].[tblprovRequisiciones]
        //           ([reqpIdClave]
        //           ,[reqppPosNo]
        //           ,[reqppIdSoc]
        //           ,[reqppProvId]
        //           ,[reqppPrecUnit]
        //           ,[reqppMoneda]
        //           ,[reqppUnidadMed]
        //           ,[reqppCargoExt]
        //           ,[reqppFecEntrega]
        //           ,[reqppCondPago]
        //           ,[reqppVendedor])
        //     VALUES
        //           (@reqpIdClave
        //           ,@reqppPosNo
        //           ,@reqppIdSoc
        //           ,@reqppProvId
        //           ,@reqppPrecUnit
        //           ,@reqppMoneda
        //           ,@reqppUnidadMed
        //           ,@reqppCargoExt
        //           ,@reqppFecEntrega
        //           ,@reqppCondPago
        //           ,@reqppVendedor)";
        //                using (var cmd = new SqlCommand(query, conn))
        //                {
        //                    cmd.Parameters.AddWithValue("@reqpIdClave", request.IdClave);
        //                    cmd.Parameters.AddWithValue("@reqppPosNo", request.PosNo);
        //                    cmd.Parameters.AddWithValue("@reqppIdSoc", request.IdSoc);
        //                    cmd.Parameters.AddWithValue("@reqppProvId", request.ProvId);
        //                    cmd.Parameters.AddWithValue("@reqppPrecUnit", request.PrecioUnit);
        //                    cmd.Parameters.AddWithValue("@reqppMoneda", request.Moneda);
        //                    cmd.Parameters.AddWithValue("@reqppUnidadMed", request.UnidadMed);
        //                    cmd.Parameters.AddWithValue("@reqppCargoExt", request.CargoExtra);
        //                    cmd.Parameters.AddWithValue("@reqppFecEntrega", request.FechaEntrega);
        //                    cmd.Parameters.AddWithValue("@reqppCondPago", request.CondPago);
        //                    cmd.Parameters.AddWithValue("@reqppVendedor", request.Vendedor);
        //                    return cmd.ExecuteNonQuery() > 0;
        //                }
        //            }
        //        }
    }
}