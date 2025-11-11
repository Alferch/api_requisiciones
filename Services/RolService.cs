using RequisicionesApi.Entidades;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Services
{
    public class RolService : IRolService
    {
        private readonly IRolRepository _repository;

        public RolService(IRolRepository repository)
        {
            _repository = repository;
        }

        public async Task<RolDto?> GetByIdAsync(string id)
        {
            var rol = await _repository.GetByIdAsync(id);
            return rol is null ? null : new RolDto { IdRol = rol.IdRol, NombreRol = rol.NombreRol };
        }

        public async Task<IEnumerable<RolDto>> GetAllAsync()
        {
            var roles = await _repository.GetAllAsync();
            return roles.Select(r => new RolDto { IdRol = r.IdRol, NombreRol = r.NombreRol });
        }

        public async Task CreateAsync(RolDto rol)
        {
            await _repository.CreateAsync(new Rol { IdRol = rol.IdRol, NombreRol = rol.NombreRol });
        }

        public async Task UpdateAsync(RolDto rol)
        {
            await _repository.UpdateAsync(new Rol { IdRol = rol.IdRol, NombreRol = rol.NombreRol });
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
        }

    }
}
