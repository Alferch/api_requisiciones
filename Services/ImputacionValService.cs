using RequisicionesApi.Entidades;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Services
{
    public class ImputacionValService : IImputacionValService
    {

        private readonly IImputacionValRepository _repository;

        public ImputacionValService(IImputacionValRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ImputacionValDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(x => new ImputacionValDto
            {
                ImpIdImp = x.ImpIdImp.ToString(),
                ImpIdSArea = x.ImpIdSArea.ToString(),
                ImpvValor = x.ImpvValor.ToString()
            });
        }

        public async Task<ImputacionValDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity is null ? null : new ImputacionValDto
            {
                ImpIdImp = entity.ImpIdImp.ToString()   ,
                ImpIdSArea = entity.ImpIdSArea.ToString()   ,
                ImpvValor = entity.ImpvValor.ToString() 
            };
        }

        public async Task CreateAsync(ImputacionValDto dto)
        {
            var entity = new ImputacionVal
            {
                ImpIdImp = dto.ImpIdImp ,
                ImpIdSArea = dto.ImpIdSArea,
                ImpvValor = dto.ImpvValor
            };
            await _repository.CreateAsync(entity);
        }

        public async Task UpdateAsync(ImputacionValDto dto)
        {
            var entity = new ImputacionVal
            {
                ImpIdImp = dto.ImpIdImp,
                ImpIdSArea = dto.ImpIdSArea,
                ImpvValor = dto.ImpvValor
            };
            await _repository.UpdateAsync(entity);
        }

        public async Task DeleteAsync(string id) => await _repository.DeleteAsync(id);

        public async Task<IEnumerable<ImputacionValDetalleDto>> GetAllDetalleAsync()
        {
            var data = await _repository.GetAllDetalleAsync();
            return data.Select(x => new ImputacionValDetalleDto
            {
                ImpIdImp = x.ImpIdImp.ToString(),
                ImpNombre = x.ImpNombre,
                SarSAreaC = x.SarSAreaC,
                SarIdArea = x.SarIdArea.ToString(),
                AreaNombre = x.AreaNombre,
                ImpIdSArea = x.ImpIdSArea.ToString(),
                SubareaNombre = x.SubareaNombre,
                ImpvValor = x.ImpvValor
            });
        }

    }
}
