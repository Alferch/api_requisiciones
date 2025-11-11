namespace RequisicionesApi.Dtos
{
    public class AutProvReqCerradaDetalleProvDto
    {

        public string reqIdClave { get; set; } = default!;
        public string? reqdIdSoc { get; set; } = default!;
        public string reqdIdClave { get; set; } = default!;
        public int reqidposNo { get; set; }
        public string reqdpMatNo { get; set; } = default!;
        public string reqdpMatDes { get; set; } = default!;
        public int reqdCantidad { get; set; }
        public string? reqdUnidadMed { get; set; }
        public DateTime reqdFecEntrega { get; set; }
        public string? reqdCiudad { get; set; }
        public string? reqdMunicipio { get; set; }
        public string dpIdClave { get; set; } = default!;
        public string prov1 { get; set; } = string.Empty;
        public string prov2 { get; set; } = string.Empty;
        public string prov3 { get; set; } = string.Empty;
        public string prov4 { get; set; } = string.Empty;
        public string prov5 { get; set; } = string.Empty;
        public int authorize { get; set; } // 0/1r
        public string accion { get; set; } = default!;


    }

    public class AutProvReqCerradaDetalleProvDtoList
    {

        public required string accion { get; set; }
        public required string vigencia { get; set; }

        public required List<AutProvReqCerradaDetalleProvDto> Modelos { get; set; }
}

 

public class AutProvPosicionList
{

    public string reqIdClave { get; set; } = default!;
    public string? reqdIdSoc { get; set; } = default!;
    public string reqdIdClave { get; set; } = default!;
    public string prov { get; set; } = string.Empty;
    public required List<AutProvRequisicionNot> Modelos { get; set; }
}


public class AutProvRequisicionNot
{

 
    public int reqidposNo { get; set; }
    public string reqdpMatNo { get; set; } = default!;
    public string reqdpMatDes { get; set; } = default!;
    public int reqdCantidad { get; set; }
    public string? reqdUnidadMed { get; set; }
    public DateTime reqdFecEntrega { get; set; }
    public string? reqdCiudad { get; set; }
    public string? reqdMunicipio { get; set; }

}


}
