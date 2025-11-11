using Microsoft.Data.SqlClient;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;
using System.Data;

public class SubAreaRepository : ISubAreaRepository
{
    private readonly IConfiguration _config;
    public SubAreaRepository(IConfiguration config) => _config = config;
 

    public async Task<IEnumerable<SubAreaDto>> GetAllAsync()
    {
        var result = new List<SubAreaDto>();
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        using var cmd = new SqlCommand("SELECT sarIdSArea, sarSAreaC, sarIdArea, arNombre FROM tblSubArea", conn);
        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new SubAreaDto
            {
                SarIdSArea = reader.GetInt32(0),
                SarSAreaC = reader.GetString(1),
                SarIdArea = reader.GetInt32(2),
                ArNombre = reader.GetString(3)
            });
        }
        return result;
    }

    public async Task<SubAreaDto> GetByIdAsync(int id)
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        using var cmd = new SqlCommand("SELECT sarIdSArea, sarSAreaC, sarIdArea, arNombre FROM tblSubArea WHERE sarIdSArea = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SubAreaDto
            {
                SarIdSArea = reader.GetInt32(0),
                SarSAreaC = reader.GetString(1),
                SarIdArea = reader.GetInt32(2),
                ArNombre = reader.GetString(3)
            };
        }
        return null;
    }

    public async Task<bool> CreateAsync(SubAreaDto dto)
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        using var cmd = new SqlCommand(@"INSERT INTO tblSubArea (sarIdSArea, sarSAreaC, sarIdArea, arNombre)
                                         VALUES (@id, @code, @areaId, @name)", conn);
        cmd.Parameters.AddWithValue("@id", dto.SarIdSArea);
        cmd.Parameters.AddWithValue("@code", dto.SarSAreaC);
        cmd.Parameters.AddWithValue("@areaId", dto.SarIdArea);
        cmd.Parameters.AddWithValue("@name", dto.ArNombre);
        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateAsync(SubAreaDto dto)
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        using var cmd = new SqlCommand(@"UPDATE tblSubArea SET 
            sarSAreaC = @code, sarIdArea = @areaId, arNombre = @name WHERE sarIdSArea = @id", conn);
        cmd.Parameters.AddWithValue("@id", dto.SarIdSArea);
        cmd.Parameters.AddWithValue("@code", dto.SarSAreaC);
        cmd.Parameters.AddWithValue("@areaId", dto.SarIdArea);
        cmd.Parameters.AddWithValue("@name", dto.ArNombre);
        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        using var cmd = new SqlCommand("DELETE FROM tblSubArea WHERE sarIdSArea = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
