namespace RequisicionesApi.Models
{
    public class UsuarioImputacionDto
    {
        public string UsrIdImp { get; set; }
        public string UsrIdRol { get; set; }

        public int? ImpIdSArea { get; set; }     // Solo lectura
        public string SubAreaNombre { get; set; } // Solo lectura
        public int? SarIdArea { get; set; }       // Solo lectura
        public string AreaNombre { get; set; }

        public string impNivel { get; set; }
    }
}
