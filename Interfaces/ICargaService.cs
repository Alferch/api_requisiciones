using Microsoft.AspNetCore.Http;
using RequisicionesApi.Dtos;
using System.Threading.Tasks;


namespace RequisicionesApi.Interfaces
{
    public interface ICargaService
    {
        Task<UploadResult> UploadExcelAsync(IFormFile file, int opcion);
        Task<UploadResult> UploadExcelDireccionesBulkAsync(IFormFile file);
    }
}




