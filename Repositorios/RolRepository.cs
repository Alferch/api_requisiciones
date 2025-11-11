using Microsoft.Data.SqlClient;
using RequisicionesApi.Entidades;
using RequisicionesApi.Interfaces;

namespace RequisicionesApi.Repositorios
{
    public class RolRepository : IRolRepository
    {
        private readonly string _connectionString;

        public RolRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public async Task<Rol?> GetByIdAsync(string id)
        {
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("SELECT rolIdRol, rolNombre FROM tblRoles WHERE rolIdRol = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? new Rol { IdRol = reader.GetString(0), NombreRol = reader.GetString(1) }
                : null;
        }

        public async Task<IEnumerable<Rol>> GetAllAsync()
        {
            var roles = new List<Rol>();
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("SELECT rolIdRol, rolNombre FROM tblRoles", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                roles.Add(new Rol { IdRol = reader.GetString(0), NombreRol = reader.GetString(1) });
            }

            return roles;
        }

        public async Task CreateAsync(Rol rol)
        {
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("INSERT INTO tblRoles (rolIdRol, rolNombre) VALUES (@Id, @Nombre)", connection);
            command.Parameters.AddWithValue("@Id", rol.IdRol);
            command.Parameters.AddWithValue("@Nombre", rol.NombreRol);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Rol rol)
        {
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("UPDATE tblRoles SET rolNombre = @Nombre WHERE rolIdRol = @Id", connection);
            command.Parameters.AddWithValue("@Id", rol.IdRol);
            command.Parameters.AddWithValue("@Nombre", rol.NombreRol);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(string id)
        {
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("DELETE FROM tblRoles WHERE rolIdRol = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

    }
}
