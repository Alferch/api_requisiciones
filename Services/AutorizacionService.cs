using Microsoft.Data.SqlClient;
using System.Data;
using static RequisicionesApi.Models.Autorizacion.AutorizacionDtos;

namespace RequisicionesApi.Services
{
 //   public class AutorizacionService
 //   {

        public interface IAutorizacionService
        {
            Task GenerarFlujoAsync(GenerarFlujoRequest req, CancellationToken ct);
            Task AutorizarAsync(AccionRequest req, CancellationToken ct);
            Task<IReadOnlyList<PendienteDto>> GetPendientesAsync(string usuario, CancellationToken ct);
            Task<IReadOnlyList<TimelineDto>> GetTimelineAsync(string reqIdClave, CancellationToken ct);
            Task<IReadOnlyList<EstadoNivelDto>> GetEstadoActualAsync(string reqIdClave, CancellationToken ct);

        Task<DataTable> GetFlujoAutPendienteAsync(string reqIdClave);

        Task<string> GetProveedorGanador(string reqIdClave);

        Task<string> GetProveedorCorreoAsync(string provIdProv);
        }

        public sealed class AutorizacionService : IAutorizacionService
        {
        private readonly string _connectionString;
//        private readonly string _cs;
       //     public AutorizacionService(IConfiguration cfg)
       //         => _cs = cfg.GetConnectionString("DbProveedores")!;


        public AutorizacionService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }



        public async Task GenerarFlujoAsync(GenerarFlujoRequest req, CancellationToken ct)
            {
                await using var cn = new SqlConnection(_connectionString);
                await cn.OpenAsync(ct);
                await using var cmd = new SqlCommand("dbo.sp_GenerarFlujoAut", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@reqIdClave", req.ReqIdClave);
                cmd.Parameters.AddWithValue("@usrIdSoc", req.UsrIdSoc);
                cmd.Parameters.AddWithValue("@reqpprov", "0000010200");
            await cmd.ExecuteNonQueryAsync(ct);
            }


        public async Task AutorizarAsync(AccionRequest req, CancellationToken ct)
        {
            await using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync(ct);
            await using var cmd = new SqlCommand("dbo.sp_AutorizarRequisicion", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@reqIdClave", req.ReqIdClave);
            cmd.Parameters.AddWithValue("@usuario", req.Usuario);
            cmd.Parameters.AddWithValue("@accion", req.Accion);
            cmd.Parameters.AddWithValue("@comentario", (object?)req.Comentario ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);



        }
        public async Task PendienteAutorizarAsync(AccionRequest req, CancellationToken ct)
            {
                await using var cn = new SqlConnection(_connectionString);
                await cn.OpenAsync(ct);
                await using var cmd = new SqlCommand("dbo.sp_GetFlujoAutPendienteMail", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@reqIdClave", req.ReqIdClave);
                //cmd.Parameters.AddWithValue("@usuario", req.Usuario);
                //cmd.Parameters.AddWithValue("@accion", req.Accion);
                //cmd.Parameters.AddWithValue("@comentario", (object?)req.Comentario ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(ct);

           

        }


        public async Task<string> GetProveedorCorreoAsync(string provIdProv)
        {
            string correoProveedor = string.Empty;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetProveedorCorreo", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parámetro de entrada
                    cmd.Parameters.Add(new SqlParameter("@provIdProv", SqlDbType.NVarChar, 20) { Value = provIdProv });

                    // Parámetro de salida
                    SqlParameter outputParam = new SqlParameter("@CorreoProveedor", SqlDbType.NVarChar, 100)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    correoProveedor = outputParam.Value.ToString();
                }
            }

            return correoProveedor;
        }


        public async Task<string> GetProveedorGanador(string reqIdClave)
        {
            string proveedorGanador = string.Empty;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetProveedorGanador", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parámetro de entrada
                    cmd.Parameters.Add(new SqlParameter("@reqIdClave", SqlDbType.NVarChar, 20) { Value = reqIdClave });

                    // Parámetro de salida
                    SqlParameter outputParam = new SqlParameter("@ProveedorGanador", SqlDbType.NVarChar, 100)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    proveedorGanador = outputParam.Value.ToString();
                }
            }

            return proveedorGanador;
        }
        

        public async Task<DataTable> GetFlujoAutPendienteAsync(string reqIdClave)
        {
            DataTable resultTable = new DataTable();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetFlujoAutPendiente", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@reqIdClave", SqlDbType.NVarChar, 20) { Value = reqIdClave });

                    await conn.OpenAsync();

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(resultTable);
                    }
                }
            }

            return resultTable;
        }


        public async Task<IReadOnlyList<PendienteDto>> GetPendientesAsync(string usuario, CancellationToken ct)
            {
                var list = new List<PendienteDto>();
                await using var cn = new SqlConnection(_connectionString);
                await cn.OpenAsync(ct);
                await using var cmd = new SqlCommand("dbo.sp_GetPendientesParaUsuario", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@usuario", usuario);
                await using var rd = await cmd.ExecuteReaderAsync(ct);
                while (await rd.ReadAsync(ct))
                {
                    list.Add(new PendienteDto(
                        rd.GetString(0),
                        rd.GetString(1),
                        rd.GetDateTime(2)
                    ));
                }
                return list;
            }

            public async Task<IReadOnlyList<TimelineDto>> GetTimelineAsync(string reqIdClave, CancellationToken ct)
            {
                var list = new List<TimelineDto>();
                await using var cn = new SqlConnection(_connectionString);
                await cn.OpenAsync(ct);
                await using var cmd = new SqlCommand("dbo.sp_GetTimelineByReq", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@reqIdClave", reqIdClave);
                await using var rd = await cmd.ExecuteReaderAsync(ct);
                while (await rd.ReadAsync(ct))
                {
                    list.Add(new TimelineDto(
                        rd.GetString(0),         // level
                        rd.GetString(1),         // evento
                        rd.GetDateTime(2),       // eventoEn
                        rd.IsDBNull(3) ? null : rd.GetString(3),
                        rd.IsDBNull(4) ? null : rd.GetString(4)
                    ));
                }
                return list;
            }

            public async Task<IReadOnlyList<EstadoNivelDto>> GetEstadoActualAsync(string reqIdClave, CancellationToken ct)
            {
                var list = new List<EstadoNivelDto>();
                await using var cn = new SqlConnection(_connectionString);
                await cn.OpenAsync(ct);
                await using var cmd = new SqlCommand("dbo.sp_GetEstadoActual", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@reqIdClave", reqIdClave);
                await using var rd = await cmd.ExecuteReaderAsync(ct);
                while (await rd.ReadAsync(ct))
                {
                    list.Add(new EstadoNivelDto(
                        rd.GetString(0),
                        rd.GetString(1),
                        rd.GetDateTime(2),
                        rd.IsDBNull(3) ? (DateTime?)null : rd.GetDateTime(3),
                        rd.IsDBNull(4) ? null : rd.GetString(4),
                        rd.IsDBNull(5) ? null : rd.GetString(5)
                    ));
                }
                return list;
            }
        }

 //   }
}
