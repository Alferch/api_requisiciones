using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface IRolService
    {
        Task<RolDto?> GetByIdAsync(string id);
        Task<IEnumerable<RolDto>> GetAllAsync();
        Task CreateAsync(RolDto rol);
        Task UpdateAsync(RolDto rol);
        Task DeleteAsync(string id);
    }
}
