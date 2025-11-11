using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface ICriterioService
    {
        Task<int> CreateAsync(Criterio criterio);
        Task<List<Criterio>> GetAllAsync();
        Task<List<Criterio>> GetBySocIdAsync(string criIdSoc);
        Task<List<Criterio>> GetByNombreAsync(string nombre);
        Task<bool> UpdateAsync(Criterio criterio);
        Task<bool> DeleteAsync(int id);
    }
}
