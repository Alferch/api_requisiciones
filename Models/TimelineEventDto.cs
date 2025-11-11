namespace RequisicionesApi.Models
{
    public class TimelineEventDto
    {

        public string NumeroRequisicion { get; set; } = default!;
        public string DescripcionRequisicion { get; set; } = default!;
        public string Hito { get; set; } = default!;
        public string Estado { get; set; } = default!;
        public DateTime? FechaEvento { get; set; }

    }
}
