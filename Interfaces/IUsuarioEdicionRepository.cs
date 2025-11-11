using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface IUsuarioEdicionRepository
    {
        Task<UsuarioEdicionDto> GetAsync(string usrIdClave);
        Task<bool> CreateAsync(UsuarioEdicionDto dto);
        Task<bool> UpdateAsync(UsuarioEdicionDto dto);
        Task<bool> DeleteAsync(string usrIdClave);
    }
}
