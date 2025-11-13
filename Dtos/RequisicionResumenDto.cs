namespace RequisicionesApi.Dtos
{

    public sealed class RequisicionResumenDto
    {
        public string reqIdClave { get; init; } = default!;
        public string? reqDescripcion { get; init; }
        public DateTime? reqFecFin { get; init; }
        public DateTime? reqFecVoBo { get; init; }
        public string? reqNiveles_Aut { get; init; }
        public DateTime? reqFecVigencia { get; init; }
        public string? reqprovgan { get; init; }

        public string? EstatusVoBo { get; init; }
        public string? EstadoCabecera { get; init; }
        public string? EstadoFlujoPedido { get; init; }
        public string? EstadoVigencia { get; init; }
        public string? FlujoResumen { get; init; }
        public string Resultado { get; init; } = default!;
    }

}
