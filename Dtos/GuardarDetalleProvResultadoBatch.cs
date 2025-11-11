namespace RequisicionesApi.Dtos
{
    public class GuardarDetalleProvResultadoBatch
    {

        public bool Ok { get; init; }
        public int Procesados { get; init; }
        public int DetallesUpserted { get; init; }
        public int ProveedoresInsertados { get; init; }
        public int VobosAplicados { get; init; }

    }
}
