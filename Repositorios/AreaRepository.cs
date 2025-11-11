using Microsoft.Data.SqlClient;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Repositorios
{

    public class AreaRepository : IAreaRepository
    {
        private readonly IConfiguration _config;
        public AreaRepository(IConfiguration config) => _config = config;

        public async Task<IEnumerable<AreaDto>> GetAllAsync()
        {
            var areas = new List<AreaDto>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("SELECT arIdArea, arNombre FROM tblArea", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                areas.Add(new AreaDto
                {
                    ArIdArea = reader.GetInt32(0),
                    ArNombre = reader.GetString(1)
                });
            }
            return areas;
        }

        public async Task<AreaDto> GetByIdAsync(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("SELECT arIdArea, arNombre FROM tblArea WHERE arIdArea = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new AreaDto
                {
                    ArIdArea = reader.GetInt32(0),
                    ArNombre = reader.GetString(1)
                };
            }
            return null;
        }

        public async Task<bool> CreateAsync(AreaDto dto)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("INSERT INTO tblArea (arIdArea, arNombre) VALUES (@id, @nombre)", conn);
            cmd.Parameters.AddWithValue("@id", dto.ArIdArea);
            cmd.Parameters.AddWithValue("@nombre", dto.ArNombre);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateAsync(AreaDto dto)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("UPDATE tblArea SET arNombre = @nombre WHERE arIdArea = @id", conn);
            cmd.Parameters.AddWithValue("@id", dto.ArIdArea);
            cmd.Parameters.AddWithValue("@nombre", dto.ArNombre);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("DELETE FROM tblArea WHERE arIdArea = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}