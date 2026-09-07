using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FisioSalud_Proyecto.Models.Administrador
{
    public class AdminChromeViewModel
    {
        public string NombreAdmin { get; set; }
        public string Iniciales { get; set; }
        public string FechaLarga { get; set; }
        public int PacientesActivos { get; set; }
        public int TerapeutasActivos { get; set; }
        public int AlertasCount { get; set; }
        public int PacientesRiesgo { get; set; }
        public List<AdminAlertaViewModel> Alertas { get; set; } = new List<AdminAlertaViewModel>();
    }

    public class AdminAlertaViewModel
    {
        public string Tipo { get; set; }
        public string Mensaje { get; set; }
        public string CssDot { get; set; }
    }

    public class AdminKpiViewModel
    {
        public string Valor { get; set; }
        public string Titulo { get; set; }
        public string Subtitulo { get; set; }
        public string Icono { get; set; }
        public string Color { get; set; }
    }

    public class AdminDashboardPageViewModel
    {
        public List<AdminKpiViewModel> Kpis { get; set; } = new List<AdminKpiViewModel>();
        public List<AdminCargaTerapeutaViewModel> CargaTerapeutas { get; set; } = new List<AdminCargaTerapeutaViewModel>();
        public List<AdminBarraDiaViewModel> CitasSemana { get; set; } = new List<AdminBarraDiaViewModel>();
        public List<AdminPuntoMesViewModel> ActividadMensual { get; set; } = new List<AdminPuntoMesViewModel>();
        public List<AdminAlertaViewModel> Alertas { get; set; } = new List<AdminAlertaViewModel>();
        public List<AdminCitaRecienteViewModel> ProximasCitas { get; set; } = new List<AdminCitaRecienteViewModel>();
        public List<AdminPlanResumenViewModel> Planes { get; set; } = new List<AdminPlanResumenViewModel>();
        public int MaxCitasDia { get; set; }
        public int MaxActividadMes { get; set; }
    }

    public class AdminCargaTerapeutaViewModel
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string Color { get; set; }
        public int Pacientes { get; set; }
        public int Capacidad { get; set; }
        public int Porcentaje { get; set; }
        public string BarClass { get; set; }
    }

    public class AdminBarraDiaViewModel
    {
        public string Dia { get; set; }
        public int Confirmadas { get; set; }
        public int Pendientes { get; set; }
        public int Canceladas { get; set; }
        public int Total { get; set; }
    }

    public class AdminPuntoMesViewModel
    {
        public string Mes { get; set; }
        public int Atendidas { get; set; }
        public int Programadas { get; set; }
        public int Total { get; set; }
    }

    public class AdminCitaRecienteViewModel
    {
        public string Paciente { get; set; }
        public string Terapeuta { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }
        public string EstadoClass { get; set; }
    }

    public class AdminPlanResumenViewModel
    {
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public int Usos { get; set; }
        public string Actualizado { get; set; }
    }

    public class AdminClinicaPageViewModel
    {
        public int OcupacionPorcentaje { get; set; }
        public int SalasLibres { get; set; }
        public int PacientesEnClinica { get; set; }
        public int CitasHoy { get; set; }
        public List<AdminSalaViewModel> Salas { get; set; } = new List<AdminSalaViewModel>();
        public List<AdminCargaTerapeutaViewModel> CargaHoy { get; set; } = new List<AdminCargaTerapeutaViewModel>();
    }

    public class AdminSalaViewModel
    {
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public string EstadoClass { get; set; }
        public string Paciente { get; set; }
        public string Terapeuta { get; set; }
        public string Horario { get; set; }
        public string Motivo { get; set; }
        public bool PuedeAsignar { get; set; }
    }

    public class AdminTerapeutasPageViewModel
    {
        public string Busqueda { get; set; }
        public int? SeleccionadoId { get; set; }
        public List<AdminTerapeutaRowViewModel> Terapeutas { get; set; } = new List<AdminTerapeutaRowViewModel>();
        public AdminTerapeutaDetalleViewModel Detalle { get; set; }
    }

    public class AdminTerapeutaRowViewModel
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string Color { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Desde { get; set; }
        public int Pacientes { get; set; }
        public int Capacidad { get; set; }
        public int Porcentaje { get; set; }
        public string BarClass { get; set; }
        public int AsistenciaPorcentaje { get; set; }
        public bool Estado { get; set; }
        public string EstadoTexto { get; set; }
        public string EstadoClass { get; set; }
    }

    public class AdminTerapeutaDetalleViewModel
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string Color { get; set; }
        public string Correo { get; set; }
        public int Pacientes { get; set; }
        public int Capacidad { get; set; }
        public int Porcentaje { get; set; }
        public string Desde { get; set; }
        public List<AdminPacienteAsignadoViewModel> PacientesAsignados { get; set; } = new List<AdminPacienteAsignadoViewModel>();
    }

    public class AdminPacienteAsignadoViewModel
    {
        public int PacienteId { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string Color { get; set; }
        public string Diagnostico { get; set; }
        public int Progreso { get; set; }
    }

    public class AdminPacientesPageViewModel
    {
        public string Busqueda { get; set; }
        public string Filtro { get; set; } = "Todos";
        public int Total { get; set; }
        public int Activos { get; set; }
        public int EnRiesgo { get; set; }
        public int Nuevos { get; set; }
        public List<AdminPacienteRowViewModel> Pacientes { get; set; } = new List<AdminPacienteRowViewModel>();
    }

    public class AdminPacienteRowViewModel
    {
        public int PacienteId { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string Color { get; set; }
        public int Edad { get; set; }
        public string Diagnostico { get; set; }
        public string Terapeuta { get; set; }
        public string TerapeutaIniciales { get; set; }
        public int Progreso { get; set; }
        public string BarClass { get; set; }
        public int Sesiones { get; set; }
        public string ProximaCita { get; set; }
        public string Estado { get; set; }
        public string EstadoClass { get; set; }
    }

    public class AdminPlanesPageViewModel
    {
        public string Tab { get; set; } = "ejercicios";
        public int TotalPlanes { get; set; }
        public int UsosMes { get; set; }
        public int TerapeutasActivos { get; set; }
        public List<AdminPlanCardViewModel> Planes { get; set; } = new List<AdminPlanCardViewModel>();
    }

    public class AdminPlanCardViewModel
    {
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public int Usos { get; set; }
        public string Actualizado { get; set; }
        public string Icono { get; set; }
    }

    public class AdminReportesPageViewModel
    {
        public string Tipo { get; set; } = "clinico";
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Formato { get; set; } = "csv";
        public List<string> Metricas { get; set; } = new List<string>();
        public List<AdminReporteHistorialViewModel> Historial { get; set; } = new List<AdminReporteHistorialViewModel>();
    }

    public class AdminReporteHistorialViewModel
    {
        public string Titulo { get; set; }
        public string Fecha { get; set; }
        public string Formato { get; set; }
        public string TipoCss { get; set; }
    }

    public class AdminAnaliticaPageViewModel
    {
        public decimal IngresosMes { get; set; }
        public decimal GastosMes { get; set; }
        public decimal GananciaNeta { get; set; }
        public decimal FacturasPendientesMonto { get; set; }
        public int FacturasAbiertasCount { get; set; }
        public int FacturasPagadasCount { get; set; }
        public int FacturasPendientesCount { get; set; }
        public int FacturasVencidasCount { get; set; }
        public List<AdminFacturaRowViewModel> FacturasRecientes { get; set; } = new List<AdminFacturaRowViewModel>();
        public int CitasMes { get; set; }
        public int CitasMesAnterior { get; set; }
        public int AtendidasMes { get; set; }
        public int CanceladasMes { get; set; }
        public int ProgramadasAbiertas { get; set; }
        public int AsistenciaPorcentaje { get; set; }
        public string VariacionCitas { get; set; }
        public string Periodo { get; set; } = "mes";
        public List<AdminPuntoMesViewModel> Serie { get; set; } = new List<AdminPuntoMesViewModel>();
        public List<AdminSatisfaccionTerapeutaViewModel> Terapeutas { get; set; } = new List<AdminSatisfaccionTerapeutaViewModel>();
        public List<AdminCitaRecienteViewModel> CitasRecientes { get; set; } = new List<AdminCitaRecienteViewModel>();
    }

    public class AdminFacturaRowViewModel
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; }
        public string Paciente { get; set; }
        public string Terapeuta { get; set; }
        public string Fecha { get; set; }
        public decimal Monto { get; set; }
        public string Estado { get; set; }
        public string EstadoClass { get; set; }
    }

    public class AdminSatisfaccionTerapeutaViewModel
    {
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string Color { get; set; }
        public int Asistencia { get; set; }
        public int Pacientes { get; set; }
        public int Citas { get; set; }
    }

    public class AdminConfiguracionPageViewModel
    {
        public string Seccion { get; set; } = "general";
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Rol { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalPacientes { get; set; }
        public int TotalCitas { get; set; }
        public UsuarioFilterViewModel Usuarios { get; set; }
    }

    public class AdminBusquedaPageViewModel
    {
        [Display(Name = "Buscar")]
        public string Q { get; set; }
        public List<AdminBusquedaItemViewModel> Resultados { get; set; } = new List<AdminBusquedaItemViewModel>();
    }

    public class AdminBusquedaItemViewModel
    {
        public string Tipo { get; set; }
        public string Titulo { get; set; }
        public string Subtitulo { get; set; }
        public string Accion { get; set; }
        public string Controlador { get; set; }
        public int? Id { get; set; }
    }
}
