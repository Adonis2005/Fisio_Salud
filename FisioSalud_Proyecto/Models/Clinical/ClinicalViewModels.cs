using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FisioSalud_Proyecto.Models.Clinical
{
    public class EvaluacionInicialFormModel
    {
        public int PacienteId { get; set; }
        public int? CitaId { get; set; }
        public string PacienteNombre { get; set; }

        [Required(ErrorMessage = "El motivo de consulta es obligatorio.")]
        [Display(Name = "Motivo de consulta")]
        public string MotivoConsulta { get; set; }

        [Display(Name = "Antecedentes médicos/quirúrgicos")]
        public string Antecedentes { get; set; }

        [Range(0, 10, ErrorMessage = "El nivel de dolor debe estar entre 0 y 10.")]
        [Display(Name = "Nivel de dolor inicial (0 - 10)")]
        public decimal? DolorInicial { get; set; }

        [Display(Name = "Evaluación física / postura / rango de movimiento")]
        public string EvaluacionFisica { get; set; }

        [Display(Name = "Observaciones adicionales")]
        public string Observaciones { get; set; }
    }

    public class DiagnosticoFormModel
    {
        public int PacienteId { get; set; }
        public int? PatologiaId { get; set; }
        public string PacienteNombre { get; set; }

        [Required(ErrorMessage = "El texto del diagnóstico es obligatorio.")]
        [Display(Name = "Diagnóstico clínico")]
        public string DiagnosticoTexto { get; set; }

        [Display(Name = "Evaluación funcional")]
        public string EvaluacionFuncional { get; set; }

        [Display(Name = "Observaciones terapéuticas")]
        public string Observaciones { get; set; }
    }

    public class PlanTratamientoFormModel
    {
        public int PacienteId { get; set; }
        public int DiagnosticoId { get; set; }
        public string PacienteNombre { get; set; }

        [Required(ErrorMessage = "El nombre del plan es obligatorio.")]
        [Display(Name = "Nombre del plan de tratamiento")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Los objetivos son obligatorios.")]
        [Display(Name = "Objetivos terapéuticos")]
        public string Objetivos { get; set; }

        [Display(Name = "Duración estimada (semanas)")]
        public int? DuracionEstimada { get; set; }

        [Display(Name = "Fecha de inicio")]
        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Display(Name = "Observaciones")]
        public string Observaciones { get; set; }
    }

    public class AsignarEjercicioFormModel
    {
        public int PlanTratamientoId { get; set; }
        public int EjercicioId { get; set; }
        public string EjercicioNombre { get; set; }

        [Required(ErrorMessage = "Especifique la frecuencia.")]
        [Display(Name = "Frecuencia (ej. 3 veces por semana)")]
        public string Frecuencia { get; set; }

        [Display(Name = "Series")]
        public int? Series { get; set; }

        [Display(Name = "Repeticiones")]
        public int? Repeticiones { get; set; }

        [Display(Name = "Indicaciones / Recomendaciones")]
        public string Observaciones { get; set; }
    }

    public class FinalizarSesionFormModel
    {
        public int CitaId { get; set; }
        public int PlanTratamientoId { get; set; }
        public int PacienteId { get; set; }
        public string PacienteNombre { get; set; }

        [Required(ErrorMessage = "Describa las actividades realizadas en la sesión.")]
        [Display(Name = "Actividades realizadas")]
        public string ActividadesRealizadas { get; set; }

        [Range(0, 10, ErrorMessage = "El dolor debe estar entre 0 y 10.")]
        [Display(Name = "Nivel de dolor actual (0 - 10)")]
        public decimal? NivelDolor { get; set; }

        [Display(Name = "Movilidad articular (%)")]
        public decimal? MovilidadArticular { get; set; }

        [Display(Name = "Fuerza muscular (%)")]
        public decimal? FuerzaMuscular { get; set; }

        [Display(Name = "Grado de recuperación (%)")]
        public decimal? GradoRecuperacion { get; set; }

        [Display(Name = "Observaciones y recomendaciones")]
        public string Observaciones { get; set; }

        [Display(Name = "Resultados alcanzados")]
        public string Resultados { get; set; }
    }

    public class RegistrarPagoFormModel
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; }
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "Seleccione el método de pago.")]
        [Display(Name = "Método de pago")]
        public string MetodoPago { get; set; } // Transferencia, Tarjeta, Efectivo

        [Display(Name = "Número de referencia / Comprobante")]
        public string Referencia { get; set; }
    }

    public class CumplimientoEjercicioFormModel
    {
        public int TratamientoEjercicioId { get; set; }
        public int PacienteId { get; set; }
        public string EjercicioNombre { get; set; }

        [Display(Name = "Series realizadas")]
        public int? SeriesRealizadas { get; set; }

        [Display(Name = "Repeticiones realizadas")]
        public int? RepeticionesRealizadas { get; set; }

        [Range(0, 10, ErrorMessage = "El dolor debe estar entre 0 y 10.")]
        [Display(Name = "Nivel de dolor sentido (0 - 10)")]
        public decimal? Dolor { get; set; }

        [Display(Name = "Comentarios o dificultades")]
        public string Comentarios { get; set; }
    }

    public class EvolucionPacienteViewModel
    {
        public int PacienteId { get; set; }
        public string PacienteNombre { get; set; }
        public string Identificacion { get; set; }
        public decimal? DolorInicial { get; set; }
        public decimal? DolorActual { get; set; }
        public int SesionesRealizadas { get; set; }
        public List<SesionEvolucionItem> Sesiones { get; set; } = new List<SesionEvolucionItem>();
        public List<EjercicioCumplimientoItem> EjerciciosCumplidos { get; set; } = new List<EjercicioCumplimientoItem>();
    }

    public class SesionEvolucionItem
    {
        public int SesionId { get; set; }
        public DateTime Fecha { get; set; }
        public int NumeroSesion { get; set; }
        public string Actividades { get; set; }
        public decimal? Dolor { get; set; }
        public decimal? Movilidad { get; set; }
        public decimal? Fuerza { get; set; }
        public decimal? Recuperacion { get; set; }
        public string Observaciones { get; set; }
    }

    public class EjercicioCumplimientoItem
    {
        public string EjercicioNombre { get; set; }
        public DateTime Fecha { get; set; }
        public int? Series { get; set; }
        public int? Repeticiones { get; set; }
        public decimal? Dolor { get; set; }
        public string Comentarios { get; set; }
    }

    public class ServicioFormViewModel
    {
        public int? ServicioId { get; set; }

        [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, 10000, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El tipo es obligatorio.")]
        public string Tipo { get; set; } = "TERAPIA";

        public bool Estado { get; set; } = true;
    }

    public class DisponibilidadFormViewModel
    {
        public int? DisponibilidadId { get; set; }
        public int FisioterapeutaId { get; set; }
        public byte DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public bool Estado { get; set; } = true;
    }
}
