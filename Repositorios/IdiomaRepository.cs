using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

//using RequisicionesApi.Data;
using RequisicionesApi.Models;
using RequisicionesApi.Repositorios;

namespace RequisicionesApi.Repositorios
{
    public class IdiomaRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public IdiomaRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Idioma>> GetAllAsync()
        {
            var list = new List<Idioma>();
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("SELECT IdIdIdioma, IdNombre FROM tblIdioma", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Idioma
                {
                    IdIdIdioma = reader["IdIdIdioma"].ToString(),
                    IdNombre = reader["IdNombre"].ToString()
                });
            }
            return list;
        }

        public async Task<Idioma> GetByIdAsync(string id)
        {
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("SELECT IdIdIdioma, IdNombre FROM tblIdioma WHERE IdIdIdioma = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Idioma
                {
                    IdIdIdioma = reader["IdIdIdioma"].ToString(),
                    IdNombre = reader["IdNombre"].ToString()
                };
            }
            return null;
        }

        public async Task<bool> CreateAsync(Idioma idioma)
        {
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("INSERT INTO tblIdioma (IdIdIdioma, IdNombre) VALUES (@id, @nombre)", conn);
            cmd.Parameters.AddWithValue("@id", idioma.IdIdIdioma);
            cmd.Parameters.AddWithValue("@nombre", idioma.IdNombre);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Idioma idioma)
        {
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("UPDATE tblIdioma SET IdNombre = @nombre WHERE IdIdIdioma = @id", conn);
            cmd.Parameters.AddWithValue("@id", idioma.IdIdIdioma);
            cmd.Parameters.AddWithValue("@nombre", idioma.IdNombre);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand("DELETE FROM tblIdioma WHERE IdIdIdioma = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
