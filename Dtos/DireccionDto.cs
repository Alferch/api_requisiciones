namespace RequisicionesApi.Dtos
{
    public class DireccionDto
    {

        public int IdDir { get; set; }
        public string CodigoPostalDir { get; set; } = default!;
        public string? CveColoniaDir { get; set; }
        public string? NombreColoniaDir { get; set; }
        public string? CveMunicipioDir { get; set; }
        public string? NombreMunicipioDir { get; set; }
        public string? CveLocalidadDir { get; set; }
        public string? NombreLocalidadDir { get; set; }
        public string CveEstadoDir { get; set; } = default!;
        public string NombreEstadoDir { get; set; } = default!;
        public DateTime FechaCreacionDir { get; set; }
        public string? UsuarioCreacionDir { get; set; }

    }
}
