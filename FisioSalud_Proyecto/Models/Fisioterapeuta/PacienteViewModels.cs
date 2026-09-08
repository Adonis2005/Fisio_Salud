using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FisioSalud_Proyecto.Models.Fisioterapeuta
{
    public class PacienteListViewModel
    {
        public int PacienteId { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string NombreCompleto => $"{Nombres} {Apellidos}";
        public string Iniciales => $"{(string.IsNullOrEmpty(Nombres) ? "" : Nombres[0].ToString())}{(string.IsNullOrEmpty(Apellidos) ? "" : Apellidos[0].ToString())}".ToUpper();
        public string Identificacion { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public int Edad => DateTime.Today.Year - FechaNacimiento.Year - (DateTime.Today < FechaNacimiento.AddYears(DateTime.Today.Year - FechaNacimiento.Year) ? 1 : 0);
        public string Sexo { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public bool Estado { get; set; } = true;
        public string EstadoTexto => Estado ? "Activo" : "Inactivo";
        public string EstadoBadgeClass => Estado ? "activo" : "riesgo";
        public string Diagnostico { get; set; } = "Sin diagnóstico";
        public int ProgresoPorcentaje { get; set; } = 50;
        public string SesionesTexto { get; set; } = "0/12";
        public string ProximaCitaTexto { get; set; } = "Hoy 10:15";
        public int NivelDolor { get; set; } = 4;
        public bool TieneMedicion { get; set; }
        public string UltimaSesionTexto { get; set; } = "Hoy";
        public DateTime FechaRegistro { get; set; }

        public List<FisioSalud_Proyecto.Models.Entities.EvaluacionInicial> Evaluaciones { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.EvaluacionInicial>();
        public List<FisioSalud_Proyecto.Models.Entities.Diagnostico> DiagnosticosLista { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.Diagnostico>();
        public List<FisioSalud_Proyecto.Models.Entities.PlanTratamiento> PlanesTratamiento { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.PlanTratamiento>();
        public List<FisioSalud_Proyecto.Models.Entities.TratamientoEjercicio> EjerciciosAsignados { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.TratamientoEjercicio>();
        public List<FisioSalud_Proyecto.Models.Entities.Ejercicio> EjerciciosDisponibles { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.Ejercicio>();
        public List<FisioSalud_Proyecto.Models.Entities.SesionRehabilitacion> SesionesLista { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.SesionRehabilitacion>();
        public List<FisioSalud_Proyecto.Models.Entities.Cita> CitasLista { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.Cita>();
    }

    public class PacienteFormViewModel
    {
        public int? PacienteId { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [MaxLength(80)]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [MaxLength(80)]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [MaxLength(20)]
        [Display(Name = "Identificación")]
        public string Identificacion { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime FechaNacimiento { get; set; }

        [Required(ErrorMessage = "Seleccione el sexo.")]
        [Display(Name = "Sexo")]
        public string Sexo { get; set; }

        [MaxLength(200)]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [MaxLength(20)]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [MaxLength(120)]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; }

        [Display(Name = "Estado activo")]
        public bool Estado { get; set; } = true;

        [Required(ErrorMessage = "Seleccione un fisioterapeuta responsable.")]
        [Display(Name = "Fisioterapeuta responsable")]
        public int? FisioterapeutaId { get; set; }

        public List<FisioSalud_Proyecto.Models.Entities.Usuario> FisioterapeutasDisponibles { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.Usuario>();
    }

    public class PacienteFilterViewModel
    {
        public string Busqueda { get; set; }
        public string Filtro { get; set; } = "Todos";
        public List<PacienteListViewModel> Pacientes { get; set; } = new List<PacienteListViewModel>();
        public PacienteListViewModel PacienteSeleccionado { get; set; }
    }

    public class AgendaHoyItemViewModel
    {
        public string Hora { get; set; }
        public string PacienteNombre { get; set; }
        public string Iniciales { get; set; }
        public string AvatarBgColor { get; set; } = "#0284c7";
        public string Diagnostico { get; set; }
        public string EstadoChip { get; set; } // Completada, En curso, Siguiente, Pendiente
        public string ChipClass { get; set; }
        public string Duracion { get; set; }
    }

    public class FisioDashboardViewModel
    {
        public List<EjercicioCardViewModel> Biblioteca { get; set; } = new List<EjercicioCardViewModel>();
        public int TotalPacientes { get; set; }
        public int PacientesActivos { get; set; }
        public int CitasHoy { get; set; }
        public int CitasCompletadasHoy { get; set; }
        public int EjerciciosAsignados { get; set; }
        public int EjerciciosPendientes { get; set; }
        public int MensajesSinLeer { get; set; }
        public int MensajesUrgentes { get; set; }

        public List<AgendaHoyItemViewModel> AgendaHoy { get; set; } = new List<AgendaHoyItemViewModel>();
        public List<PacienteListViewModel> PacientesActivosLista { get; set; } = new List<PacienteListViewModel>();
        public List<PacienteListViewModel> PacientesRecientes { get; set; } = new List<PacienteListViewModel>();
    }

    public class CitaCalendarBlockViewModel
    {
        public int CitaId { get; set; }
        public string Estado { get; set; }
        public string PacienteNombre { get; set; }
        public string Iniciales { get; set; }
        public string Diagnostico { get; set; }
        public string HoraInicio { get; set; }
        public string Duracion { get; set; }
        public string CodigoSesion { get; set; }
        public string ColorClass { get; set; } // pink, cyan, purple, green, yellow
        public int DiaSemana { get; set; } // 1: Lun, 2: Mar, 3: Mier, 4: Jue, 5: Vie, 6: Sab
        public int HoraSlot { get; set; } // 8, 9, 10, 11, 12, 13, 14, 15, 16
    }

    public class FisioAgendaViewModel
    {
        public string RangoFechas { get; set; }
        public DateTime FechaReferencia { get; set; }
        public DateTime SemanaAnterior => FechaReferencia.AddDays(-7);
        public DateTime SemanaSiguiente => FechaReferencia.AddDays(7);
        public List<CitaCalendarBlockViewModel> CitasBloque { get; set; } = new List<CitaCalendarBlockViewModel>();
        public List<FisioSalud_Proyecto.Models.Entities.Paciente> PacientesDisponibles { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.Paciente>();
        public List<FisioSalud_Proyecto.Models.Entities.Servicio> ServiciosDisponibles { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.Servicio>();
    }

    public class FisioNuevaCitaFormViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un paciente.")]
        public int PacienteId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un servicio.")]
        public int ServicioId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Range(15, 240)]
        public int DuracionMinutos { get; set; } = 60;

        [MaxLength(500)]
        public string MotivoConsulta { get; set; }

        [MaxLength(1000)]
        public string Observaciones { get; set; }
    }

    public class EjercicioCardViewModel
    {
        public int EjercicioId { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; } // Columna, Hombro, Caderas, Cuello, Rodilla, Tobillo, Respiracion
        public string Dosificacion { get; set; } // ej. "Columna · 3x15"
        public string Dificultad { get; set; } // Fácil, Moderado, Difícil
        public string DificultadBadgeClass { get; set; }
        public int AsignadosCount { get; set; }
        public int ProgresoPorcentaje { get; set; }
        public string TagHeaderClass { get; set; } // blue, green, purple, yellow, pink, cyan
        public string IconClass { get; set; }
        public string Descripcion { get; set; }
        public string Recomendaciones { get; set; }
    }

    public class PlanAsignacionViewModel
    {
        public int PlanTratamientoId { get; set; }
        public int PacienteId { get; set; }
        public string Etiqueta { get; set; }
    }

    public class FisioEjerciciosViewModel
    {
        public string Busqueda { get; set; }
        public string CategoriaSeleccionada { get; set; } = "Todos";
        public int TotalEjercicios { get; set; }
        public int AsignadosActivos { get; set; }
        public int CategoriasCount { get; set; }
        public int NuevosEsteMes { get; set; }
        public List<EjercicioCardViewModel> Ejercicios { get; set; } = new List<EjercicioCardViewModel>();
        public List<PlanAsignacionViewModel> PlanesDisponibles { get; set; } = new List<PlanAsignacionViewModel>();
    }

    public class ChatMessageItemViewModel
    {
        public int MensajeId { get; set; }
        public string EmisorNombre { get; set; }
        public bool EsSaliente { get; set; } // true: Fisioterapeuta, false: Paciente
        public string Contenido { get; set; }
        public string Hora { get; set; }
        public DateTime FechaHora { get; set; }
    }

    public class ChatConversationViewModel
    {
        public int PacienteId { get; set; }
        public string PacienteNombre { get; set; }
        public string Iniciales { get; set; }
        public string Diagnostico { get; set; }
        public string UltimoMensaje { get; set; }
        public string UltimaHora { get; set; }
        public int MensajesNoLeidos { get; set; }
        public int Edad { get; set; }
        public string SesionesTexto { get; set; }
        public int ProgresoPorcentaje { get; set; }
        public int DolorActual { get; set; }
        public string ProximaCitaTexto { get; set; }
    }

    public class FisioMensajesViewModel
    {
        public List<ChatConversationViewModel> Conversaciones { get; set; } = new List<ChatConversationViewModel>();
        public ChatConversationViewModel ConversacionActiva { get; set; }
        public List<ChatMessageItemViewModel> MensajesChat { get; set; } = new List<ChatMessageItemViewModel>();
        public string NuevoMensajeTexto { get; set; }
    }

    public class FisioConfiguracionViewModel
    {
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Especialidad { get; set; } = "Fisioterapia General";
        public string DireccionConsultorio { get; set; }
        public bool NotificacionesEmail { get; set; } = true;
        public bool NotificacionesCitas { get; set; } = true;
    }
}
