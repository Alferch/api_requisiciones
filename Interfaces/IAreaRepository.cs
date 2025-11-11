using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface IAreaRepository
    {
        Task<IEnumerable<AreaDto>> GetAllAsync();
        Task<AreaDto> GetByIdAsync(int id);
        Task<bool> CreateAsync(AreaDto dto);
        Task<bool> UpdateAsync(AreaDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
