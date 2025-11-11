using Microsoft.Data.SqlClient;
using RequisicionesApi.Entidades;
using RequisicionesApi.Interfaces;

namespace RequisicionesApi.Repositorios
{
    public class ImputacionValRepository : IImputacionValRepository
    {
        private readonly string _connectionString;

        public ImputacionValRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public async Task<IEnumerable<ImputacionVal>> GetAllAsync()
        {
            var list = new List<ImputacionVal>();
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("SELECT impIdImp, convert(varchar, impIdSArea)  impIdSArea, impvValor FROM tblImpVal", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new ImputacionVal
                {
                    ImpIdImp = reader.GetString(0),
                    ImpIdSArea = reader.GetString(1),
                    ImpvValor = reader.GetString(2)
                });
            }

            return list;
        }

        public async Task<ImputacionVal?> GetByIdAsync(string id)
        {
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("SELECT impIdImp, convert(varchar,impIdSArea) impIdSArea, convert(varchar,impvValor) impvValor FROM tblImpVal WHERE impIdImp = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? new ImputacionVal
                {
                    ImpIdImp = reader.GetString(0),
                    ImpIdSArea = reader.GetString(1),
                    ImpvValor = reader.GetString(2)
                }
                : null;
        }

        public async Task CreateAsync(ImputacionVal entity)
        {
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("INSERT INTO tblImpVal (impIdImp, impIdSArea, impvValor) VALUES (@ImpIdImp, @ImpIdSArea, @ImpvValor)", connection);
            command.Parameters.AddWithValue("@ImpIdImp", entity.ImpIdImp);
            command.Parameters.AddWithValue("@ImpIdSArea", entity.ImpIdSArea);
            command.Parameters.AddWithValue("@ImpvValor", entity.ImpvValor);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(ImputacionVal entity)
        {
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("UPDATE tblImpVal SET impIdSArea = @ImpIdSArea, impvValor = @ImpvValor WHERE impIdImp = @ImpIdImp", connection);
            command.Parameters.AddWithValue("@ImpIdImp", entity.ImpIdImp);
            command.Parameters.AddWithValue("@ImpIdSArea", entity.ImpIdSArea);
            command.Parameters.AddWithValue("@ImpvValor", entity.ImpvValor);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(string id)
        {
            using var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand("DELETE FROM tblImpVal WHERE impIdImp = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<ImputacionValDetalle>> GetAllDetalleAsync()
        {
            var lista = new List<ImputacionValDetalle>();

            using var connection = new SqlConnection(_connectionString);
            var query = @"
SELECT i.impIdImp, i.impNombre, sarSAreaC, convert(varchar, sarIdArea) sarIdArea, a.arNombre,
       convert(varchar,iv.impIdSArea) impIdSArea, sa.arNombre AS Subarea, convert(varchar, impvValor) impvValor
FROM tblImputacion i
INNER JOIN tblImpVal iv ON iv.impIdImp = i.impIdImp
INNER JOIN tblSubArea sa ON sa.sarIdSArea = iv.impIdSArea
INNER JOIN tblArea a ON a.arIdArea = sa.sarIdArea
ORDER BY i.impIdImp";

            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new ImputacionValDetalle
                {
                    ImpIdImp = reader.GetString(0),
                    ImpNombre = reader.GetString(1),
                    SarSAreaC = reader.GetString(2),
                    SarIdArea = reader.GetString(3),
                    AreaNombre = reader.GetString(4),
                    ImpIdSArea = reader.GetString(5),
                    SubareaNombre = reader.GetString(6),
                    ImpvValor = reader.GetString(7)
                });
            }

            return lista;
        }
    }
}


