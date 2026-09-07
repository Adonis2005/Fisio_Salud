using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Models.Entities;
using FisioSalud_Proyecto.Models.Fisioterapeuta;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Services
{
    public interface IPacienteService
    {
        Task<FisioDashboardViewModel> GetDashboardAsync(int fisioterapeutaId);
        Task<FisioAgendaViewModel> GetAgendaAsync(int fisioterapeutaId);
        Task<PacienteFilterViewModel> GetPacientesAsync(int fisioterapeutaId, string busqueda, string filtro);
        Task<FisioEjerciciosViewModel> GetEjerciciosAsync(int fisioterapeutaId, string busqueda, string categoria);
        Task<FisioMensajesViewModel> GetMensajesAsync(int? pacienteId);
        Task<PacienteFormViewModel> GetPacienteFormAsync(int? id);
        Task<PacienteListViewModel> GetPacienteDetalleAsync(int id);
        Task<(bool Success, string Error)> CreateAsync(PacienteFormViewModel model);
        Task<(bool Success, string Error)> UpdateAsync(PacienteFormViewModel model);
    }

    public class PacienteService : IPacienteService
    {
        private readonly FisioSaludDbContext _context;

        public PacienteService(FisioSaludDbContext context)
        {
            _context = context;
        }

        public async Task<FisioDashboardViewModel> GetDashboardAsync(int fisioterapeutaId)
        {
            var pacienteIds = _context.Citas.Where(c => c.FisioterapeutaId == fisioterapeutaId).Select(c => c.PacienteId).Distinct();
            var dbPacientes = await _context.Pacientes.Where(p => pacienteIds.Contains(p.PacienteId)).AsNoTracking()
                .OrderByDescending(p => p.FechaRegistro)
                .ToListAsync();

            var resultPacientes = dbPacientes.Select(MapToList).ToList();

            var hoy = DateTime.Today;
            var citasHoy = await _context.Citas
                .Include(c => c.Paciente)
                .Where(c => c.FisioterapeutaId == fisioterapeutaId && c.Fecha.Date == hoy)
                .AsNoTracking()
                .OrderBy(c => c.HoraInicio)
                .ToListAsync();

            var agendaHoy = citasHoy.Select(c => new AgendaHoyItemViewModel
            {
                Hora = c.HoraInicio.ToString(@"hh\:mm"),
                PacienteNombre = c.Paciente != null ? c.Paciente.NombreCompleto : "Paciente N/A",
                Iniciales = c.Paciente != null ? GetIniciales(c.Paciente.Nombres, c.Paciente.Apellidos) : "PA",
                AvatarBgColor = "#0096e6",
                Diagnostico = !string.IsNullOrEmpty(c.MotivoConsulta) ? c.MotivoConsulta : "Consulta de Fisioterapia",
                EstadoChip = MapEstadoChip(c.Estado),
                ChipClass = MapChipClass(c.Estado),
                Duracion = c.HoraFin.HasValue ? $"{(int)(c.HoraFin.Value - c.HoraInicio).TotalMinutes}m" : "30m"
            }).ToList();

            var citasCompletadasHoy = citasHoy.Count(c => string.Equals(c.Estado, "COMPLETADA", StringComparison.OrdinalIgnoreCase));

            return new FisioDashboardViewModel
            {
                TotalPacientes = resultPacientes.Count,
                PacientesActivos = resultPacientes.Count(p => p.Estado),
                CitasHoy = citasHoy.Count,
                CitasCompletadasHoy = citasCompletadasHoy,
                EjerciciosAsignados = 0,
                EjerciciosPendientes = 0,
                MensajesSinLeer = 0,
                MensajesUrgentes = 0,
                AgendaHoy = agendaHoy,
                PacientesActivosLista = resultPacientes,
                PacientesRecientes = resultPacientes.Take(5).ToList()
            };
        }

        public async Task<FisioAgendaViewModel> GetAgendaAsync(int fisioterapeutaId)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            var inicioSemana = hoy.AddDays(-1 * diff).Date;
            var finSemana = inicioSemana.AddDays(6).Date;

            var dbCitas = await _context.Citas
                .Include(c => c.Paciente)
                .Where(c => c.FisioterapeutaId == fisioterapeutaId && c.Fecha.Date >= inicioSemana && c.Fecha.Date <= finSemana)
                .AsNoTracking()
                .ToListAsync();

            var citasBloque = dbCitas.Select(c =>
            {
                int diaSemana = (int)c.Fecha.DayOfWeek;
                int diaSlot = diaSemana == 0 ? 7 : diaSemana;

                return new CitaCalendarBlockViewModel
                {
                    CitaId = c.CitaId,
                    PacienteNombre = c.Paciente != null ? c.Paciente.Nombres : "Paciente",
                    Iniciales = c.Paciente != null ? GetIniciales(c.Paciente.Nombres, c.Paciente.Apellidos) : "PA",
                    Diagnostico = !string.IsNullOrEmpty(c.MotivoConsulta) ? c.MotivoConsulta : "Consulta",
                    HoraInicio = c.HoraInicio.ToString(@"hh\:mm"),
                    Duracion = c.HoraFin.HasValue ? $"{(int)(c.HoraFin.Value - c.HoraInicio).TotalMinutes}m" : "30m",
                    CodigoSesion = "S.1",
                    ColorClass = "cyan",
                    DiaSemana = diaSlot,
                    HoraSlot = c.HoraInicio.Hours
                };
            }).ToList();

            string rango = $"{inicioSemana:dd MMM} – {finSemana:dd MMM yyyy}";

            return new FisioAgendaViewModel
            {
                RangoFechas = rango,
                FechaReferencia = hoy,
                CitasBloque = citasBloque
            };
        }

        public async Task<PacienteFilterViewModel> GetPacientesAsync(int fisioterapeutaId, string busqueda, string filtro)
        {
            var pacienteIds = _context.Citas.Where(c => c.FisioterapeutaId == fisioterapeutaId).Select(c => c.PacienteId).Distinct();
            var query = _context.Pacientes.AsNoTracking().Where(p => pacienteIds.Contains(p.PacienteId));

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLower();
                query = query.Where(p =>
                    p.Nombres.ToLower().Contains(term) ||
                    p.Apellidos.ToLower().Contains(term) ||
                    p.Identificacion.Contains(term) ||
                    (p.Correo != null && p.Correo.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(filtro) && filtro != "Todos")
            {
                if (filtro == "Activos") query = query.Where(p => p.Estado);
                else if (filtro == "Inactivos" || filtro == "En riesgo") query = query.Where(p => !p.Estado);
            }

            var dbList = (await query
                .OrderBy(p => p.Apellidos)
                .ThenBy(p => p.Nombres)
                .ToListAsync())
                .Select(MapToList)
                .ToList();

            return new PacienteFilterViewModel
            {
                Busqueda = busqueda,
                Filtro = string.IsNullOrEmpty(filtro) ? "Todos" : filtro,
                Pacientes = dbList,
                PacienteSeleccionado = dbList.FirstOrDefault()
            };
        }

        public async Task<FisioEjerciciosViewModel> GetEjerciciosAsync(int fisioterapeutaId, string busqueda, string categoria)
        {
            var query = _context.Ejercicios.AsNoTracking().Where(e => e.Estado);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLower();
                query = query.Where(e => e.Nombre.ToLower().Contains(term) || (e.Descripcion != null && e.Descripcion.ToLower().Contains(term)));
            }
            var ejercicios = await query.OrderBy(e => e.Nombre).ToListAsync();
            var asignados = await _context.TratamientoEjercicios.AsNoTracking()
                .Where(te => te.Estado && te.PlanTratamiento.FisioterapeutaId == fisioterapeutaId)
                .GroupBy(te => te.EjercicioId).Select(g => new { EjercicioId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.EjercicioId, x => x.Count);
            return new FisioEjerciciosViewModel
            {
                Busqueda = busqueda,
                CategoriaSeleccionada = string.IsNullOrEmpty(categoria) ? "Todos" : categoria,
                TotalEjercicios = ejercicios.Count,
                AsignadosActivos = asignados.Values.Sum(),
                CategoriasCount = 1,
                NuevosEsteMes = ejercicios.Count(e => e.FechaRegistro >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)),
                Ejercicios = ejercicios.Select(e => new EjercicioCardViewModel { EjercicioId = e.EjercicioId, Nombre = e.Nombre, AsignadosCount = asignados.ContainsKey(e.EjercicioId) ? asignados[e.EjercicioId] : 0, Dosificacion = e.DuracionMinutos.HasValue ? $"{e.DuracionMinutos} min" : "Sin duración", Dificultad = "No especificada" }).ToList()
            };
        }

        public async Task<FisioMensajesViewModel> GetMensajesAsync(int? pacienteId)
        {
            await Task.CompletedTask;
            return new FisioMensajesViewModel
            {
                Conversaciones = new List<ChatConversationViewModel>(),
                ConversacionActiva = null,
                MensajesChat = new List<ChatMessageItemViewModel>()
            };
        }

        public async Task<PacienteFormViewModel> GetPacienteFormAsync(int? id)
        {
            if (!id.HasValue)
                return new PacienteFormViewModel { Estado = true, FechaNacimiento = DateTime.Today.AddYears(-30) };

            var paciente = await _context.Pacientes.FindAsync(id.Value);
            if (paciente == null) return null;

            return new PacienteFormViewModel
            {
                PacienteId = paciente.PacienteId,
                Nombres = paciente.Nombres,
                Apellidos = paciente.Apellidos,
                Identificacion = paciente.Identificacion,
                FechaNacimiento = paciente.FechaNacimiento,
                Sexo = paciente.Sexo,
                Direccion = paciente.Direccion,
                Telefono = paciente.Telefono,
                Correo = paciente.Correo,
                Estado = paciente.Estado
            };
        }

        public async Task<PacienteListViewModel> GetPacienteDetalleAsync(int id)
        {
            var paciente = await _context.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.PacienteId == id);
            if (paciente == null) return null;
            return MapToList(paciente);
        }

        public async Task<(bool Success, string Error)> CreateAsync(PacienteFormViewModel model)
        {
            if (await _context.Pacientes.AnyAsync(p => p.Identificacion == model.Identificacion.Trim()))
                return (false, "La identificación ya está registrada.");

            var paciente = MapFromForm(model);
            paciente.FechaRegistro = DateTime.Now;

            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> UpdateAsync(PacienteFormViewModel model)
        {
            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.PacienteId == model.PacienteId);
            if (paciente == null)
                return (false, "Paciente no encontrado.");

            if (await _context.Pacientes.AnyAsync(p =>
                p.Identificacion == model.Identificacion.Trim() && p.PacienteId != model.PacienteId))
                return (false, "La identificación ya está registrada.");

            paciente.Nombres = model.Nombres.Trim();
            paciente.Apellidos = model.Apellidos.Trim();
            paciente.Identificacion = model.Identificacion.Trim();
            paciente.FechaNacimiento = model.FechaNacimiento;
            paciente.Sexo = model.Sexo;
            paciente.Direccion = model.Direccion?.Trim();
            paciente.Telefono = model.Telefono?.Trim();
            paciente.Correo = model.Correo?.Trim().ToLower();
            paciente.Estado = model.Estado;
            paciente.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        private static Paciente MapFromForm(PacienteFormViewModel model)
        {
            return new Paciente
            {
                Nombres = model.Nombres.Trim(),
                Apellidos = model.Apellidos.Trim(),
                Identificacion = model.Identificacion.Trim(),
                FechaNacimiento = model.FechaNacimiento,
                Sexo = model.Sexo,
                Direccion = model.Direccion?.Trim(),
                Telefono = model.Telefono?.Trim(),
                Correo = model.Correo?.Trim().ToLower(),
                Estado = model.Estado
            };
        }

        private static PacienteListViewModel MapToList(Paciente p)
        {
            return new PacienteListViewModel
            {
                PacienteId = p.PacienteId,
                Nombres = p.Nombres,
                Apellidos = p.Apellidos,
                Identificacion = p.Identificacion,
                FechaNacimiento = p.FechaNacimiento,
                Sexo = p.Sexo,
                Telefono = p.Telefono,
                Correo = p.Correo,
                Estado = p.Estado,
                FechaRegistro = p.FechaRegistro,
                Diagnostico = "Fisioterapia General",
                ProgresoPorcentaje = 0,
                SesionesTexto = "0/0",
                ProximaCitaTexto = "Sin programar",
                NivelDolor = 0,
                UltimaSesionTexto = p.FechaRegistro.ToString("dd MMM yyyy")
            };
        }

        private static string GetIniciales(string nombres, string apellidos)
        {
            string n = string.IsNullOrWhiteSpace(nombres) ? "" : nombres.Trim()[0].ToString();
            string a = string.IsNullOrWhiteSpace(apellidos) ? "" : apellidos.Trim()[0].ToString();
            return (n + a).ToUpper();
        }

        private static string MapEstadoChip(string estado)
        {
            if (string.Equals(estado, "COMPLETADA", StringComparison.OrdinalIgnoreCase)) return "Completada";
            if (string.Equals(estado, "EN_CURSO", StringComparison.OrdinalIgnoreCase)) return "En curso";
            if (string.Equals(estado, "CANCELADA", StringComparison.OrdinalIgnoreCase)) return "Cancelada";
            return "Pendiente";
        }

        private static string MapChipClass(string estado)
        {
            if (string.Equals(estado, "COMPLETADA", StringComparison.OrdinalIgnoreCase)) return "completada";
            if (string.Equals(estado, "EN_CURSO", StringComparison.OrdinalIgnoreCase)) return "en-curso";
            if (string.Equals(estado, "CANCELADA", StringComparison.OrdinalIgnoreCase)) return "cancelada";
            return "pendiente";
        }
    }
}
