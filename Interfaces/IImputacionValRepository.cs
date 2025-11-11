using Microsoft.Data.SqlClient;
using RequisicionesApi.Entidades;
using RequisicionesApi.Repositorios;

namespace RequisicionesApi.Interfaces
{
    public interface IImputacionValRepository
    {
        Task<IEnumerable<ImputacionVal>> GetAllAsync();
        Task<ImputacionVal?> GetByIdAsync(string id);
        Task CreateAsync(ImputacionVal entity);
        Task UpdateAsync(ImputacionVal entity);
        Task DeleteAsync(string id);

        Task<IEnumerable<ImputacionValDetalle>> GetAllDetalleAsync();
    }
}
