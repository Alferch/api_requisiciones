using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface ISubAreaRepository
    {
        Task<IEnumerable<SubAreaDto>> GetAllAsync();
        Task<SubAreaDto> GetByIdAsync(int id);
        Task<bool> CreateAsync(SubAreaDto dto);
        Task<bool> UpdateAsync(SubAreaDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
