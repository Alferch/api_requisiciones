namespace RequisicionesApi.Models
{
    public class Criterio
    {
        public int CriId { get; set; }
        public string CriIdSoc { get; set; }
        public string CriNombre { get; set; }
        public int CriPonderacion { get; set; }
        public string CriDescripcion { get; set; }
        public string? CriCampo { get; set; }
    }
}
