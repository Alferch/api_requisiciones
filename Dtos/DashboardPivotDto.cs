namespace RequisicionesApi.Dtos
{

    public sealed class DashboardPivotDto
    {
        // Conteos
        public int Creadas { get; init; }
        public int CerradasXComprador { get; init; }
        public int ProcesoDeAutorizacion { get; init; }
        public int EstadoFlujoPedido { get; init; }
        public int CanceladasXVigencia { get; init; }
        public int Canceladas { get; init; }
        public int Total { get; init; }

        // Porcentajes
        public decimal CreadasPct { get; init; }
        public decimal CerradasXCompradorPct { get; init; }
        public decimal ProcesoDeAutorizacionPct { get; init; }
        public decimal EstadoFlujoPedidoPct { get; init; }
        public decimal CanceladasXVigenciaPct { get; init; }
        public decimal CanceladasPct { get; init; }
        public decimal TotalPct { get; init; }
    }

}
