using System.ComponentModel.DataAnnotations;

namespace RequisicionesApi.Models
{
    public class HisPedidoModel
    {
        [Required, StringLength(4)] public string hisIdSoc { get; set; }
        [Required, StringLength(10)] public string hisProvId { get; set; }
        [Required, StringLength(10)] public string hisIdPedido { get; set; }
        [Required, StringLength(10)] public string hisPosMaterial { get; set; }
        [Required, StringLength(1)] public string hisImputacion { get; set; }
        [Required, StringLength(4)] public string hisCentro { get; set; }
        [Required, StringLength(4)] public string hisOrgCompras { get; set; }
        [Required, StringLength(3)] public string hisGpoCompras { get; set; }
        [Required, StringLength(4)] public string hisCPag { get; set; }
        [StringLength(18)] public string hisIdMaterial { get; set; }
        [Required, StringLength(40)] public string hisDescripción { get; set; }
        [Required, StringLength(14)] public string hisCantidad { get; set; }
        [Required, StringLength(4)] public string hisUM { get; set; }
        [Required, StringLength(14)] public string hisPrecio { get; set; }
        [Required, StringLength(3)] public string hisMoneda { get; set; }
        [Required, StringLength(2)] public string hisIndicaIva { get; set; }
    }
}
