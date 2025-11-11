using RequisicionesApi.Entidades;

namespace RequisicionesApi.Interfaces
{
    public interface IRolRepository
    {
        Task<Rol?> GetByIdAsync(string id);
        Task<IEnumerable<Rol>> GetAllAsync();
        Task CreateAsync(Rol rol);
        Task UpdateAsync(Rol rol);
        Task DeleteAsync(string id);
    }
}
