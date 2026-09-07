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
        private readonly IMensajeService _mensajeService;

        public FisioterapeutaController(IPacienteService pacienteService, FisioSaludDbContext context, IMensajeService mensajeService)
        {
            _pacienteService = pacienteService;
            _context = context;
            _mensajeService = mensajeService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var fisioterapeutaId = ObtenerUsuarioAutenticado();
            if (fisioterapeutaId == null) return Forbid();
            var model = await _pacienteService.GetDashboardAsync(fisioterapeutaId.Value);
            return View(model);
        }

        public async Task<IActionResult> Agenda()
        {
            var fisioterapeutaId = ObtenerUsuarioAutenticado();
            if (fisioterapeutaId == null) return Forbid();
            var model = await _pacienteService.GetAgendaAsync(fisioterapeutaId.Value);
            return View(model);
        }

        public async Task<IActionResult> Pacientes(string busqueda, string filtro = "Todos")
        {
            var fisioterapeutaId = ObtenerUsuarioAutenticado();
            if (fisioterapeutaId == null) return Forbid();
            var model = await _pacienteService.GetPacientesAsync(fisioterapeutaId.Value, busqueda, filtro);
            return View(model);
        }

        public async Task<IActionResult> Ejercicios(string busqueda, string categoria = "Todos")
        {
            var fisioterapeutaId = ObtenerUsuarioAutenticado();
            if (fisioterapeutaId == null) return Forbid();
            var model = await _pacienteService.GetEjerciciosAsync(fisioterapeutaId.Value, busqueda, categoria);
            return View(model);
        }

        public async Task<IActionResult> Mensajes(int? pacienteId)
        {
            var userId = ObtenerUsuarioAutenticado();
            if (userId == null) return Forbid();

            var model = await _pacienteService.GetMensajesAsync(userId.Value, pacienteId);
            if (pacienteId.HasValue && model.ConversacionActiva == null) return Forbid();

            var conversaciones = await _mensajeService.ListarConversacionesAsync(userId.Value);
            model.Conversaciones = conversaciones.Select(c => new ChatConversationViewModel
            {
                PacienteId = _context.Pacientes.AsNoTracking().Where(p => p.UsuarioId == c.UsuarioId).Select(p => p.PacienteId).FirstOrDefault(),
                PacienteNombre = c.Nombre,
                Iniciales = c.Iniciales,
                UltimoMensaje = c.UltimoMensaje,
                UltimaHora = c.FechaUltimoMensaje.ToString("g"),
                MensajesNoLeidos = c.NoLeidos
            }).ToList();

            if (pacienteId.HasValue)
            {
                var pacienteUsuarioId = await _context.Pacientes.AsNoTracking()
                    .Where(p => p.PacienteId == pacienteId.Value)
                    .Select(p => (int?)p.UsuarioId).FirstOrDefaultAsync();
                if (pacienteUsuarioId == null || !await _mensajeService.PuedeConversarAsync(userId.Value, pacienteUsuarioId.Value)) return Forbid();
                model.MensajesChat = (await _mensajeService.ObtenerMensajesAsync(userId.Value, pacienteUsuarioId.Value))
                    .Select(m => new ChatMessageItemViewModel { MensajeId = m.MensajeId, EmisorNombre = m.RemitenteNombre, EsSaliente = m.RemitenteId == userId.Value, Contenido = m.Contenido, FechaHora = m.FechaEnvio, Hora = m.FechaEnvio.ToString("g") }).ToList();
                await _mensajeService.MarcarLeidosAsync(userId.Value, pacienteUsuarioId.Value);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarMensaje(int pacienteId, string nuevoMensajeTexto)
        {
            var userId = ObtenerUsuarioAutenticado();
            if (userId == null) return Forbid();
            var destinatarioId = await _context.Pacientes.AsNoTracking().Where(p => p.PacienteId == pacienteId).Select(p => (int?)p.UsuarioId).FirstOrDefaultAsync();
            if (destinatarioId == null) return NotFound();
            var result = await _mensajeService.EnviarAsync(userId.Value, destinatarioId.Value, nuevoMensajeTexto);
            if (!result.Success) TempData["Error"] = result.Error;
            else TempData["Success"] = "Mensaje enviado correctamente.";
            return RedirectToAction(nameof(Mensajes), new { pacienteId });
        }

        private int? ObtenerUsuarioAutenticado()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
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
