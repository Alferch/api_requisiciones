using System.ComponentModel.DataAnnotations;

public class MaestroMaterialModel
{
    [Required, StringLength(18)]
    public string mmatIdClave { get; set; }

    [Required, StringLength(4)]
    public string mmatIdSoc { get; set; }

    [Required, StringLength(18)]
    public string mmatIdCompleto { get; set; }

    [Required, StringLength(40)]
    public string mmatDescripción { get; set; }

    [Required, StringLength(4)]
    public string mmatTipoM { get; set; }

    [Required, StringLength(9)]
    public string mmatIdGrupoM { get; set; }

    [Required, StringLength(3)]
    public string mmatUnidadMedida { get; set; }

    [Required, StringLength(3)]
    public string mmatMoneda { get; set; }

    [Required, StringLength(14)]
    public string mmatPrecioMM { get; set; }

    [StringLength(14)]
    public string mmatExistencia { get; set; }

    [StringLength(10)]
    public string mmatUltimopedido { get; set; }

    [Required]
    public DateTime mmatFechaultpedido { get; set; }

    [Required, RegularExpression("A|I", ErrorMessage = "Solo se permiten 'A' o 'I'")]
    public string mmatEstado { get; set; }

    public string mmatEspecificaciones { get; set; }
}