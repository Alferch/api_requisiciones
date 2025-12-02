using RequisicionesApi.Interfaces;
using RequisicionesApi.Models.Condiciones;
using System.Data;
using Microsoft.Data.SqlClient;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace RequisicionesApi.Services
{
    public class CondAdicEncabezadoService : ICondAdicEncabezadoService
    {


        private readonly string _connectionString;

        public CondAdicEncabezadoService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }



        public async Task InsertAsync(List<CondAdicEncabezado> entidades)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO tblCondAdicEncabezado 
                         (reqIdClave, IdCondicion, provIdProv, Proceso, Posicion, Importe)
                         VALUES (@reqIdClave, @IdCondicion, @provIdProv, @Proceso, @Posicion, @Importe)";

                await conn.OpenAsync();

                foreach (var entidad in entidades)
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@reqIdClave", entidad.ReqIdClave);
                        cmd.Parameters.AddWithValue("@IdCondicion", entidad.IdCondicion);
                        cmd.Parameters.AddWithValue("@provIdProv", entidad.ProvIdProv);
                        cmd.Parameters.AddWithValue("@Proceso", entidad.Proceso);
                        cmd.Parameters.AddWithValue("@Posicion", entidad.Posicion);
                        cmd.Parameters.AddWithValue("@Importe", entidad.Importe);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        //public async Task InsertAsync(List<CondAdicEncabezado> entidad)
        //{
        //    using (SqlConnection conn = new SqlConnection(_connectionString))
        //    {
        //        string query = @"INSERT INTO tblCondAdicEncabezado 
        //                     (reqIdClave, IdCondicion, provIdProv, Proceso, Posicion, Importe)
        //                     VALUES (@reqIdClave, @IdCondicion, @provIdProv, @Proceso, @Posicion, @Importe)";
        //        SqlCommand cmd = new SqlCommand(query, conn);
        //        cmd.Parameters.AddWithValue("@reqIdClave", entidad.ReqIdClave);
        //        cmd.Parameters.AddWithValue("@IdCondicion", entidad.IdCondicion);
        //        cmd.Parameters.AddWithValue("@provIdProv", entidad.ProvIdProv);
        //        cmd.Parameters.AddWithValue("@Proceso", entidad.Proceso);
        //        cmd.Parameters.AddWithValue("@Posicion", entidad.Posicion);
        //        cmd.Parameters.AddWithValue("@Importe", entidad.Importe);

        //        await conn.OpenAsync();
        //        await cmd.ExecuteNonQueryAsync();
        //    }
        //}

        public async Task<CondAdicEncabezado> GetByIdAsync(string reqIdClave, string idCondicion)
        {
            CondAdicEncabezado entidad = null;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT reqIdClave, IdCondicion, provIdProv, Proceso, Posicion, Importe
                             FROM tblCondAdicEncabezado
                             WHERE reqIdClave = @reqIdClave AND IdCondicion = @IdCondicion";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@reqIdClave", reqIdClave);
                cmd.Parameters.AddWithValue("@IdCondicion", idCondicion);

                await conn.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        entidad = new CondAdicEncabezado
                        {
                            ReqIdClave = reader["reqIdClave"].ToString(),
                            IdCondicion = reader["IdCondicion"].ToString(),
                            ProvIdProv = reader["provIdProv"].ToString(),
                            Proceso = reader["Proceso"].ToString(),
                            Posicion = Convert.ToInt32(reader["Posicion"]),
                            Importe = Convert.ToDecimal(reader["Importe"])
                        };
                    }
                }
            }
            return entidad;
        }

        public async Task<IEnumerable<CondAdicEncabezado>> GetAllAsync()
        {
            var lista = new List<CondAdicEncabezado>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT reqIdClave, IdCondicion, provIdProv, Proceso, Posicion, Importe FROM tblCondAdicEncabezado";
                SqlCommand cmd = new SqlCommand(query, conn);
                await conn.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new CondAdicEncabezado
                        {
                            ReqIdClave = reader["reqIdClave"].ToString(),
                            IdCondicion = reader["IdCondicion"].ToString(),
                            ProvIdProv = reader["provIdProv"].ToString(),
                            Proceso = reader["Proceso"].ToString(),
                            Posicion = Convert.ToInt32(reader["Posicion"]),
                            Importe = Convert.ToDecimal(reader["Importe"])
                        });
                    }
                }
            }
            return lista;
        }



        public async Task<int> UpdateAsync(List<CondAdicEncabezado> entidades)
        {
            if (entidades == null || entidades.Count == 0)
                return 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (SqlTransaction tx = conn.BeginTransaction())
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE tblCondAdicEncabezado
              SET provIdProv = @provIdProv, Proceso = @Proceso, Posicion = @Posicion, Importe = @Importe
              WHERE reqIdClave = @reqIdClave AND IdCondicion = @IdCondicion", conn, tx))
                {
                    // Define parámetros una sola vez (reutilizados en el loop)
                    var pReqIdClave = cmd.Parameters.Add("@reqIdClave", SqlDbType.Int);
                    var pIdCondicion = cmd.Parameters.Add("@IdCondicion", SqlDbType.Int);
                    var pProvIdProv = cmd.Parameters.Add("@provIdProv", SqlDbType.Int);
                    var pProceso = cmd.Parameters.Add("@Proceso", SqlDbType.VarChar, 50);
                    var pPosicion = cmd.Parameters.Add("@Posicion", SqlDbType.Int);
                    var pImporte = cmd.Parameters.Add("@Importe", SqlDbType.Decimal);
                    pImporte.Precision = 18;
                    pImporte.Scale = 2;

                    int totalRows = 0;

                    try
                    {
                        foreach (var entidad in entidades)
                        {
                            pReqIdClave.Value = entidad.ReqIdClave;
                            pIdCondicion.Value = entidad.IdCondicion;
                            pProvIdProv.Value = entidad.ProvIdProv;
                            pProceso.Value = (object?)entidad.Proceso ?? DBNull.Value;
                            pPosicion.Value = entidad.Posicion;
                            pImporte.Value = entidad.Importe;

                            totalRows += await cmd.ExecuteNonQueryAsync();
                        }

                        tx.Commit();
                        return totalRows; // filas actualizadas en total
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        //public async Task UpdateAsync(List<CondAdicEncabezado> entidad)
        //{
        //    using (SqlConnection conn = new SqlConnection(_connectionString))
        //    {
        //        string query = @"UPDATE tblCondAdicEncabezado
        //                     SET provIdProv = @provIdProv, Proceso = @Proceso, Posicion = @Posicion, Importe = @Importe
        //                     WHERE reqIdClave = @reqIdClave AND IdCondicion = @IdCondicion";
        //        SqlCommand cmd = new SqlCommand(query, conn);
        //        cmd.Parameters.AddWithValue("@reqIdClave", entidad.ReqIdClave);
        //        cmd.Parameters.AddWithValue("@IdCondicion", entidad.IdCondicion);
        //        cmd.Parameters.AddWithValue("@provIdProv", entidad.ProvIdProv);
        //        cmd.Parameters.AddWithValue("@Proceso", entidad.Proceso);
        //        cmd.Parameters.AddWithValue("@Posicion", entidad.Posicion);
        //        cmd.Parameters.AddWithValue("@Importe", entidad.Importe);

        //        await conn.OpenAsync();
        //        await cmd.ExecuteNonQueryAsync();
        //    }
        //}

        public async Task DeleteAsync(string reqIdClave, string idCondicion)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"DELETE FROM tblCondAdicEncabezado WHERE reqIdClave = @reqIdClave AND IdCondicion = @IdCondicion";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@reqIdClave", reqIdClave);
                cmd.Parameters.AddWithValue("@IdCondicion", idCondicion);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }



        public async Task<IEnumerable<Condicion>> GetAllAsyncCondicion()
        {
            var lista = new List<Condicion>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT IdCondicion, Descripcion, Etiqueta, Porcentaje FROM tblCondiciones";
                SqlCommand cmd = new SqlCommand(query, conn);
                await conn.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new Condicion
                        {
                            IdCondicion = reader["IdCondicion"].ToString(),
                            Descripcion = reader["Descripcion"].ToString(),
                            Etiqueta = reader["Etiqueta"].ToString(),
                            Porcentaje = reader["Porcentaje"] == DBNull.Value ? null : Convert.ToDecimal(reader["Porcentaje"])
                        });
                    }
                }
            }
            return lista;
        }



    }
}
