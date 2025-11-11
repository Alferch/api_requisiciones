using Microsoft.Data.SqlClient;
using RequisicionesApi.Models;

namespace RequisicionesApi.Repositorios
{
    public class ProveedorRepository
    {
        private readonly string _connectionString;

        public ProveedorRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Models.ProveedorModel>> GetProveedoresAsync(string? id, string? nombre, string? rfc, int pagina, int tamano)
        {
            var proveedores = new List<Models.ProveedorModel>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
            SELECT *
            FROM tblProveedores
            WHERE (@Id IS NULL OR provIdProv LIKE '%' + @Id + '%')
              AND (@Nombre IS NULL OR provNombre LIKE '%' + @Nombre + '%')
              AND (@RFC IS NULL OR provRFC LIKE '%' + @RFC + '%')
            ORDER BY provIdProv
            OFFSET @Offset ROWS FETCH NEXT @Tamano ROWS ONLY;
        ", connection);

            command.Parameters.AddWithValue("@Id", (object?)id ?? DBNull.Value);
            command.Parameters.AddWithValue("@Nombre", (object?)nombre ?? DBNull.Value);
            command.Parameters.AddWithValue("@RFC", (object?)rfc ?? DBNull.Value);
            command.Parameters.AddWithValue("@Offset", (pagina - 1) * tamano);
            command.Parameters.AddWithValue("@Tamano", tamano);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                proveedores.Add(new Models.ProveedorModel
                {
                    ProvIdSoc = reader["provIdSoc"].ToString()!,
                    provIdGrupoM = reader["provIdGrupoM"].ToString()!,
                    ProvIdProv = reader["provIdProv"].ToString()!,
                    ProvNombre = reader["provNombre"].ToString()!,
                    ProvRFC = reader["provRFC"].ToString()!,
                    ProvNomVendedor = reader["provNomVendedor"].ToString()!,
                    ProvTelefono = reader["provTeléfono"].ToString()!,
                    ProvCorreo = reader["provCorreo"].ToString()!,
                    ProvIdioma = reader["provIdioma"]?.ToString(),
                    ProvClasificacion = reader["ProvClasificacion"]?.ToString(),
                });
            }

            return proveedores;
        }
    }
}
