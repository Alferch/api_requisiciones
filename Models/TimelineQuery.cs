namespace RequisicionesApi.Models
{
    public class TimelineQuery
    {
 
        public string? Numero { get; set; }
        public bool UseLike { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? Hito { get; set; }
        public string? Estado { get; set; }
        /// <summary> 'timeline' (por tipo de hito) o 'fecha' (por FechaEvento) </summary>
        public string Order { get; set; } = "timeline";
 

}
}
