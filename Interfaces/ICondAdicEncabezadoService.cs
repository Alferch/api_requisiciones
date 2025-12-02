using RequisicionesApi.Models.Condiciones;
using System.Threading.Tasks;

namespace RequisicionesApi.Interfaces
{
    public interface ICondAdicEncabezadoService
    {

        Task InsertAsync(List<CondAdicEncabezado> entidades);
//        Task InsertAsync(CondAdicEncabezado entidad);
        Task<CondAdicEncabezado> GetByIdAsync(string reqIdClave, string idCondicion);
        Task<IEnumerable<CondAdicEncabezado>> GetAllAsync();
        //Task UpdateAsync(CondAdicEncabezado entidad);
        Task<int> UpdateAsync(List<CondAdicEncabezado> entidades);
        Task DeleteAsync(string reqIdClave, string idCondicion);

        Task<IEnumerable<Condicion>> GetAllAsyncCondicion();



    }
}
