namespace RequisicionesApi.Models
{
    public class UsuarioRolPermisosDto
    {
        public string UsrIdRol { get; set; }
        public string RolNombre { get; set; }  // Solo lectura

        public string UsrPerAlta { get; set; }
        public string UsrPerUpdate { get; set; }
        public string UsrPerDel { get; set; }
        public string UsrPerAut { get; set; }
        public string UsrPerLic { get; set; }
        public string UsrPerReq { get; set; }
        public string Configuracion { get; set; }
        public string UsrPerVoBo { get; set; }
        public string UsrPerCompara { get; set; }
        public string UsrPerLibera { get; set; }

        public List<UsuarioImputacionDto>? Imputaciones { get; set; }
    }
}
