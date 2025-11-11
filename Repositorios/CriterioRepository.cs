using Microsoft.Data.SqlClient;
using RequisicionesApi.Models;
using System.Data;

namespace RequisicionesApi.Repositorios
{
    public class CriterioRepository
    {


        private readonly IConfiguration _config;
        public CriterioRepository(IConfiguration config) => _config = config;
        private SqlConnection CreateConnection() => new(_config.GetConnectionString("DefaultConnection"));


        public async Task<int> CreateAsync(Criterio criterio)
        {
            using var conn = CreateConnection();
           // using var conn = SqlHelper.GetConnection(_config);
            using var cmd = new SqlCommand(@"INSERT INTO tblCriterio (criIdSoc, criNombre, criPonderacion, criDescripcion, criCampo)
                                         VALUES (@soc, @nom, @pond, @desc, @campo); SELECT SCOPE_IDENTITY();", conn);
            cmd.Parameters.AddWithValue("@soc", criterio.CriIdSoc);
            cmd.Parameters.AddWithValue("@nom", criterio.CriNombre);
            cmd.Parameters.AddWithValue("@pond", criterio.CriPonderacion);
            cmd.Parameters.AddWithValue("@desc", criterio.CriDescripcion);
            cmd.Parameters.AddWithValue("@campo", (object?)criterio.CriCampo ?? DBNull.Value);

            await conn.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<Criterio>> GetAllAsync()
        {
            var list = new List<Criterio>();
            using var conn = CreateConnection();
//            using var conn = SqlHelper.GetConnection(_config);
            using var cmd = new SqlCommand("SELECT * FROM tblCriterio", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Criterio
                {
                    CriId = reader.GetInt32(0),
                    CriIdSoc = reader.GetString(1),
                    CriNombre = reader.GetString(2),
                    CriPonderacion = reader.GetInt32(3),
                    CriDescripcion = reader.GetString(4),
                    CriCampo = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
            return list;
        }

        public async Task<List<Criterio>> GetBySocIdAsync(string criIdSoc)
        {
            var list = new List<Criterio>();
            using var conn = CreateConnection();
//            using var conn = SqlHelper.GetConnection(_config);
            using var cmd = new SqlCommand("SELECT * FROM tblCriterio WHERE criIdSoc = @soc", conn);
            cmd.Parameters.AddWithValue("@soc", criIdSoc);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Criterio
                {
                    CriId = reader.GetInt32(0),
                    CriIdSoc = reader.GetString(1),
                    CriNombre = reader.GetString(2),
                    CriPonderacion = reader.GetInt32(3),
                    CriDescripcion = reader.GetString(4),
                    CriCampo = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
            return list;
        }

        public async Task<List<Criterio>> GetByNombreAsync(string nombre)
        {
            var list = new List<Criterio>();
            using var conn = CreateConnection();
//            using var conn = SqlHelper.GetConnection(_config);
            using var cmd = new SqlCommand("SELECT * FROM tblCriterio WHERE criNombre LIKE @nombre", conn);
            cmd.Parameters.AddWithValue("@nombre", $"%{nombre}%");

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Criterio
                {
                    CriId = reader.GetInt32(0),
                    CriIdSoc = reader.GetString(1),
                    CriNombre = reader.GetString(2),
                    CriPonderacion = reader.GetInt32(3),
                    CriDescripcion = reader.GetString(4),
                    CriCampo = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
            return list;
        }

        public async Task<bool> UpdateAsync(Criterio criterio)
        {
        //    using var conn = CreateConnection();
        //    using var conn = SqlHelper.GetConnection(_config);
        //    using var cmd = new SqlCommand(@"UPDATE tblCriterio SET criIdSoc=@soc, criNombre=@nom, criPonderacion=@pond,
        //                                 criDescripcion=@desc, criCampo=@campo WHERE criId=@id", conn);
        //    cmd.Parameters.AddWithValue("@id", criterio.CriId);
        //    cmd.Parameters.AddWithValue("@soc", criterio.CriIdSoc);
        //    cmd.Parameters.AddWithValue("@nom", criterio.CriNombre);
        //    cmd.Parameters.AddWithValue("@pond", criterio.CriPonderacion);
        //    cmd.Parameters.AddWithValue("@desc", criterio.CriDescripcion);
        //    cmd.Parameters.AddWithValue("@campo", (object?)criterio.CriCampo ?? DBNull.Value);

        //    await conn.OpenAsync();
        //    return await cmd.ExecuteNonQueryAsync() > 0;
        //}


        //public async Task<bool> ActualizarCriterioAsync(Criterio criterio)
        //{
            try
            {
                using var conn = CreateConnection(); // O SqlHelper.GetConnection(_config)
                using var cmd = new SqlCommand(@"
            UPDATE tblCriterio 
            SET criIdSoc = @soc, 
                criNombre = @nom, 
                criPonderacion = @pond,
                criDescripcion = @desc, 
                criCampo = @campo 
            WHERE criId = @id", conn);

                cmd.Parameters.Add("@id", SqlDbType.Int).Value = criterio.CriId;
                cmd.Parameters.Add("@soc", SqlDbType.Int).Value = criterio.CriIdSoc;
                cmd.Parameters.Add("@nom", SqlDbType.NVarChar, 100).Value = criterio.CriNombre;
                cmd.Parameters.Add("@pond", SqlDbType.Decimal).Value = criterio.CriPonderacion;
                cmd.Parameters.Add("@desc", SqlDbType.NVarChar, 500).Value = criterio.CriDescripcion;
                cmd.Parameters.Add("@campo", SqlDbType.NVarChar, 100).Value = (object?)criterio.CriCampo ?? DBNull.Value;

                await conn.OpenAsync();
                int filasAfectadas = await cmd.ExecuteNonQueryAsync();

                bool actualizacionExitosa = filasAfectadas > 0;
                return actualizacionExitosa;
            }
            catch (SqlException ex)
            {
                // Aquí puedes loguear el error con tu sistema de logging (Serilog, NLog, etc.)
                Console.Error.WriteLine($"Error SQL al actualizar criterio: {ex.Message}");
                throw new ApplicationException("Ocurrió un error al actualizar el criterio en la base de datos.", ex);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error general: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = CreateConnection();
//            using var conn = SqlHelper.GetConnection(_config);
            using var cmd = new SqlCommand("DELETE FROM tblCriterio WHERE criId=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
