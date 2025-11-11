using RequisicionesApi.Models.Condiciones;
using System.Threading.Tasks;

namespace RequisicionesApi.Interfaces
{
    public interface ICondAdicEncabezadoService
    {


        Task InsertAsync(CondAdicEncabezado entidad);
        Task<CondAdicEncabezado> GetByIdAsync(string reqIdClave, string idCondicion);
        Task<IEnumerable<CondAdicEncabezado>> GetAllAsync();
        Task UpdateAsync(CondAdicEncabezado entidad);
        Task DeleteAsync(string reqIdClave, string idCondicion);

        Task<IEnumerable<Condicion>> GetAllAsyncCondicion();

    }
}
