using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("Pacientes")]
    public class Paciente
    {
        [Key]
        public int PacienteId { get; set; }

        [Required, MaxLength(80)]
        public string Nombres { get; set; }

        [Required, MaxLength(80)]
        public string Apellidos { get; set; }

        [Required, MaxLength(20)]
        public string Identificacion { get; set; }

        [Required]
        public DateTime FechaNacimiento { get; set; }

        [Required, MaxLength(1)]
        public string Sexo { get; set; }

        [MaxLength(200)]
        public string Direccion { get; set; }

        [MaxLength(20)]
        public string Telefono { get; set; }

        [MaxLength(120)]
        public string Correo { get; set; }

        public bool Estado { get; set; } = true;

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaActualizacion { get; set; }

        public int? UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; }

        [NotMapped]
        public string NombreCompleto => $"{Nombres} {Apellidos}";
    }
}
