using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }

        public int RolId { get; set; }

        [Required, MaxLength(80)]
        public string Nombres { get; set; }

        [Required, MaxLength(80)]
        public string Apellidos { get; set; }

        [Required, MaxLength(20)]
        public string Identificacion { get; set; }

        [Required, MaxLength(120)]
        public string Correo { get; set; }

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; }

        [MaxLength(20)]
        public string Telefono { get; set; }

        public bool Estado { get; set; } = true;

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaActualizacion { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        [ForeignKey(nameof(RolId))]
        public Rol Rol { get; set; }

        [NotMapped]
        public string NombreCompleto => $"{Nombres} {Apellidos}";
    }
}
