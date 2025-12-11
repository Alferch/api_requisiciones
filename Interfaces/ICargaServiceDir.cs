using RequisicionesApi.Dtos;

namespace RequisicionesApi.Interfaces
{
    public interface ICargaServiceDir
    {
         Task<UploadResult> UploadExcelDireccionesBulkAsync(IFormFile file);
    }
}
