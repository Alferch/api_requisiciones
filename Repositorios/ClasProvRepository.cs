using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using RequisicionesApi.Models;
using Microsoft.Data.SqlClient;

namespace RequisicionesApi.Repositorios
{
    public class ClasProvRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public ClasProvRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<ClasProv>> GetAllAsync()
        {
            var list = new List<ClasProv>();
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("SELECT cpIdclas, cpNombre FROM tblClasProv", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ClasProv
                {
                    CpIdclas = reader["cpIdclas"].ToString(),
                    CpNombre = reader["cpNombre"].ToString()
                });
            }
            return list;
        }

        public async Task<ClasProv> GetByIdAsync(string id)
        {
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("SELECT cpIdclas, cpNombre FROM tblClasProv WHERE cpIdclas = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ClasProv
                {
                    CpIdclas = reader["cpIdclas"].ToString(),
                    CpNombre = reader["cpNombre"].ToString()
                };
            }
            return null;
        }

        public async Task<bool> CreateAsync(ClasProv item)
        {
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("INSERT INTO tblClasProv (cpIdclas, cpNombre) VALUES (@id, @nombre)", conn);
            cmd.Parameters.AddWithValue("@id", item.CpIdclas);
            cmd.Parameters.AddWithValue("@nombre", item.CpNombre);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateAsync(ClasProv item)
        {
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("UPDATE tblClasProv SET cpNombre = @nombre WHERE cpIdclas = @id", conn);
            cmd.Parameters.AddWithValue("@id", item.CpIdclas);
            cmd.Parameters.AddWithValue("@nombre", item.CpNombre);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("DELETE FROM tblClasProv WHERE cpIdclas = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
