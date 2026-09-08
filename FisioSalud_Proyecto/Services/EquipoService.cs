using System;
using System.Linq;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Clinical;
using FisioSalud_Proyecto.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Services
{
    public interface IEquipoService
    {
        Task<EquiposPageViewModel> GetAsync(int? fisioterapeutaId = null);
        Task<(bool Success, string Error)> GuardarEquipoAsync(EquipoTerapeutico model);
        Task<(bool Success, string Error)> CambiarEstadoEquipoAsync(int equipoId, string estado);
        Task<(bool Success, string Error)> ProgramarAsync(ProgramarUsoEquipoFormModel model, int fisioterapeutaId);
        Task<(bool Success, string Error)> CambiarEstadoUsoAsync(int usoEquipoId, int fisioterapeutaId, string estado, string resultado = null, bool administrador = false);
    }

    public class EquipoService : IEquipoService
    {
        private readonly FisioSaludDbContext _context;
        public EquipoService(FisioSaludDbContext context) => _context = context;

        public async Task<EquiposPageViewModel> GetAsync(int? fisioterapeutaId = null)
        {
            var hoy = DateTime.Today;
            var usosQuery = _context.UsosEquipo.AsNoTracking().Include(u => u.Equipo).Include(u => u.Paciente).Include(u => u.Fisioterapeuta).Where(u => u.Fecha >= hoy.AddDays(-7));
            if (fisioterapeutaId.HasValue) usosQuery = usosQuery.Where(u => u.FisioterapeutaId == fisioterapeutaId.Value);
            var pacientesQuery = _context.Pacientes.AsNoTracking().Where(p => p.Estado);
            var citasQuery = _context.Citas.AsNoTracking().Include(c => c.Paciente).Where(c => c.Fecha >= hoy && c.Estado != CitaEstados.Cancelada);
            if (fisioterapeutaId.HasValue)
            {
                var id = fisioterapeutaId.Value;
                var asignados = _context.AsignacionesPaciente.Where(a => a.FisioterapeutaId == id && a.Estado).Select(a => a.PacienteId)
                    .Union(_context.Citas.Where(c => c.FisioterapeutaId == id && c.Estado != CitaEstados.Cancelada).Select(c => c.PacienteId));
                pacientesQuery = pacientesQuery.Where(p => asignados.Contains(p.PacienteId));
                citasQuery = citasQuery.Where(c => c.FisioterapeutaId == id);
            }
            return new EquiposPageViewModel
            {
                Equipos = await _context.EquiposTerapeuticos.AsNoTracking().OrderBy(e => e.Nombre).ToListAsync(),
                Usos = await usosQuery.OrderByDescending(u => u.Fecha).ThenBy(u => u.HoraInicio).ToListAsync(),
                Pacientes = await pacientesQuery.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToListAsync(),
                Citas = await citasQuery.OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio).ToListAsync()
            };
        }

        public async Task<(bool Success, string Error)> GuardarEquipoAsync(EquipoTerapeutico model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Codigo)) return (false, "Nombre y código son obligatorios.");
            var codigo = model.Codigo.Trim().ToUpperInvariant();
            if (await _context.EquiposTerapeuticos.AnyAsync(e => e.Codigo == codigo && e.EquipoId != model.EquipoId)) return (false, "Ya existe un equipo con ese código.");
            var entity = model.EquipoId > 0 ? await _context.EquiposTerapeuticos.FindAsync(model.EquipoId) : null;
            if (entity == null)
            {
                entity = new EquipoTerapeutico { FechaRegistro = DateTime.Now, Estado = true };
                _context.EquiposTerapeuticos.Add(entity);
            }
            entity.Nombre = model.Nombre.Trim(); entity.Codigo = codigo; entity.Tipo = string.IsNullOrWhiteSpace(model.Tipo) ? "CAMINADORA" : model.Tipo.Trim().ToUpperInvariant();
            entity.VelocidadMaxima = model.VelocidadMaxima; entity.InclinacionMaxima = model.InclinacionMaxima;
            entity.EstadoOperativo = string.IsNullOrWhiteSpace(model.EstadoOperativo) ? "DISPONIBLE" : model.EstadoOperativo;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> CambiarEstadoEquipoAsync(int equipoId, string estado)
        {
            var validos = new[] { "DISPONIBLE", "MANTENIMIENTO", "INACTIVO" };
            estado = estado?.Trim().ToUpperInvariant();
            if (!validos.Contains(estado)) return (false, "Estado de equipo no válido.");
            var equipo = await _context.EquiposTerapeuticos.FindAsync(equipoId);
            if (equipo == null) return (false, "Equipo no encontrado.");
            if (estado != "DISPONIBLE" && await _context.UsosEquipo.AnyAsync(u => u.EquipoId == equipoId && u.Fecha >= DateTime.Today && (u.Estado == "PROGRAMADO" || u.Estado == "EN_USO"))) return (false, "El equipo tiene usos activos o programados; cancélelos antes de bloquearlo.");
            equipo.EstadoOperativo = estado; equipo.Estado = estado != "INACTIVO";
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> ProgramarAsync(ProgramarUsoEquipoFormModel model, int fisioterapeutaId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (model != null && (model.Velocidad < 0 || model.Inclinacion < 0 || model.HoraInicio < TimeSpan.Zero || model.HoraFin >= TimeSpan.FromDays(1) || model.Fecha.Date.Add(model.HoraInicio) <= DateTime.Now)) return (false, "Revisa los parámetros y selecciona un horario futuro.");
            if (model == null || model.EquipoId <= 0 || model.PacienteId <= 0 || model.HoraFin <= model.HoraInicio) return (false, "Complete un horario válido para el uso del equipo.");
            if (model.Fecha.Date < DateTime.Today) return (false, "No se puede programar el equipo en una fecha pasada.");
            var equipo = await _context.EquiposTerapeuticos.FirstOrDefaultAsync(e => e.EquipoId == model.EquipoId && e.Estado && e.EstadoOperativo == "DISPONIBLE");
            if (equipo == null) return (false, "El equipo no está disponible.");
            var acceso = await _context.AsignacionesPaciente.AnyAsync(a => a.PacienteId == model.PacienteId && a.FisioterapeutaId == fisioterapeutaId && a.Estado)
                || await _context.Citas.AnyAsync(c => c.PacienteId == model.PacienteId && c.FisioterapeutaId == fisioterapeutaId && c.Estado != CitaEstados.Cancelada);
            if (!acceso) return (false, "El paciente no pertenece a su cartera clínica.");
            if (model.CitaId.HasValue && !await _context.Citas.AnyAsync(c => c.CitaId == model.CitaId && c.PacienteId == model.PacienteId && c.FisioterapeutaId == fisioterapeutaId)) return (false, "La cita no corresponde al paciente seleccionado.");
            if (equipo.VelocidadMaxima.HasValue && model.Velocidad > equipo.VelocidadMaxima) return (false, "La velocidad supera el límite del equipo.");
            if (equipo.InclinacionMaxima.HasValue && model.Inclinacion > equipo.InclinacionMaxima) return (false, "La inclinación supera el límite del equipo.");
            var ocupado = await _context.UsosEquipo.AnyAsync(u => u.EquipoId == model.EquipoId && u.Fecha == model.Fecha.Date && u.Estado != "CANCELADO" && u.HoraInicio < model.HoraFin && model.HoraInicio < u.HoraFin);
            if (ocupado) return (false, "La caminadora ya está reservada en ese horario.");
            _context.UsosEquipo.Add(new UsoEquipo { EquipoId = model.EquipoId, PacienteId = model.PacienteId, FisioterapeutaId = fisioterapeutaId, CitaId = model.CitaId, Fecha = model.Fecha.Date, HoraInicio = model.HoraInicio, HoraFin = model.HoraFin, Velocidad = model.Velocidad, Inclinacion = model.Inclinacion, Indicaciones = model.Indicaciones?.Trim(), Estado = "PROGRAMADO", FechaRegistro = DateTime.Now });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> CambiarEstadoUsoAsync(int usoEquipoId, int fisioterapeutaId, string estado, string resultado = null, bool administrador = false)
        {
            var uso = await _context.UsosEquipo.FindAsync(usoEquipoId);
            if (uso == null) return (false, "Programación no encontrada.");
            if (!administrador && uso.FisioterapeutaId != fisioterapeutaId) return (false, "La programación no pertenece a su agenda.");
            estado = estado?.ToUpperInvariant();
            if (estado == "EN_USO" && uso.Fecha.Date != DateTime.Today) return (false, "El equipo solo puede iniciarse el día programado.");
            if (estado == "EN_USO" && uso.Estado != "PROGRAMADO") return (false, "Solo puede iniciar un uso programado.");
            if (estado == "FINALIZADO" && uso.Estado != "EN_USO") return (false, "Debe iniciar el equipo antes de finalizarlo.");
            if (estado == "CANCELADO" && uso.Estado == "FINALIZADO") return (false, "Un uso finalizado no puede cancelarse.");
            if (!new[] { "EN_USO", "FINALIZADO", "CANCELADO" }.Contains(estado)) return (false, "Estado de uso no válido.");
            uso.Estado = estado; if (!string.IsNullOrWhiteSpace(resultado)) uso.Resultado = resultado.Trim();
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
