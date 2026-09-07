using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("Patologias")]
    public class Patologia
    {
        [Key] public int PatologiaId { get; set; }
        [Required, MaxLength(120)] public string Nombre { get; set; }
        [MaxLength(500)] public string Descripcion { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
        public ICollection<Diagnostico> Diagnosticos { get; set; } = new List<Diagnostico>();
    }

    [Table("Diagnosticos")]
    public class Diagnostico
    {
        [Key] public int DiagnosticoId { get; set; }
        public int PacienteId { get; set; }
        public int? PatologiaId { get; set; }
        public int FisioterapeutaId { get; set; }
        [Required, MaxLength(500)] public string DiagnosticoTexto { get; set; }
        [MaxLength(1000)] public string Observaciones { get; set; }
        [MaxLength(1000)] public string EvaluacionFuncional { get; set; }
        public DateTime FechaDiagnostico { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
        public Paciente Paciente { get; set; }
        public Patologia Patologia { get; set; }
        public Usuario Fisioterapeuta { get; set; }
        public ICollection<PlanTratamiento> PlanesTratamiento { get; set; } = new List<PlanTratamiento>();
    }

    [Table("PlanesTratamiento")]
    public class PlanTratamiento
    {
        [Key] public int PlanTratamientoId { get; set; }
        public int PacienteId { get; set; }
        public int DiagnosticoId { get; set; }
        public int FisioterapeutaId { get; set; }
        [Required, MaxLength(150)] public string Nombre { get; set; }
        [Required, MaxLength(1000)] public string Objetivos { get; set; }
        public int? DuracionEstimada { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinEstimada { get; set; }
        [Required, MaxLength(20)] public string Estado { get; set; } = "ACTIVO";
        [MaxLength(1000)] public string Observaciones { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public Paciente Paciente { get; set; }
        public Diagnostico Diagnostico { get; set; }
        public Usuario Fisioterapeuta { get; set; }
        public ICollection<TratamientoEjercicio> TratamientoEjercicios { get; set; } = new List<TratamientoEjercicio>();
        public ICollection<SesionRehabilitacion> Sesiones { get; set; } = new List<SesionRehabilitacion>();
    }

    [Table("Ejercicios")]
    public class Ejercicio
    {
        [Key] public int EjercicioId { get; set; }
        [Required, MaxLength(150)] public string Nombre { get; set; }
        [MaxLength(1000)] public string Descripcion { get; set; }
        public int? DuracionMinutos { get; set; }
        [MaxLength(1000)] public string Recomendaciones { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
        public ICollection<TratamientoEjercicio> Tratamientos { get; set; } = new List<TratamientoEjercicio>();
    }

    [Table("TratamientoEjercicios")]
    public class TratamientoEjercicio
    {
        [Key] public int TratamientoEjercicioId { get; set; }
        public int PlanTratamientoId { get; set; }
        public int EjercicioId { get; set; }
        [Required, MaxLength(100)] public string Frecuencia { get; set; }
        public int? Series { get; set; }
        public int? Repeticiones { get; set; }
        [MaxLength(1000)] public string Observaciones { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaAsignacion { get; set; }
        public PlanTratamiento PlanTratamiento { get; set; }
        public Ejercicio Ejercicio { get; set; }
        public ICollection<Seguimiento> Seguimientos { get; set; } = new List<Seguimiento>();
    }

    [Table("SesionesRehabilitacion")]
    public class SesionRehabilitacion
    {
        [Key] public int SesionId { get; set; }
        public int PlanTratamientoId { get; set; }
        public int? CitaId { get; set; }
        public int FisioterapeutaId { get; set; }
        public int NumeroSesion { get; set; }
        public DateTime FechaSesion { get; set; }
        [Required, MaxLength(1500)] public string ActividadesRealizadas { get; set; }
        [MaxLength(1500)] public string Observaciones { get; set; }
        [MaxLength(1500)] public string Resultados { get; set; }
        [Required, MaxLength(20)] public string Estado { get; set; } = "REALIZADA";
        public DateTime FechaRegistro { get; set; }
        public PlanTratamiento PlanTratamiento { get; set; }
        public Cita Cita { get; set; }
        public Usuario Fisioterapeuta { get; set; }
        public ICollection<Seguimiento> Seguimientos { get; set; } = new List<Seguimiento>();
    }

    [Table("Seguimientos")]
    public class Seguimiento
    {
        [Key] public int SeguimientoId { get; set; }
        public int SesionId { get; set; }
        public decimal? NivelDolor { get; set; }
        public decimal? MovilidadArticular { get; set; }
        public decimal? FuerzaMuscular { get; set; }
        public decimal? GradoRecuperacion { get; set; }
        [MaxLength(1500)] public string Observaciones { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int? TratamientoEjercicioId { get; set; }
        public SesionRehabilitacion Sesion { get; set; }
        public TratamientoEjercicio TratamientoEjercicio { get; set; }
    }

    [Table("Mensajes")]
    public class Mensaje
    {
        [Key] public int MensajeId { get; set; }
        public int RemitenteId { get; set; }
        public int DestinatarioId { get; set; }
        [Required, MaxLength(2000)] public string Contenido { get; set; }
        public DateTime FechaEnvio { get; set; }
        public bool Leido { get; set; }
        public Usuario Remitente { get; set; }
        public Usuario Destinatario { get; set; }
    }
}
