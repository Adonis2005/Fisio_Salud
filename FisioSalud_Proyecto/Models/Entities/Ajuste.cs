using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("AjustesSistema")]
    public class Ajuste
    {
        [Key, MaxLength(100)] public string Clave { get; set; }
        [Required] public string Valor { get; set; }
    }
}
