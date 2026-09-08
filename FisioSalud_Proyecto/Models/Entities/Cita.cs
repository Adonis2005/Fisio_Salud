using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("Citas")]
    public class Cita
    {
        [Key]
        public int CitaId { get; set; }

        public int PacienteId { get; set; }

        public int FisioterapeutaId { get; set; }

        public int? ServicioId { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public TimeSpan HoraInicio { get; set; }

        public TimeSpan? HoraFin { get; set; }

        [MaxLength(500)]
        public string MotivoConsulta { get; set; }

        [Required, MaxLength(20)]
        public string Estado { get; set; } = "PROGRAMADA";

        [MaxLength(1000)]
        public string Observaciones { get; set; }

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaActualizacion { get; set; }

        [ForeignKey(nameof(PacienteId))]
        public Paciente Paciente { get; set; }

        [ForeignKey(nameof(FisioterapeutaId))]
        public Usuario Fisioterapeuta { get; set; }

        [ForeignKey(nameof(ServicioId))]
        public Servicio Servicio { get; set; }
    }
}
