using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Administrador;
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

        public AdministradorController(
            IUsuarioService usuarioService,
            IAdminPanelService adminPanelService,
            IPacienteService pacienteService)
        {
            _usuarioService = usuarioService;
            _adminPanelService = adminPanelService;
            _pacienteService = pacienteService;
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
            return View("PacienteForm", await _pacienteService.GetPacienteFormAsync(null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPaciente(PacienteFormViewModel model)
        {
            ViewData["Title"] = "Nuevo paciente";
            if (!ModelState.IsValid)
                return View("PacienteForm", model);

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
            return View("PacienteForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPaciente(PacienteFormViewModel model)
        {
            ViewData["Title"] = "Editar paciente";
            if (!ModelState.IsValid)
                return View("PacienteForm", model);

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
            return View(await _adminPanelService.GetReportesAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarReporte(string tipo, DateTime fechaInicio, DateTime fechaFin, string formato, string[] metricas)
        {
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
            ViewData["Title"] = "Configuración del Sistema";
            var usuarioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            return View(await _adminPanelService.GetConfiguracionAsync(usuarioId, seccion, busqueda, rolId, estado));
        }

        public async Task<IActionResult> Buscar(string q)
        {
            ViewData["Title"] = "Buscar en el sistema";
            return View(await _adminPanelService.BuscarAsync(q));
        }

        public async Task<IActionResult> Usuarios(string busqueda, int? rolId, bool? estado)
        {
            return RedirectToAction(nameof(Configuracion), new { seccion = "usuarios", busqueda, rolId, estado });
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
