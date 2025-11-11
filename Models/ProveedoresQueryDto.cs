namespace RequisicionesApi.Models
{
    public class ProveedoresQueryDto
    {
        public int Pagina { get; set; } = 1;
        public int Tamano { get; set; } = 10;
        public string? Id { get; set; }
        public string? Nombre { get; set; }
        public string? RFC { get; set; }
    }
}
