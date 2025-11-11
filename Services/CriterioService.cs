using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;
using RequisicionesApi.Repositorios;

namespace RequisicionesApi.Services
{
    public class CriterioService : ICriterioService
    {
        private readonly CriterioRepository _repo;

        public CriterioService(CriterioRepository repo)
        {
            _repo = repo;
        }

        public Task<int> CreateAsync(Criterio criterio) => _repo.CreateAsync(criterio);
        public Task<List<Criterio>> GetAllAsync() => _repo.GetAllAsync();
        public Task<List<Criterio>> GetBySocIdAsync(string criIdSoc) => _repo.GetBySocIdAsync(criIdSoc);
        public Task<List<Criterio>> GetByNombreAsync(string nombre) => _repo.GetByNombreAsync(nombre);
        public Task<bool> UpdateAsync(Criterio criterio) => _repo.UpdateAsync(criterio);
        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
