using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("AsignacionesPaciente")]
    public class AsignacionPaciente
    {
        [Key] public int AsignacionPacienteId { get; set; }
        public int PacienteId { get; set; }
        public int FisioterapeutaId { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaAsignacion { get; set; }
        public DateTime? FechaFin { get; set; }
        public Paciente Paciente { get; set; }
        public Usuario Fisioterapeuta { get; set; }
    }

    [Table("EquiposTerapeuticos")]
    public class EquipoTerapeutico
    {
        [Key] public int EquipoId { get; set; }
        [Required, MaxLength(120)] public string Nombre { get; set; }
        [Required, MaxLength(30)] public string Codigo { get; set; }
        [Required, MaxLength(50)] public string Tipo { get; set; }
        [Required, MaxLength(20)] public string EstadoOperativo { get; set; } = "DISPONIBLE";
        [Column(TypeName = "decimal(8,2)")] public decimal? VelocidadMaxima { get; set; }
        [Column(TypeName = "decimal(8,2)")] public decimal? InclinacionMaxima { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
        public ICollection<UsoEquipo> Usos { get; set; } = new List<UsoEquipo>();
    }

    [Table("UsosEquipo")]
    public class UsoEquipo
    {
        [Key] public int UsoEquipoId { get; set; }
        public int EquipoId { get; set; }
        public int PacienteId { get; set; }
        public int FisioterapeutaId { get; set; }
        public int? CitaId { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        [Column(TypeName = "decimal(8,2)")] public decimal? Velocidad { get; set; }
        [Column(TypeName = "decimal(8,2)")] public decimal? Inclinacion { get; set; }
        [Required, MaxLength(20)] public string Estado { get; set; } = "PROGRAMADO";
        [MaxLength(1000)] public string Indicaciones { get; set; }
        [MaxLength(1000)] public string Resultado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public EquipoTerapeutico Equipo { get; set; }
        public Paciente Paciente { get; set; }
        public Usuario Fisioterapeuta { get; set; }
        public Cita Cita { get; set; }
    }

    [Table("EvaluacionesIniciales")]
    public class EvaluacionInicial
    {
        [Key] public int EvaluacionInicialId { get; set; }
        public int PacienteId { get; set; }
        public int FisioterapeutaId { get; set; }
        public int? CitaId { get; set; }
        [Required, MaxLength(1000)] public string MotivoConsulta { get; set; }
        [MaxLength(2000)] public string Antecedentes { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? DolorInicial { get; set; }
        [MaxLength(2000)] public string EvaluacionFisica { get; set; }
        [MaxLength(2000)] public string Observaciones { get; set; }
        public DateTime FechaEvaluacion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public Paciente Paciente { get; set; }
        public Usuario Fisioterapeuta { get; set; }
        public Cita Cita { get; set; }
    }

    [Table("Servicios")]
    public class Servicio
    {
        [Key] public int ServicioId { get; set; }
        [Required, MaxLength(150)] public string Nombre { get; set; }
        [MaxLength(500)] public string Descripcion { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal Precio { get; set; }
        [Required, MaxLength(20)] public string Tipo { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
        public ICollection<DetalleFactura> DetallesFactura { get; set; } = new List<DetalleFactura>();
    }

    [Table("Pagos")]
    public class Pago
    {
        [Key] public int PagoId { get; set; }
        public int FacturaId { get; set; }
        [Required, MaxLength(30)] public string MetodoPago { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal Monto { get; set; }
        [Required, MaxLength(20)] public string Estado { get; set; } = "PENDIENTE";
        [MaxLength(100)] public string Referencia { get; set; }
        public DateTime? FechaPago { get; set; }
        public DateTime FechaRegistro { get; set; }
        public Factura Factura { get; set; }
    }

    [Table("TratamientosAplicados")]
    public class TratamientoAplicado
    {
        [Key] public int TratamientoAplicadoId { get; set; }
        [Required, MaxLength(150)] public string Nombre { get; set; }
        [MaxLength(500)] public string Descripcion { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
        public ICollection<SesionTratamiento> Sesiones { get; set; } = new List<SesionTratamiento>();
    }

    [Table("SesionTratamientos")]
    public class SesionTratamiento
    {
        [Key] public int SesionTratamientoId { get; set; }
        public int SesionId { get; set; }
        public int TratamientoAplicadoId { get; set; }
        public int? DuracionMinutos { get; set; }
        [MaxLength(1000)] public string Observaciones { get; set; }
        [MaxLength(1000)] public string Resultado { get; set; }
        public SesionRehabilitacion Sesion { get; set; }
        public TratamientoAplicado TratamientoAplicado { get; set; }
    }

    [Table("EjerciciosRealizados")]
    public class EjercicioRealizado
    {
        [Key] public int EjercicioRealizadoId { get; set; }
        public int TratamientoEjercicioId { get; set; }
        public int? SesionId { get; set; }
        public int PacienteId { get; set; }
        public DateTime FechaRealizacion { get; set; }
        public int? SeriesRealizadas { get; set; }
        public int? RepeticionesRealizadas { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? Dolor { get; set; }
        [Required, MaxLength(20)] public string Estado { get; set; } = "REALIZADO";
        [MaxLength(1000)] public string Comentarios { get; set; }
        public TratamientoEjercicio TratamientoEjercicio { get; set; }
        public SesionRehabilitacion Sesion { get; set; }
        public Paciente Paciente { get; set; }
    }

    [Table("DisponibilidadesFisioterapeuta")]
    public class DisponibilidadFisioterapeuta
    {
        [Key] public int DisponibilidadId { get; set; }
        public int FisioterapeutaId { get; set; }
        public byte DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public bool Estado { get; set; } = true;
        public Usuario Fisioterapeuta { get; set; }
    }
}
