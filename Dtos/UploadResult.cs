using System.Collections.Generic;

namespace RequisicionesApi.Dtos
{
    public class UploadResult
    {

        public int Opcion { get; set; }
        public int Insertados { get; set; }
        public int Rechazados { get; set; }
        public List<RowError> Errores { get; set; } = new();

    }
}
