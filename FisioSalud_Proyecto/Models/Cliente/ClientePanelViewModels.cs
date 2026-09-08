using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FisioSalud_Proyecto.Models.Cliente
{
    public class ClientePageViewModel
    {
        public string NombreCompleto { get; set; }
        public string Iniciales { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public bool Activo { get; set; }
        public int SemanaRehabilitacion { get; set; }
        public string FechaHoyFormato { get; set; }
        public bool TieneExpediente { get; set; }
        public int PacienteId { get; set; }
    }

    public class SemanaChartPunto
    {
        public string Etiqueta { get; set; }
        public int Atendidas { get; set; }
        public int Programadas { get; set; }
    }

    public class ClienteDashboardViewModel : ClientePageViewModel
    {
        public int TotalCitas { get; set; }
        public int CitasProgramadas { get; set; }
        public int CitasAtendidas { get; set; }
        public int CitasCanceladas { get; set; }
        public int AsistenciaPorcentaje { get; set; }
        public CitaClienteViewModel UltimaCita { get; set; }
        public CitaClienteViewModel ProximaCita { get; set; }
        public List<CitaClienteViewModel> ProximasCitas { get; set; } = new List<CitaClienteViewModel>();
        public List<CitaClienteViewModel> CitasRecientes { get; set; } = new List<CitaClienteViewModel>();
        public List<SemanaChartPunto> EvolucionSemanal { get; set; } = new List<SemanaChartPunto>();
        public string TerapeutaAsignado { get; set; }
        public string TerapeutaIniciales { get; set; }
        public string UltimaNotaClinica { get; set; }
        public DateTime? FechaInicioTratamiento { get; set; }
    }

    public class EspecialistaOpcionViewModel
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; }
        public string Iniciales { get; set; }
        public string Especialidad { get; set; }
    }

    public class HorarioSlotViewModel
    {
        public string Hora { get; set; }
        public bool Ocupado { get; set; }
        public bool Seleccionado { get; set; }
    }

    public class DiaCalendarioViewModel
    {
        public int Dia { get; set; }
        public DateTime Fecha { get; set; }
        public bool FueraDeMes { get; set; }
        public bool EsHoy { get; set; }
        public bool Seleccionado { get; set; }
        public bool TieneDisponibilidad { get; set; }
        public bool EsPasado { get; set; }
    }

    public class NuevaCitaFormViewModel
    {
        public DateTime Fecha { get; set; }
        public int? FisioterapeutaId { get; set; }
        public string Hora { get; set; }
        public string Servicio { get; set; }
        public string Mensaje { get; set; }
    }

    public class CitasPageViewModel : ClientePageViewModel
    {
        public string Vista { get; set; } = "nueva";
        public NuevaCitaFormViewModel Formulario { get; set; } = new NuevaCitaFormViewModel();
        public List<CitaClienteViewModel> Citas { get; set; } = new List<CitaClienteViewModel>();
        public List<EspecialistaOpcionViewModel> Especialistas { get; set; } = new List<EspecialistaOpcionViewModel>();
        public List<DiaCalendarioViewModel> Calendario { get; set; } = new List<DiaCalendarioViewModel>();
        public List<HorarioSlotViewModel> Horarios { get; set; } = new List<HorarioSlotViewModel>();
        public List<string> Servicios { get; set; } = new List<string>();
        public string MesEtiqueta { get; set; }
        public DateTime MesVisible { get; set; }
        public string EspecialistaNombre { get; set; }
        public string Consultorio { get; set; } = "Consultorio 2";
    }

    public class HistorialPageViewModel : ClientePageViewModel
    {
        public string Filtro { get; set; } = "TODAS";
        public int? CitaSeleccionadaId { get; set; }
        public List<CitaClienteViewModel> Citas { get; set; } = new List<CitaClienteViewModel>();
        public CitaDetalleViewModel Detalle { get; set; }
        public int HorasTerapia { get; set; }
        public int SemanasPlan { get; set; }
        public int CitasAtendidas { get; set; }
        public int CitasProgramadas { get; set; }
        public int CitasCanceladas { get; set; }
        public int AsistenciaPorcentaje { get; set; }
    }

    public class CitaDetalleViewModel
    {
        public int CitaId { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
        public string Fisioterapeuta { get; set; }
        public string FisioterapeutaIniciales { get; set; }
        public string MotivoConsulta { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }
        public int DuracionMinutos { get; set; }
    }

    public class ProgresoPageViewModel : ClientePageViewModel
    {
        public List<FisioSalud_Proyecto.Models.Entities.TratamientoEjercicio> Rutina { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.TratamientoEjercicio>();
        public List<FisioSalud_Proyecto.Models.Entities.PlanTratamiento> Planes { get; set; } = new List<FisioSalud_Proyecto.Models.Entities.PlanTratamiento>();
        public FisioSalud_Proyecto.Models.Clinical.EvolucionPacienteViewModel Evolucion { get; set; } = new FisioSalud_Proyecto.Models.Clinical.EvolucionPacienteViewModel();
        public int CitasAtendidas { get; set; }
        public int CitasProgramadas { get; set; }
        public int TotalCitas { get; set; }
        public int AsistenciaPorcentaje { get; set; }
        public int CompletadasPorcentaje { get; set; }
        public List<SemanaChartPunto> EvolucionSemanal { get; set; } = new List<SemanaChartPunto>();
        public List<CitaClienteViewModel> CitasRecientes { get; set; } = new List<CitaClienteViewModel>();
        public DateTime? FechaInicioTratamiento { get; set; }
    }

    public class ConversacionResumenViewModel
    {
        public int FisioterapeutaId { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string UltimoMotivo { get; set; }
        public DateTime? UltimaFecha { get; set; }
        public string UltimaNota { get; set; }
        public bool Seleccionada { get; set; }
    }

    public class MensajesPageViewModel : ClientePageViewModel
    {
        public List<ConversacionResumenViewModel> Conversaciones { get; set; } = new List<ConversacionResumenViewModel>();
        public ConversacionResumenViewModel ConversacionActiva { get; set; }
        public List<MensajeClienteViewModel> Mensajes { get; set; } = new List<MensajeClienteViewModel>();
        public string NuevoMensaje { get; set; }
    }

    public class MensajeClienteViewModel
    {
        public int RemitenteId { get; set; }
        public string RemitenteNombre { get; set; }
        public string Contenido { get; set; }
        public DateTime FechaEnvio { get; set; }
        public bool EsSaliente { get; set; }
    }

    public class PerfilFormViewModel : ClientePageViewModel
    {
        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [MaxLength(80)]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [MaxLength(80)]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress]
        [MaxLength(120)]
        public string CorreoPerfil { get; set; }

        [MaxLength(20)]
        public string TelefonoPerfil { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        [Required]
        [MaxLength(1)]
        public string Sexo { get; set; }

        [MaxLength(200)]
        public string Direccion { get; set; }

        public string Identificacion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string TerapeutaAsignado { get; set; }
        public string TerapeutaIniciales { get; set; }
        public DateTime? TerapeutaDesde { get; set; }
        public int SesionesCompletadas { get; set; }
        public int SesionesTotales { get; set; }
        public int ProgresoPorcentaje { get; set; }
        public int DiasActivo { get; set; }
        public CitaClienteViewModel ProximaCita { get; set; }
        public string Tab { get; set; } = "personal";
    }

    public class CambioPasswordViewModel
    {
        [Required(ErrorMessage = "Ingrese su contraseña actual.")]
        [DataType(DataType.Password)]
        public string PasswordActual { get; set; }

        [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe incluir mayúsculas, minúsculas y números.")]
        [DataType(DataType.Password)]
        public string PasswordNueva { get; set; }

        [Required]
        [Compare(nameof(PasswordNueva), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string PasswordConfirmar { get; set; }
    }

    public class ConfiguracionPageViewModel : ClientePageViewModel
    {
        public CambioPasswordViewModel Password { get; set; } = new CambioPasswordViewModel();
        public string FechaRegistroFormato { get; set; }
    }

    public class FacturasPageViewModel : ClientePageViewModel
    {
        public List<FacturaClienteItemViewModel> Facturas { get; set; } = new List<FacturaClienteItemViewModel>();
        public decimal TotalPagado { get; set; }
        public decimal TotalPendiente { get; set; }
    }

    public class FacturaClienteItemViewModel
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime Fecha { get; set; }
        public string Fisioterapeuta { get; set; }
        public decimal Monto { get; set; }
        public string Estado { get; set; }
        public string EstadoClass { get; set; }
    }
}
