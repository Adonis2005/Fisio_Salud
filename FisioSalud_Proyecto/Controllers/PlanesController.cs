using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Entities;
using FisioSalud_Proyecto.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Controllers
{
    [Authorize(Roles = Roles.Administrador + "," + Roles.Fisioterapeuta)]
    public class PlanesController : Controller
    {
        private readonly FisioSaludDbContext db;
        private readonly IAdminPanelService admin;
        public PlanesController(FisioSaludDbContext db, IAdminPanelService admin) { this.db = db; this.admin = admin; }
        private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        private IQueryable<PlanTratamiento> Accesibles() => db.PlanesTratamiento.Where(p => User.IsInRole(Roles.Administrador) || p.FisioterapeutaId == UsuarioId);
        public async Task<IActionResult> Index(string busqueda, string estado)
        {
            if (User.IsInRole(Roles.Administrador)) ViewBag.Chrome = await admin.GetChromeAsync(User.Identity.Name);
            var query = Accesibles().Include(p => p.Paciente).Include(p => p.Fisioterapeuta).Include(p => p.Sesiones).AsNoTracking();
            if (!string.IsNullOrWhiteSpace(busqueda)) query = query.Where(p => p.Nombre.Contains(busqueda) || p.Paciente.Nombres.Contains(busqueda) || p.Paciente.Apellidos.Contains(busqueda));
            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(p => p.Estado == estado);
            ViewBag.Busqueda = busqueda; ViewBag.Estado = estado;
            ViewBag.Diagnosticos = await db.Diagnosticos.Include(d => d.Paciente).Where(d => d.Estado && d.FisioterapeutaId == UsuarioId).ToListAsync();
            return View(await query.OrderByDescending(p => p.FechaCreacion).ToListAsync());
        }
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Fisioterapeuta)]
        public async Task<IActionResult> Guardar(int planTratamientoId, int diagnosticoId, string nombre, string objetivos, int duracionEstimada, DateTime fechaInicio)
        {
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 150 || string.IsNullOrWhiteSpace(objetivos) || objetivos.Length > 1000 || duracionEstimada < 1 || duracionEstimada > 104 || fechaInicio == default)
            { TempData["Error"] = "Completa nombre, objetivos, fecha y duración (1 a 104 semanas)."; return RedirectToAction(nameof(Index)); }
            var diagnostico = await db.Diagnosticos.FirstOrDefaultAsync(d => d.DiagnosticoId == diagnosticoId && d.FisioterapeutaId == UsuarioId && d.Estado);
            if (diagnostico == null) return Forbid();
            var plan = planTratamientoId > 0 ? await Accesibles().FirstOrDefaultAsync(p => p.PlanTratamientoId == planTratamientoId && p.FisioterapeutaId == UsuarioId) : null;
            if (planTratamientoId > 0 && plan == null) return NotFound();
            if (plan != null && (plan.Estado == "COMPLETADO" || plan.Estado == "CANCELADO" || plan.PacienteId != diagnostico.PacienteId))
            { TempData["Error"] = "Un plan cerrado no se edita ni se transfiere a otro paciente."; return RedirectToAction(nameof(Index)); }
            if (plan == null) { plan = new PlanTratamiento { PacienteId = diagnostico.PacienteId, FisioterapeutaId = UsuarioId, Estado = "ACTIVO", FechaCreacion = DateTime.Now }; db.PlanesTratamiento.Add(plan); }
            plan.DiagnosticoId = diagnosticoId; plan.Nombre = nombre.Trim(); plan.Objetivos = objetivos.Trim(); plan.DuracionEstimada = duracionEstimada; plan.FechaInicio = fechaInicio.Date; plan.FechaFinEstimada = fechaInicio.Date.AddDays(duracionEstimada * 7); plan.FechaActualizacion = DateTime.Now;
            await db.SaveChangesAsync(); TempData["Success"] = "Plan guardado. Ya puedes asignarle ejercicios desde el expediente."; return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Fisioterapeuta)]
        public async Task<IActionResult> Estado(int id, string estado)
        {
            var plan = await Accesibles().FirstOrDefaultAsync(p => p.PlanTratamientoId == id && p.FisioterapeutaId == UsuarioId);
            if (plan == null) return NotFound();
            if (!new[] { "ACTIVO", "PAUSADO", "COMPLETADO", "CANCELADO" }.Contains(estado) || plan.Estado == "COMPLETADO" || plan.Estado == "CANCELADO")
            { TempData["Error"] = "La transición solicitada no está permitida."; return RedirectToAction(nameof(Index)); }
            if (estado == "COMPLETADO" && !await db.SesionesRehabilitacion.AnyAsync(s => s.PlanTratamientoId == id))
            { TempData["Error"] = "Registra al menos una sesión antes de dar el alta del plan."; return RedirectToAction(nameof(Index)); }
            plan.Estado = estado; plan.FechaActualizacion = DateTime.Now;
            await db.SaveChangesAsync(); TempData["Success"] = "Estado del plan actualizado."; return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Fisioterapeuta)]
        public async Task<IActionResult> RetirarEjercicio(int id)
        {
            var asignacion = await db.TratamientoEjercicios.Include(t => t.PlanTratamiento).FirstOrDefaultAsync(t => t.TratamientoEjercicioId == id && t.PlanTratamiento.FisioterapeutaId == UsuarioId);
            if (asignacion == null) return NotFound();
            asignacion.Estado = false; await db.SaveChangesAsync(); TempData["Success"] = "Ejercicio retirado de la rutina. Su historial se conserva.";
            return RedirectToAction("VerPaciente", "Fisioterapeuta", new { id = asignacion.PlanTratamiento.PacienteId });
        }
    }
}
