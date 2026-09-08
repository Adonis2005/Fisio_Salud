using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Administrador;
using FisioSalud_Proyecto.Models.Clinical;
using FisioSalud_Proyecto.Models.Fisioterapeuta;
using FisioSalud_Proyecto.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FisioSalud_Proyecto.Controllers.Administrador
{
    [Authorize(Roles = Roles.Administrador)]
    public class AdministradorController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IAdminPanelService _adminPanelService;
        private readonly IPacienteService _pacienteService;
        private readonly IEquipoService _equipoService;

        public AdministradorController(
            IUsuarioService usuarioService,
            IAdminPanelService adminPanelService,
            IPacienteService pacienteService,
            IEquipoService equipoService)
        {
            _usuarioService = usuarioService;
            _adminPanelService = adminPanelService;
            _pacienteService = pacienteService;
            _equipoService = equipoService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var chrome = await _adminPanelService.GetChromeAsync(User.Identity?.Name);
            ViewBag.Chrome = chrome;
            ViewBag.AdminNombre = chrome.NombreAdmin;
            ViewBag.AdminIniciales = chrome.Iniciales;
            ViewBag.AdminFecha = chrome.FechaLarga;
            ViewBag.PacientesActivos = chrome.PacientesActivos;
            ViewBag.TerapeutasActivos = chrome.TerapeutasActivos;
            ViewBag.AlertasCount = chrome.AlertasCount;
            await next();
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewData["Title"] = "Panel de Administración Central";
            return View(await _adminPanelService.GetDashboardAsync());
        }

        public async Task<IActionResult> Clinica()
        {
            ViewData["Title"] = "Gestión de la Clínica";
            return View(await _adminPanelService.GetClinicaAsync());
        }

        public async Task<IActionResult> Terapeutas(string busqueda, int? seleccionadoId)
        {
            ViewData["Title"] = "Terapeutas y Personal";
            return View(await _adminPanelService.GetTerapeutasAsync(busqueda, seleccionadoId));
        }

        public async Task<IActionResult> Pacientes(string busqueda, string filtro)
        {
            ViewData["Title"] = "Todos los Pacientes";
            return View(await _adminPanelService.GetPacientesAsync(busqueda, filtro));
        }

        [HttpGet]
        public async Task<IActionResult> CrearPaciente()
        {
            ViewData["Title"] = "Nuevo paciente";
            ViewData["EsAdministrador"] = true;
            return View("~/Views/Fisioterapeuta/PacienteForm.cshtml", await _pacienteService.GetPacienteFormAsync(null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPaciente(PacienteFormViewModel model)
        {
            ViewData["Title"] = "Nuevo paciente";
            ViewData["EsAdministrador"] = true;
            if (!ModelState.IsValid)
            {
                model.FisioterapeutasDisponibles = (await _pacienteService.GetPacienteFormAsync(null)).FisioterapeutasDisponibles;
                return View("PacienteForm", model);
            }

            var result = await _pacienteService.CreateAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View("PacienteForm", model);
            }

            TempData["Success"] = "Paciente registrado correctamente.";
            return RedirectToAction(nameof(Pacientes));
        }

        [HttpGet]
        public async Task<IActionResult> EditarPaciente(int id)
        {
            var model = await _pacienteService.GetPacienteFormAsync(id);
            if (model == null) return NotFound();
            ViewData["Title"] = "Editar paciente";
            ViewData["EsAdministrador"] = true;
            return View("PacienteForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPaciente(PacienteFormViewModel model)
        {
            ViewData["Title"] = "Editar paciente";
            ViewData["EsAdministrador"] = true;
            if (!ModelState.IsValid)
            {
                model.FisioterapeutasDisponibles = (await _pacienteService.GetPacienteFormAsync(model.PacienteId)).FisioterapeutasDisponibles;
                return View("PacienteForm", model);
            }

            var result = await _pacienteService.UpdateAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View("PacienteForm", model);
            }

            TempData["Success"] = "Paciente actualizado correctamente.";
            return RedirectToAction(nameof(Pacientes));
        }

        public async Task<IActionResult> Planes(string tab)
        {
            ViewData["Title"] = "Planes y Biblioteca";
            return View(await _adminPanelService.GetPlanesAsync(tab));
        }

        public async Task<IActionResult> Reportes()
        {
            ViewData["Title"] = "Generador de Reportes";
            return View("ReportesOperativos", await _adminPanelService.GetReportesAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarReporte(string tipo, DateTime fechaInicio, DateTime fechaFin, string formato, string[] metricas)
        {
            if (fechaInicio == default || fechaFin < fechaInicio || (fechaFin-fechaInicio).TotalDays > 3660)
            { TempData["Error"]="Selecciona un período válido de hasta diez años."; return RedirectToAction(nameof(Reportes)); }
            if (formato == "excel" || formato == "imprimir")
            {
                var reporte = await _adminPanelService.GenerarReporteCsvAsync(tipo, fechaInicio, fechaFin, Array.Empty<string>());
                if (formato == "excel") return File(ReporteExportador.Excel(reporte.Content), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", System.IO.Path.ChangeExtension(reporte.FileName,"xlsx"));
                return View("ReporteImprimible", ReporteExportador.LeerCsv(reporte.Content));
            }
            if (string.Equals(formato, "csv", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(formato))
            {
                var file = await _adminPanelService.GenerarReporteCsvAsync(tipo, fechaInicio, fechaFin, metricas ?? Array.Empty<string>());
                return File(Encoding.UTF8.GetBytes(file.Content), "text/csv; charset=utf-8", file.FileName);
            }

            TempData["Info"] = "Por ahora el exportador genera CSV. PDF y Excel se pueden añadir cuando exista un módulo de documentos.";
            return RedirectToAction(nameof(Reportes));
        }

        public async Task<IActionResult> Analitica(string periodo)
        {
            ViewData["Title"] = "Analítica operativa";
            return View(await _adminPanelService.GetAnaliticaAsync(periodo));
        }

        public async Task<IActionResult> Configuracion(string seccion, string busqueda, int? rolId, bool? estado)
        {
            if (seccion != "usuarios") return RedirectToAction("Index", "Ajustes");
            ViewData["Title"] = "Configuración del Sistema";
            var usuarioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            return View(await _adminPanelService.GetConfiguracionAsync(usuarioId, seccion, busqueda, rolId, estado));
        }

        public async Task<IActionResult> Buscar(string q)
        {
            ViewData["Title"] = "Buscar en el sistema";
            return View(await _adminPanelService.BuscarAsync(q));
        }

        public IActionResult Usuarios(string busqueda, int? rolId, bool? estado)
        {
            return RedirectToAction(nameof(Configuracion), new { seccion = "usuarios", busqueda, rolId, estado });
        }

        public async Task<IActionResult> Servicios()
        {
            ViewData["Title"] = "Servicios y Tarifas";
            return View(await _adminPanelService.GetServiciosAsync());
        }

        public async Task<IActionResult> Equipos()
        {
            ViewData["Title"] = "Equipos terapéuticos";
            return View(await _equipoService.GetAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEquipo(FisioSalud_Proyecto.Models.Entities.EquipoTerapeutico model)
        {
            var result = await _equipoService.GuardarEquipoAsync(model);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Equipo guardado correctamente." : result.Error;
            return RedirectToAction(nameof(Equipos));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoEquipo(int equipoId, string estado)
        {
            var result = await _equipoService.CambiarEstadoEquipoAsync(equipoId, estado);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Estado del equipo actualizado." : result.Error;
            return RedirectToAction(nameof(Equipos));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarUsoEquipo(int usoEquipoId)
        {
            var result = await _equipoService.CambiarEstadoUsoAsync(usoEquipoId, 0, "CANCELADO", administrador: true);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Reserva del equipo cancelada." : result.Error;
            return RedirectToAction(nameof(Equipos));
        }

        public async Task<IActionResult> Citas(string busqueda, string estado, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            ViewData["Title"] = "Gestión de Citas";
            return View(await _adminPanelService.GetCitasAsync(busqueda, estado, fechaDesde, fechaHasta));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoCita(int citaId, string nuevoEstado, string observacion)
        {
            var result = await _adminPanelService.CambiarEstadoCitaAsync(citaId, nuevoEstado, observacion);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Estado de la cita actualizado." : result.Error;
            return RedirectToAction(nameof(Citas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> CancelarCitaAdmin(int citaId) =>
            CambiarEstadoCita(citaId, CitaEstados.Cancelada, "Cancelada desde administración.");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> MarcarNoAsistio(int citaId) =>
            CambiarEstadoCita(citaId, CitaEstados.NoAsistio, "Paciente marcado como no asistió.");

        public async Task<IActionResult> Facturas(string busqueda, string estado, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            ViewData["Title"] = "Facturación y Pagos";
            return View(await _adminPanelService.GetFacturasAsync(busqueda, estado, fechaDesde, fechaHasta));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPagoAdmin(int facturaId, string metodoPago, string referencia)
        {
            var result = await _adminPanelService.RegistrarPagoAdminAsync(facturaId, metodoPago, referencia);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Pago registrado correctamente." : result.Error;
            return RedirectToAction(nameof(Facturas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarFactura(int facturaId)
        {
            var result = await _adminPanelService.CambiarEstadoFacturaAsync(facturaId, PagoEstados.Cancelado);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Factura cancelada." : result.Error;
            return RedirectToAction(nameof(Facturas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReembolsarFactura(int facturaId)
        {
            var result = await _adminPanelService.CambiarEstadoFacturaAsync(facturaId, PagoEstados.Reembolsado);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Factura marcada como reembolsada." : result.Error;
            return RedirectToAction(nameof(Facturas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarServicio(ServicioFormViewModel model)
        {
            var result = await _adminPanelService.SaveServicioAsync(model);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Servicio guardado correctamente." : result.Error;
            return RedirectToAction(nameof(Servicios));
        }

        public async Task<IActionResult> Disponibilidad()
        {
            ViewData["Title"] = "Horarios y Disponibilidad de Terapeutas";
            ViewBag.Terapeutas = await _usuarioService.GetUsuariosPorRolAsync(Roles.Fisioterapeuta);
            return View(await _adminPanelService.GetDisponibilidadesAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarDisponibilidad(DisponibilidadFormViewModel model)
        {
            var result = await _adminPanelService.SaveDisponibilidadAsync(model);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Horario de disponibilidad actualizado." : result.Error;
            return RedirectToAction(nameof(Disponibilidad));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEjercicioCatalogo(FisioSalud_Proyecto.Models.Entities.Ejercicio model)
        {
            var result = await _adminPanelService.SaveEjercicioCatalogoAsync(model);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Ejercicio guardado en catálogo." : result.Error;
            return RedirectToAction(nameof(Planes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEjercicioCatalogo(int ejercicioId)
        {
            var result = await _adminPanelService.ToggleEjercicioCatalogoAsync(ejercicioId);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Estado del ejercicio actualizado." : result.Error;
            return RedirectToAction(nameof(Planes), new { tab = "ejercicios" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPatologia(FisioSalud_Proyecto.Models.Entities.Patologia model)
        {
            var result = await _adminPanelService.SavePatologiaAsync(model);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Patología guardada en catálogo." : result.Error;
            return RedirectToAction(nameof(Planes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePatologia(int patologiaId)
        {
            var result = await _adminPanelService.TogglePatologiaAsync(patologiaId);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Estado de la patología actualizado." : result.Error;
            return RedirectToAction(nameof(Planes), new { tab = "tratamientos" });
        }

        [HttpGet]
        public async Task<IActionResult> CrearUsuario()
        {
            ViewData["Title"] = "Nuevo usuario";
            return View("UsuarioForm", await _usuarioService.GetUsuarioFormAsync(null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(UsuarioFormViewModel model)
        {
            ViewData["Title"] = "Nuevo usuario";
            model.RolesDisponibles = await _usuarioService.GetRolesAsync();

            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "La contraseña es obligatoria al crear un usuario.");

            if (!ModelState.IsValid)
                return View("UsuarioForm", model);

            var result = await _usuarioService.CreateAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View("UsuarioForm", model);
            }

            TempData["Success"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(Configuracion), new { seccion = "usuarios" });
        }

        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            var model = await _usuarioService.GetUsuarioFormAsync(id);
            if (model == null) return NotFound();
            ViewData["Title"] = "Editar usuario";
            return View("UsuarioForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(UsuarioFormViewModel model)
        {
            ViewData["Title"] = "Editar usuario";
            model.RolesDisponibles = await _usuarioService.GetRolesAsync();

            if (!ModelState.IsValid)
                return View("UsuarioForm", model);

            var result = await _usuarioService.UpdateAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View("UsuarioForm", model);
            }

            TempData["Success"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Configuracion), new { seccion = "usuarios" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var result = await _usuarioService.ToggleEstadoAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Estado del usuario actualizado."
                : result.Error;
            return RedirectToAction(nameof(Configuracion), new { seccion = "usuarios" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var result = await _usuarioService.DeleteAsync(id);
            if (!result.Success)
                TempData["Error"] = result.Error;
            else if (result.SoftDelete)
                TempData["Info"] = result.Error;
            else
                TempData["Success"] = "Usuario eliminado correctamente.";

            return RedirectToAction(nameof(Configuracion), new { seccion = "usuarios" });
        }
    }
}
