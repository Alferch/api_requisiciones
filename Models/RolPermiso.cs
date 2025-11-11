namespace RequisicionesApi.Models
{
    public class RolPermiso
    {
        public string UsrIdRol { get; set; }
        public string RolNombre { get; set; }
        public Permisos Permisos { get; set; }
        public List<Imputacion> Imputaciones { get; set; }
    }
}
