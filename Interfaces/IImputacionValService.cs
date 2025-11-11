using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface IImputacionValService
    {
        Task<IEnumerable<ImputacionValDto>> GetAllAsync();
        Task<ImputacionValDto?> GetByIdAsync(string id);
        Task CreateAsync(ImputacionValDto dto);
        Task UpdateAsync(ImputacionValDto dto);
        Task DeleteAsync(string id);

        Task<IEnumerable<ImputacionValDetalleDto>> GetAllDetalleAsync();

    }
}
