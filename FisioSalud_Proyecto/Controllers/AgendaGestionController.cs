using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Cliente;
using FisioSalud_Proyecto.Models.Entities;
using FisioSalud_Proyecto.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Controllers
{
    public class GestionAgendaViewModel : ClientePageViewModel
    {
        public List<Cita> Reservas { get; set; } = new List<Cita>();
        public List<Paciente> Pacientes { get; set; } = new List<Paciente>();
        public List<Usuario> Terapeutas { get; set; } = new List<Usuario>();
        public List<Servicio> Servicios { get; set; } = new List<Servicio>();
    }
    [Authorize(Roles = Roles.Administrador + "," + Roles.Fisioterapeuta + "," + Roles.Cliente)]
    public class AgendaGestionController : Controller
    {
        private readonly FisioSaludDbContext db;
        private readonly IAdminPanelService admin;
        private readonly ICitaService citas;
        public AgendaGestionController(FisioSaludDbContext db, IAdminPanelService admin, ICitaService citas) { this.db=db; this.admin=admin; this.citas=citas; }
        private int Id => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        private IQueryable<Cita> Accesibles()
        {
            var query = db.Citas.AsQueryable();
            if(User.IsInRole(Roles.Fisioterapeuta)) query=query.Where(c=>c.FisioterapeutaId==Id);
            else if(User.IsInRole(Roles.Cliente)) query=query.Where(c=>c.Paciente.UsuarioId==Id);
            return query;
        }
        public async Task<IActionResult> Index()
        {
            var usuario=await db.Usuarios.FindAsync(Id);
            var m=new GestionAgendaViewModel { NombreCompleto=usuario.NombreCompleto, Correo=usuario.Correo, Iniciales=string.Concat(usuario.Nombres.Take(1))+string.Concat(usuario.Apellidos.Take(1)), Activo=usuario.Estado, FechaHoyFormato=DateTime.Today.ToString("dd/MM/yyyy") };
            var paciente=await db.Pacientes.FirstOrDefaultAsync(p=>p.UsuarioId==Id); m.PacienteId=paciente?.PacienteId ?? 0; m.TieneExpediente=paciente!=null;
            m.Reservas=await Accesibles().AsNoTracking().Include(c=>c.Paciente).Include(c=>c.Fisioterapeuta).Where(c=>c.Estado==CitaEstados.Solicitada || c.Estado==CitaEstados.PendientePago || c.Estado==CitaEstados.Confirmada || c.Estado==CitaEstados.Programada).OrderBy(c=>c.Fecha).ThenBy(c=>c.HoraInicio).ToListAsync();
            if(User.IsInRole(Roles.Administrador))
            {
                ViewBag.Chrome=await admin.GetChromeAsync(usuario.NombreCompleto);
                m.Pacientes=await db.Pacientes.Where(p=>p.Estado).OrderBy(p=>p.Apellidos).ToListAsync();
                m.Terapeutas=await db.Usuarios.Where(u=>u.Estado && u.Rol.Nombre==Roles.Fisioterapeuta).OrderBy(u=>u.Apellidos).ToListAsync();
                m.Servicios=await db.Servicios.Where(s=>s.Estado).OrderBy(s=>s.Nombre).ToListAsync();
            }
            return View(m);
        }
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles=Roles.Administrador)]
        public async Task<IActionResult> Crear(int pacienteId, NuevaCitaFormViewModel formulario)
        {
            var paciente=await db.Pacientes.FirstOrDefaultAsync(p=>p.PacienteId==pacienteId && p.Estado);
            if(paciente==null || !ModelState.IsValid) { TempData["Error"]="Revisa los datos del paciente y horario."; return RedirectToAction(nameof(Index)); }
            var r=await citas.AgendarCitaAsync(paciente.Identificacion,paciente.Correo,formulario);
            TempData[r.Success?"Success":"Error"]=r.Success?"Cita creada y factura pendiente generada. Registra el cobro en Facturación.":r.Error;
            return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reprogramar(int citaId, DateTime fecha, TimeSpan hora, string motivo)
        {
            if(!ModelState.IsValid || fecha==default || hora<TimeSpan.Zero || hora>=TimeSpan.FromDays(1) || fecha.Date.Add(hora)<=DateTime.Now || string.IsNullOrWhiteSpace(motivo) || motivo.Length>250)
            {TempData["Error"]="Indica un horario futuro y un motivo de hasta 250 caracteres."; return RedirectToAction(nameof(Index));}
            await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var cita=await Accesibles().FirstOrDefaultAsync(c=>c.CitaId==citaId);
            if(cita==null) return NotFound();
            if(!new[]{CitaEstados.Solicitada,CitaEstados.PendientePago,CitaEstados.Confirmada,CitaEstados.Programada}.Contains(cita.Estado))
            {TempData["Error"]="Solo se reprograman citas pendientes o confirmadas, no atenciones cerradas.";return RedirectToAction(nameof(Index));}
            var fin=hora.Add(cita.HoraFin.HasValue ? cita.HoraFin.Value-cita.HoraInicio : TimeSpan.FromHours(1));
            byte dia=(byte)(fecha.DayOfWeek==DayOfWeek.Sunday?7:(int)fecha.DayOfWeek);
            if(!await db.DisponibilidadesFisioterapeuta.AnyAsync(d=>d.FisioterapeutaId==cita.FisioterapeutaId && d.Estado && d.DiaSemana==dia && hora>=d.HoraInicio && fin<=d.HoraFin))
            {TempData["Error"]="El horario está fuera de la disponibilidad del terapeuta.";return RedirectToAction(nameof(Index));}
            var otras=await db.Citas.Where(c=>c.CitaId!=citaId && c.Fecha==fecha.Date && c.Estado!=CitaEstados.Cancelada && (c.FisioterapeutaId==cita.FisioterapeutaId || c.PacienteId==cita.PacienteId)).ToListAsync();
            if(otras.Any(c=>c.HoraInicio<fin && hora<(c.HoraFin ?? c.HoraInicio.Add(TimeSpan.FromHours(1)))))
            {TempData["Error"]="El terapeuta o el paciente ya tiene otra cita en ese horario.";return RedirectToAction(nameof(Index));}
            if(await db.UsosEquipo.AnyAsync(u=>u.CitaId==citaId && (u.Estado=="PROGRAMADO" || u.Estado=="EN_USO")))
            {TempData["Error"]="Cancela primero la reserva de equipo vinculada para no dejar horarios inconsistentes.";return RedirectToAction(nameof(Index));}
            cita.Observaciones=(cita.Observaciones ?? "")+$"\nReprogramada de {cita.Fecha:dd/MM/yyyy} {cita.HoraInicio:hh\\:mm} por usuario {Id}: {motivo.Trim()}";
            if(cita.Observaciones.Length>1000) {TempData["Error"]="El historial de observaciones está lleno; contacta administración.";return RedirectToAction(nameof(Index));}
            cita.Fecha=fecha.Date;cita.HoraInicio=hora;cita.HoraFin=fin;cita.FechaActualizacion=DateTime.Now;
            await db.SaveChangesAsync();await transaction.CommitAsync();TempData["Success"]="Cita reprogramada. Se conserva su factura y el pago existente.";return RedirectToAction(nameof(Index));
        }
    }
}
