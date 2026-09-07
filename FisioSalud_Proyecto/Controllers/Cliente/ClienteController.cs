using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Models.Cliente;
using FisioSalud_Proyecto.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FisioSalud_Proyecto.Controllers.Cliente
{
    [Authorize(Roles = Roles.Cliente)]
    public class ClienteController : Controller
    {
        private readonly ICitaService _citaService;
        private readonly IAuthService _authService;
        private readonly IMensajeService _mensajeService;
        private readonly FisioSaludDbContext _context;

        public ClienteController(ICitaService citaService, IAuthService authService, IMensajeService mensajeService, FisioSaludDbContext context)
        {
            _citaService = citaService;
            _authService = authService;
            _mensajeService = mensajeService;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = await _citaService.GetDashboardAsync(GetIdentificacion(), GetCorreo(), User.Identity.Name);
            ViewData["Title"] = "Mi Panel de Recuperación";
            return View(model);
        }

        public async Task<IActionResult> Citas(string vista, int? fechaAnio, int? fechaMes, int? fechaDia, int? mesAnio, int? mesMes, int? fisioterapeutaId, string hora, string servicio)
        {
            var fecha = fechaAnio.HasValue && fechaMes.HasValue && fechaDia.HasValue
                ? new System.DateTime(fechaAnio.Value, fechaMes.Value, fechaDia.Value)
                : (System.DateTime?)null;
            var mes = mesAnio.HasValue && mesMes.HasValue
                ? new System.DateTime(mesAnio.Value, mesMes.Value, 1)
                : fecha;

            var model = await _citaService.GetCitasPageAsync(
                GetIdentificacion(), GetCorreo(), User.Identity.Name,
                fecha, fisioterapeutaId, hora, servicio, vista, mes);

            ViewData["Title"] = "Mis Citas";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgendarCita(NuevaCitaFormViewModel formulario)
        {
            var result = await _citaService.AgendarCitaAsync(GetIdentificacion(), GetCorreo(), formulario);
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Citas), new
                {
                    vista = "nueva",
                    fechaAnio = formulario.Fecha.Year,
                    fechaMes = formulario.Fecha.Month,
                    fechaDia = formulario.Fecha.Day,
                    fisioterapeutaId = formulario.FisioterapeutaId,
                    hora = formulario.Hora,
                    servicio = formulario.Servicio
                });
            }

            TempData["Success"] = "Tu cita quedó confirmada y registrada en el sistema.";
            return RedirectToAction(nameof(Citas), new { vista = "lista" });
        }

        public async Task<IActionResult> Progreso()
        {
            var model = await _citaService.GetProgresoAsync(GetIdentificacion(), GetCorreo(), User.Identity.Name);
            ViewData["Title"] = "Mi Progreso";
            return View(model);
        }

        public async Task<IActionResult> Historial(string filtro, int? citaId)
        {
            var model = await _citaService.GetHistorialAsync(GetIdentificacion(), GetCorreo(), User.Identity.Name, filtro, citaId);
            ViewData["Title"] = "Mi Historial";
            return View(model);
        }

        public async Task<IActionResult> Mensajes(int? fisioterapeutaId)
        {
            var usuarioId = ObtenerUsuarioAutenticado();
            if (usuarioId == null) return Forbid();
            var model = await _citaService.GetMensajesAsync(GetIdentificacion(), GetCorreo(), User.Identity.Name, fisioterapeutaId);
            var conversaciones = await _mensajeService.ListarConversacionesAsync(usuarioId.Value);
            model.Conversaciones = conversaciones.Select(c => new ConversacionResumenViewModel
            {
                FisioterapeutaId = c.UsuarioId,
                Nombre = c.Nombre,
                Iniciales = c.Iniciales,
                UltimaNota = c.UltimoMensaje,
                UltimaFecha = c.FechaUltimoMensaje
            }).ToList();
            if (fisioterapeutaId.HasValue)
            {
                if (!await _mensajeService.PuedeConversarAsync(usuarioId.Value, fisioterapeutaId.Value)) return Forbid();
                model.Mensajes = (await _mensajeService.ObtenerMensajesAsync(usuarioId.Value, fisioterapeutaId.Value))
                    .Select(m => new MensajeClienteViewModel { RemitenteId = m.RemitenteId, RemitenteNombre = m.RemitenteNombre, Contenido = m.Contenido, FechaEnvio = m.FechaEnvio, EsSaliente = m.RemitenteId == usuarioId.Value }).ToList();
                await _mensajeService.MarcarLeidosAsync(usuarioId.Value, fisioterapeutaId.Value);
            }
            ViewData["Title"] = "Mensajes";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarMensaje(int fisioterapeutaId, string nuevoMensaje)
        {
            var usuarioId = ObtenerUsuarioAutenticado();
            if (usuarioId == null) return Forbid();
            var result = await _mensajeService.EnviarAsync(usuarioId.Value, fisioterapeutaId, nuevoMensaje);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Mensaje enviado correctamente." : result.Error;
            return RedirectToAction(nameof(Mensajes), new { fisioterapeutaId });
        }

        public async Task<IActionResult> Perfil(string tab)
        {
            var model = await _citaService.GetPerfilAsync(GetIdentificacion(), GetCorreo(), User.Identity.Name);
            model.Tab = string.IsNullOrWhiteSpace(tab) ? "personal" : tab;
            ViewData["Title"] = "Mi Perfil";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(PerfilFormViewModel model)
        {
            ViewData["Title"] = "Mi Perfil";
            model.Tab = "personal";

            if (!ModelState.IsValid)
            {
                var fresh = await _citaService.GetPerfilAsync(GetIdentificacion(), GetCorreo(), User.Identity.Name);
                model.NombreCompleto = fresh.NombreCompleto;
                model.Iniciales = fresh.Iniciales;
                model.FechaHoyFormato = fresh.FechaHoyFormato;
                model.SemanaRehabilitacion = fresh.SemanaRehabilitacion;
                model.Activo = fresh.Activo;
                model.SesionesCompletadas = fresh.SesionesCompletadas;
                model.SesionesTotales = fresh.SesionesTotales;
                model.ProgresoPorcentaje = fresh.ProgresoPorcentaje;
                model.DiasActivo = fresh.DiasActivo;
                model.TerapeutaAsignado = fresh.TerapeutaAsignado;
                model.TerapeutaIniciales = fresh.TerapeutaIniciales;
                model.TerapeutaDesde = fresh.TerapeutaDesde;
                model.ProximaCita = fresh.ProximaCita;
                model.Identificacion = fresh.Identificacion;
                return View(model);
            }

            var result = await _citaService.UpdatePerfilAsync(GetIdentificacion(), GetCorreo(), model);
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Perfil));
            }

            TempData["Success"] = "Tu perfil se actualizó en Pacientes y Usuarios.";
            return RedirectToAction(nameof(Perfil));
        }

        public async Task<IActionResult> Configuracion()
        {
            var model = await _citaService.GetConfiguracionAsync(GetIdentificacion(), GetCorreo(), User.Identity.Name);
            ViewData["Title"] = "Configuración";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambioPasswordViewModel password)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Revisa los datos de la contraseña.";
                return RedirectToAction(nameof(Configuracion));
            }

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _authService.ChangePasswordAsync(usuarioId, password.PasswordActual, password.PasswordNueva);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Contraseña actualizada correctamente."
                : result.Error;
            return RedirectToAction(nameof(Configuracion));
        }

        public async Task<IActionResult> Facturas()
        {
            var model = await _citaService.GetFacturasClienteAsync(GetIdentificacion(), GetCorreo(), User.Identity.Name);
            ViewData["Title"] = "Mis Facturas";
            return View(model);
        }

        private string GetIdentificacion() => User.FindFirstValue("Identificacion");
        private string GetCorreo() => User.FindFirstValue(ClaimTypes.Email);
    }
}
