namespace RequisicionesApi.Dtos
{
    public class RequisicionAnexoDto
    {
        public int reqAidposNo { get; set; }
        public byte[] reqAnexo { get; set; } = Array.Empty<byte>();
    }
}
