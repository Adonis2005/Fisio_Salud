using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Fisioterapeuta;
using FisioSalud_Proyecto.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Controllers.Fisioterapeuta
{
    [Authorize(Roles = Roles.Fisioterapeuta)]
    public class FisioterapeutaController : Controller
    {
        private readonly IPacienteService _pacienteService;
        private readonly FisioSaludDbContext _context;

        public FisioterapeutaController(IPacienteService pacienteService, FisioSaludDbContext context)
        {
            _pacienteService = pacienteService;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = await _pacienteService.GetDashboardAsync();
            return View(model);
        }

        public async Task<IActionResult> Agenda()
        {
            var model = await _pacienteService.GetAgendaAsync();
            return View(model);
        }

        public async Task<IActionResult> Pacientes(string busqueda, string filtro = "Todos")
        {
            var model = await _pacienteService.GetPacientesAsync(busqueda, filtro);
            return View(model);
        }

        public async Task<IActionResult> Ejercicios(string busqueda, string categoria = "Todos")
        {
            var model = await _pacienteService.GetEjerciciosAsync(busqueda, categoria);
            return View(model);
        }

        public async Task<IActionResult> Mensajes(int? pacienteId)
        {
            var model = await _pacienteService.GetMensajesAsync(pacienteId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuardarMensaje(int pacienteId, string nuevoMensajeTexto)
        {
            TempData["Success"] = "Mensaje enviado correctamente.";
            return RedirectToAction(nameof(Mensajes), new { pacienteId });
        }

        public async Task<IActionResult> Configuracion()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = new FisioConfiguracionViewModel();

            if (int.TryParse(userIdStr, out int userId))
            {
                var usuario = await _context.Usuarios.FindAsync(userId);
                if (usuario != null)
                {
                    model.Nombres = usuario.Nombres;
                    model.Apellidos = usuario.Apellidos;
                    model.Correo = usuario.Correo;
                    model.Telefono = usuario.Telefono;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Configuracion(FisioConfiguracionViewModel model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var usuario = await _context.Usuarios.FindAsync(userId);
                if (usuario != null)
                {
                    if (!string.IsNullOrWhiteSpace(model.Nombres)) usuario.Nombres = model.Nombres.Trim();
                    if (!string.IsNullOrWhiteSpace(model.Apellidos)) usuario.Apellidos = model.Apellidos.Trim();
                    if (!string.IsNullOrWhiteSpace(model.Correo)) usuario.Correo = model.Correo.Trim().ToLower();
                    usuario.Telefono = model.Telefono?.Trim();
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Configuración actualizada correctamente.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CrearPaciente()
        {
            var model = await _pacienteService.GetPacienteFormAsync(null);
            return View("PacienteForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPaciente(PacienteFormViewModel model)
        {
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
            return View("PacienteForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPaciente(PacienteFormViewModel model)
        {
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

        [HttpGet]
        public async Task<IActionResult> VerPaciente(int id)
        {
            var model = await _pacienteService.GetPacienteDetalleAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }
    }
}
