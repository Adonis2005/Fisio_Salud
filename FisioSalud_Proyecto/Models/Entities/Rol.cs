using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("Roles")]
    public class Rol
    {
        [Key]
        public int RolId { get; set; }

        [Required, MaxLength(30)]
        public string Nombre { get; set; }

        [MaxLength(150)]
        public string Descripcion { get; set; }

        public bool Estado { get; set; } = true;

        public ICollection<Usuario> Usuarios { get; set; }
    }
}
