using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Models.Entities;
using FisioSalud_Proyecto.Models.Fisioterapeuta;
using Microsoft.EntityFrameworkCore;

using FisioSalud_Proyecto.Models.Clinical;
using FisioSalud_Proyecto.Helpers;

namespace FisioSalud_Proyecto.Services
{
    public interface IPacienteService
    {
        Task<FisioDashboardViewModel> GetDashboardAsync(int fisioterapeutaId);
        Task<FisioAgendaViewModel> GetAgendaAsync(int fisioterapeutaId, DateTime? fechaReferencia = null);
        Task<PacienteFilterViewModel> GetPacientesAsync(int fisioterapeutaId, string busqueda, string filtro);
        Task<FisioEjerciciosViewModel> GetEjerciciosAsync(int fisioterapeutaId, string busqueda, string categoria);
        Task<FisioMensajesViewModel> GetMensajesAsync(int fisioterapeutaId, int? pacienteId);
        Task<PacienteFormViewModel> GetPacienteFormAsync(int? id, int? fisioterapeutaId = null);
        Task<PacienteListViewModel> GetPacienteDetalleAsync(int id);
        Task<(bool Success, string Error)> CreateAsync(PacienteFormViewModel model, int? fisioterapeutaId = null);
        Task<(bool Success, string Error)> UpdateAsync(PacienteFormViewModel model);
        Task<(bool Success, string Error)> IniciarAtencionAsync(int citaId, int fisioterapeutaId);
        Task<(bool Success, string Error)> GuardarEvaluacionInicialAsync(EvaluacionInicialFormModel model, int fisioterapeutaId);
        Task<(bool Success, string Error)> GuardarDiagnosticoAsync(DiagnosticoFormModel model, int fisioterapeutaId);
        Task<(bool Success, string Error)> CrearPlanTratamientoAsync(PlanTratamientoFormModel model, int fisioterapeutaId);
        Task<(bool Success, string Error)> AsignarEjercicioAsync(AsignarEjercicioFormModel model, int fisioterapeutaId);
        Task<(bool Success, string Error)> FinalizarSesionAsync(FinalizarSesionFormModel model, int fisioterapeutaId);
        Task<(bool Success, string Error)> CancelarCitaAsync(int citaId, int fisioterapeutaId);
        Task<(bool Success, string Error)> AgendarCitaAsync(FisioNuevaCitaFormViewModel model, int fisioterapeutaId);
        Task<bool> TieneAccesoPacienteAsync(int pacienteId, int fisioterapeutaId);
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
            var pacienteIds = PacientesAsignados(fisioterapeutaId);
            var dbPacientes = await _context.Pacientes.Where(p => pacienteIds.Contains(p.PacienteId)).AsNoTracking()
                .OrderByDescending(p => p.FechaRegistro)
                .ToListAsync();

            var resultPacientes = dbPacientes.Select(MapToList).ToList();
            await CompletarResumenClinicoAsync(resultPacientes, fisioterapeutaId);

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

            var citasCompletadasHoy = citasHoy.Count(c => c.Estado == CitaEstados.Atendida);
            var ejerciciosAsignados = await _context.TratamientoEjercicios.CountAsync(te => te.Estado && te.PlanTratamiento.FisioterapeutaId == fisioterapeutaId);
            var ejerciciosCumplidos = await _context.EjerciciosRealizados.CountAsync(er => er.TratamientoEjercicio.PlanTratamiento.FisioterapeutaId == fisioterapeutaId);
            var mensajesSinLeer = await _context.Mensajes.CountAsync(m => m.DestinatarioId == fisioterapeutaId && !m.Leido);

            return new FisioDashboardViewModel
            {
                TotalPacientes = resultPacientes.Count,
                PacientesActivos = resultPacientes.Count(p => p.Estado),
                CitasHoy = citasHoy.Count,
                CitasCompletadasHoy = citasCompletadasHoy,
                EjerciciosAsignados = ejerciciosAsignados,
                EjerciciosPendientes = Math.Max(0, ejerciciosAsignados - ejerciciosCumplidos),
                MensajesSinLeer = mensajesSinLeer,
                MensajesUrgentes = 0,
                AgendaHoy = agendaHoy,
                PacientesActivosLista = resultPacientes,
                PacientesRecientes = resultPacientes.Take(5).ToList(),
                Biblioteca = (await GetEjerciciosAsync(fisioterapeutaId, null, null)).Ejercicios.Take(3).ToList()
            };
        }

        public async Task<FisioAgendaViewModel> GetAgendaAsync(int fisioterapeutaId, DateTime? fechaReferencia = null)
        {
            var referencia = (fechaReferencia ?? DateTime.Today).Date;
            int diff = (7 + (referencia.DayOfWeek - DayOfWeek.Monday)) % 7;
            var inicioSemana = referencia.AddDays(-1 * diff).Date;
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
                    Estado = c.Estado,
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

            var pacienteIds = PacientesAsignados(fisioterapeutaId);
            var pacientes = await _context.Pacientes.AsNoTracking().Where(p => p.Estado && pacienteIds.Contains(p.PacienteId)).OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToListAsync();
            var servicios = await _context.Servicios.AsNoTracking().Where(s => s.Estado).OrderBy(s => s.Nombre).ToListAsync();
            return new FisioAgendaViewModel
            {
                RangoFechas = rango,
                FechaReferencia = inicioSemana,
                CitasBloque = citasBloque,
                PacientesDisponibles = pacientes,
                ServiciosDisponibles = servicios
            };
        }

        public async Task<PacienteFilterViewModel> GetPacientesAsync(int fisioterapeutaId, string busqueda, string filtro)
        {
            var pacienteIds = PacientesAsignados(fisioterapeutaId);
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

            await CompletarResumenClinicoAsync(dbList, fisioterapeutaId);
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
            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todos")
            {
                var cat = categoria.Trim().ToLower();
                query = query.Where(e => e.Nombre.ToLower().Contains(cat) || (e.Descripcion != null && e.Descripcion.ToLower().Contains(cat)));
            }
            var ejercicios = await query.OrderBy(e => e.Nombre).ToListAsync();
            var asignados = await _context.TratamientoEjercicios.AsNoTracking()
                .Where(te => te.Estado && te.PlanTratamiento.FisioterapeutaId == fisioterapeutaId)
                .GroupBy(te => te.EjercicioId).Select(g => new { EjercicioId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.EjercicioId, x => x.Count);
            var planes = await _context.PlanesTratamiento.AsNoTracking()
                .Include(p => p.Paciente)
                .Where(p => p.FisioterapeutaId == fisioterapeutaId && p.Estado == "ACTIVO")
                .OrderBy(p => p.Paciente.Apellidos).ThenBy(p => p.Paciente.Nombres).ThenBy(p => p.Nombre)
                .Select(p => new PlanAsignacionViewModel
                {
                    PlanTratamientoId = p.PlanTratamientoId,
                    PacienteId = p.PacienteId,
                    Etiqueta = p.Paciente.Nombres + " " + p.Paciente.Apellidos + " — " + p.Nombre
                }).ToListAsync();
            return new FisioEjerciciosViewModel
            {
                Busqueda = busqueda,
                CategoriaSeleccionada = string.IsNullOrEmpty(categoria) ? "Todos" : categoria,
                TotalEjercicios = ejercicios.Count,
                AsignadosActivos = asignados.Values.Sum(),
                CategoriasCount = ejercicios.Count == 0 ? 0 : ejercicios.Select(e => CategoriaEjercicio(e.Nombre + " " + e.Descripcion)).Distinct().Count(),
                NuevosEsteMes = ejercicios.Count(e => e.FechaRegistro >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)),
                Ejercicios = ejercicios.Select(e => new EjercicioCardViewModel { EjercicioId = e.EjercicioId, Nombre = e.Nombre, Categoria = CategoriaEjercicio(e.Nombre + " " + e.Descripcion), AsignadosCount = asignados.ContainsKey(e.EjercicioId) ? asignados[e.EjercicioId] : 0, Dosificacion = e.DuracionMinutos.HasValue ? $"{e.DuracionMinutos} min" : "Sin duración", Dificultad = "Clínico", DificultadBadgeClass = "activo", TagHeaderClass = "cyan", IconClass = "bi bi-activity", Descripcion = e.Descripcion, Recomendaciones = e.Recomendaciones }).ToList(),
                PlanesDisponibles = planes
            };
        }

        public async Task<FisioMensajesViewModel> GetMensajesAsync(int fisioterapeutaId, int? pacienteId)
        {
            var model = new FisioMensajesViewModel();
            if (!pacienteId.HasValue) return model;
            if (!await TieneAccesoPacienteAsync(pacienteId.Value, fisioterapeutaId)) return model;

            var paciente = await _context.Pacientes.AsNoTracking()
                .Where(p => p.PacienteId == pacienteId.Value)
                .Select(p => new
                {
                    p.PacienteId,
                    p.UsuarioId,
                    p.Nombres,
                    p.Apellidos,
                    p.FechaNacimiento
                })
                .FirstOrDefaultAsync();

            if (paciente == null) return model;

            model.ConversacionActiva = new ChatConversationViewModel
            {
                PacienteId = paciente.PacienteId,
                PacienteNombre = $"{paciente.Nombres} {paciente.Apellidos}".Trim(),
                Iniciales = GetIniciales(paciente.Nombres, paciente.Apellidos),
                Diagnostico = "Consulta de fisioterapia",
                Edad = DateTime.Today.Year - paciente.FechaNacimiento.Year -
                    (DateTime.Today < paciente.FechaNacimiento.Date.AddYears(DateTime.Today.Year - paciente.FechaNacimiento.Year) ? 1 : 0)
            };

            return model;
        }

        public async Task<PacienteFormViewModel> GetPacienteFormAsync(int? id, int? fisioterapeutaId = null)
        {
            var fisioterapeutas = await _context.Usuarios.AsNoTracking().Include(u => u.Rol)
                .Where(u => u.Estado && u.Rol.Nombre == Roles.Fisioterapeuta).OrderBy(u => u.Apellidos).ThenBy(u => u.Nombres).ToListAsync();
            if (!id.HasValue)
                return new PacienteFormViewModel { Estado = true, FechaNacimiento = DateTime.Today.AddYears(-30), FisioterapeutaId = fisioterapeutaId, FisioterapeutasDisponibles = fisioterapeutas };

            var paciente = await _context.Pacientes.FindAsync(id.Value);
            if (paciente == null) return null;

            var asignacion = await _context.AsignacionesPaciente.AsNoTracking().FirstOrDefaultAsync(a => a.PacienteId == id.Value && a.Estado);
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
                Estado = paciente.Estado,
                FisioterapeutaId = fisioterapeutaId ?? asignacion?.FisioterapeutaId,
                FisioterapeutasDisponibles = fisioterapeutas
            };
        }

        public async Task<PacienteListViewModel> GetPacienteDetalleAsync(int id)
        {
            var paciente = await _context.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.PacienteId == id);
            if (paciente == null) return null;

            var vm = MapToList(paciente);

            vm.Evaluaciones = await _context.EvaluacionesIniciales.AsNoTracking()
                .Where(e => e.PacienteId == id)
                .OrderByDescending(e => e.FechaEvaluacion)
                .ToListAsync();

            vm.DiagnosticosLista = await _context.Diagnosticos.AsNoTracking()
                .Where(d => d.PacienteId == id)
                .OrderByDescending(d => d.FechaDiagnostico)
                .ToListAsync();

            vm.PlanesTratamiento = await _context.PlanesTratamiento.AsNoTracking()
                .Where(p => p.PacienteId == id)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            var planIds = vm.PlanesTratamiento.Select(p => p.PlanTratamientoId).ToList();

            vm.EjerciciosAsignados = await _context.TratamientoEjercicios
                .Include(te => te.Ejercicio)
                .Where(te => planIds.Contains(te.PlanTratamientoId))
                .AsNoTracking()
                .ToListAsync();
            vm.EjerciciosDisponibles = await _context.Ejercicios.AsNoTracking().Where(e => e.Estado).OrderBy(e => e.Nombre).ToListAsync();

            vm.SesionesLista = await _context.SesionesRehabilitacion
                .Include(s => s.Seguimientos)
                .Where(s => planIds.Contains(s.PlanTratamientoId))
                .OrderByDescending(s => s.FechaSesion)
                .AsNoTracking()
                .ToListAsync();

            vm.CitasLista = await _context.Citas
                .Include(c => c.Servicio)
                .Where(c => c.PacienteId == id)
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.HoraInicio)
                .AsNoTracking()
                .ToListAsync();

            var diagReciente = vm.DiagnosticosLista.FirstOrDefault();
            if (diagReciente != null) vm.Diagnostico = diagReciente.DiagnosticoTexto;

            var ultSeguimiento = vm.SesionesLista.SelectMany(s => s.Seguimientos).OrderByDescending(s => s.FechaRegistro).FirstOrDefault();
            var evalInicial = vm.Evaluaciones.OrderBy(e => e.FechaEvaluacion).FirstOrDefault();

            if (ultSeguimiento?.NivelDolor.HasValue == true)
                vm.NivelDolor = (int)Math.Round(ultSeguimiento.NivelDolor.Value);
            else if (evalInicial?.DolorInicial.HasValue == true)
                vm.NivelDolor = (int)Math.Round(evalInicial.DolorInicial.Value);

            vm.SesionesTexto = $"{vm.SesionesLista.Count}/12";

            var proximaCita = vm.CitasLista.FirstOrDefault(c => c.Fecha >= DateTime.Today && c.Estado != CitaEstados.Cancelada);
            if (proximaCita != null)
                vm.ProximaCitaTexto = $"{proximaCita.Fecha:dd/MM/yyyy} {proximaCita.HoraInicio:hh\\:mm}";

            return vm;
        }

        public async Task<(bool Success, string Error)> CreateAsync(PacienteFormViewModel model, int? fisioterapeutaId = null)
        {
            if (await _context.Pacientes.AnyAsync(p => p.Identificacion == model.Identificacion.Trim()))
                return (false, "La identificación ya está registrada.");

            var responsableId = fisioterapeutaId ?? model.FisioterapeutaId;
            if (!responsableId.HasValue || !await EsFisioterapeutaActivoAsync(responsableId.Value))
                return (false, "Seleccione un fisioterapeuta responsable activo.");

            var paciente = MapFromForm(model);
            paciente.FechaRegistro = DateTime.Now;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();
            _context.AsignacionesPaciente.Add(new AsignacionPaciente { PacienteId = paciente.PacienteId, FisioterapeutaId = responsableId.Value, Estado = true, FechaAsignacion = DateTime.Now });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
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

            if (model.FisioterapeutaId.HasValue)
                await AsignarPacienteAsync(paciente.PacienteId, model.FisioterapeutaId.Value);

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

        private async Task CompletarResumenClinicoAsync(List<PacienteListViewModel> pacientes, int fisioterapeutaId)
        {
            var ids = pacientes.Select(p => p.PacienteId).ToList();
            var diagnosticos = await _context.Diagnosticos.AsNoTracking().Where(d => ids.Contains(d.PacienteId) && d.FisioterapeutaId == fisioterapeutaId && d.Estado).OrderByDescending(d => d.FechaDiagnostico).ThenByDescending(d => d.DiagnosticoId).ToListAsync();
            var sesiones = await _context.SesionesRehabilitacion.AsNoTracking().Include(s => s.PlanTratamiento).Where(s => ids.Contains(s.PlanTratamiento.PacienteId) && s.FisioterapeutaId == fisioterapeutaId).OrderByDescending(s => s.FechaSesion).ToListAsync();
            var seguimientos = await _context.Seguimientos.AsNoTracking().Include(s => s.Sesion).ThenInclude(s => s.PlanTratamiento).Where(s => ids.Contains(s.Sesion.PlanTratamiento.PacienteId) && s.Sesion.FisioterapeutaId == fisioterapeutaId).OrderByDescending(s => s.FechaRegistro).ToListAsync();
            var hoy = DateTime.Today;
            var citas = await _context.Citas.AsNoTracking().Where(c => ids.Contains(c.PacienteId) && c.FisioterapeutaId == fisioterapeutaId && c.Fecha >= hoy && c.Estado != CitaEstados.Cancelada && c.Estado != CitaEstados.Atendida && c.Estado != CitaEstados.NoAsistio).OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio).ToListAsync();
            foreach (var p in pacientes)
            {
                var medicion = seguimientos.FirstOrDefault(s => s.Sesion.PlanTratamiento.PacienteId == p.PacienteId);
                var realizadas = sesiones.Where(s => s.PlanTratamiento.PacienteId == p.PacienteId).ToList();
                var proxima = citas.FirstOrDefault(c => c.PacienteId == p.PacienteId && c.Fecha.Add(c.HoraInicio) > DateTime.Now);
                p.Diagnostico = diagnosticos.FirstOrDefault(d => d.PacienteId == p.PacienteId)?.DiagnosticoTexto ?? "Sin diagnóstico registrado";
                p.ProgresoPorcentaje = (int)(medicion?.GradoRecuperacion ?? 0);
                p.NivelDolor = (int)(medicion?.NivelDolor ?? 0);
                p.TieneMedicion = medicion != null;
                p.SesionesTexto = realizadas.Count + " realizadas";
                p.UltimaSesionTexto = realizadas.FirstOrDefault()?.FechaSesion.ToString("dd/MM/yyyy") ?? "Sin sesiones";
                p.ProximaCitaTexto = proxima == null ? "Sin programar" : proxima.Fecha.Add(proxima.HoraInicio).ToString("dd/MM HH:mm");
            }
        }

        private static string GetIniciales(string nombres, string apellidos)
        {
            string n = string.IsNullOrWhiteSpace(nombres) ? "" : nombres.Trim()[0].ToString();
            string a = string.IsNullOrWhiteSpace(apellidos) ? "" : apellidos.Trim()[0].ToString();
            return (n + a).ToUpper();
        }

        public async Task<(bool Success, string Error)> IniciarAtencionAsync(int citaId, int fisioterapeutaId)
        {
            var cita = await _context.Citas.FindAsync(citaId);
            if (cita == null) return (false, "Cita no encontrada.");
            if (cita.FisioterapeutaId != fisioterapeutaId) return (false, "La cita no pertenece a tu agenda.");
            if (cita.Estado != CitaEstados.Confirmada)
                return (false, "Solo se pueden iniciar citas confirmadas.");
            if (cita.Fecha.Date != DateTime.Today)
                return (false, "La atención solo puede iniciarse el día programado.");
            cita.Estado = CitaEstados.EnAtencion;
            cita.FechaActualizacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> GuardarEvaluacionInicialAsync(EvaluacionInicialFormModel model, int fisioterapeutaId)
        {
            if (model != null && !await TieneAccesoPacienteAsync(model.PacienteId, fisioterapeutaId)) return (false, "No tienes acceso a este paciente.");
            if (model?.CitaId.HasValue == true && !await _context.Citas.AnyAsync(c => c.CitaId == model.CitaId && c.PacienteId == model.PacienteId && c.FisioterapeutaId == fisioterapeutaId))
                return (false, "La cita seleccionada no corresponde al paciente.");
            if (model == null || model.PacienteId <= 0) return (false, "Datos de evaluación no válidos.");

            var eval = new EvaluacionInicial
            {
                PacienteId = model.PacienteId,
                FisioterapeutaId = fisioterapeutaId,
                CitaId = model.CitaId,
                MotivoConsulta = string.IsNullOrWhiteSpace(model.MotivoConsulta) ? "Evaluación Inicial" : model.MotivoConsulta,
                Antecedentes = model.Antecedentes,
                DolorInicial = model.DolorInicial,
                EvaluacionFisica = model.EvaluacionFisica,
                Observaciones = model.Observaciones,
                FechaEvaluacion = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            _context.EvaluacionesIniciales.Add(eval);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> GuardarDiagnosticoAsync(DiagnosticoFormModel model, int fisioterapeutaId)
        {
            if (model != null && !await TieneAccesoPacienteAsync(model.PacienteId, fisioterapeutaId)) return (false, "No tienes acceso a este paciente.");
            if (model?.PatologiaId.HasValue == true && !await _context.Patologias.AnyAsync(p => p.PatologiaId == model.PatologiaId && p.Estado))
                return (false, "La patología seleccionada no está disponible.");
            if (model == null || model.PacienteId <= 0 || string.IsNullOrWhiteSpace(model.DiagnosticoTexto))
                return (false, "Diagnóstico no válido.");

            var diag = new Diagnostico
            {
                PacienteId = model.PacienteId,
                PatologiaId = model.PatologiaId,
                FisioterapeutaId = fisioterapeutaId,
                DiagnosticoTexto = model.DiagnosticoTexto,
                Observaciones = model.Observaciones,
                EvaluacionFuncional = model.EvaluacionFuncional,
                FechaDiagnostico = DateTime.Today,
                Estado = true,
                FechaRegistro = DateTime.Now
            };

            _context.Diagnosticos.Add(diag);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> CrearPlanTratamientoAsync(PlanTratamientoFormModel model, int fisioterapeutaId)
        {
            if (model != null && !await _context.Diagnosticos.AnyAsync(d => d.DiagnosticoId == model.DiagnosticoId && d.PacienteId == model.PacienteId && d.FisioterapeutaId == fisioterapeutaId))
                return (false, "El diagnóstico no corresponde a este paciente o terapeuta.");
            if (model == null || model.PacienteId <= 0 || model.DiagnosticoId <= 0)
                return (false, "Plan de tratamiento no válido.");

            var plan = new PlanTratamiento
            {
                PacienteId = model.PacienteId,
                DiagnosticoId = model.DiagnosticoId,
                FisioterapeutaId = fisioterapeutaId,
                Nombre = string.IsNullOrWhiteSpace(model.Nombre) ? "Plan de Fisioterapia" : model.Nombre,
                Objetivos = string.IsNullOrWhiteSpace(model.Objetivos) ? "Recuperación funcional" : model.Objetivos,
                DuracionEstimada = model.DuracionEstimada,
                FechaInicio = model.FechaInicio != default ? model.FechaInicio : DateTime.Today,
                Estado = "ACTIVO",
                Observaciones = model.Observaciones,
                FechaCreacion = DateTime.Now
            };

            _context.PlanesTratamiento.Add(plan);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> AsignarEjercicioAsync(AsignarEjercicioFormModel model, int fisioterapeutaId)
        {
            if (model != null && !await _context.PlanesTratamiento.AnyAsync(p => p.PlanTratamientoId == model.PlanTratamientoId && p.FisioterapeutaId == fisioterapeutaId && p.Estado == "ACTIVO"))
                return (false, "El plan no pertenece a tu cartera activa.");
            if (model != null && !await _context.Ejercicios.AnyAsync(e => e.EjercicioId == model.EjercicioId && e.Estado))
                return (false, "El ejercicio no está disponible.");
            if (model != null && await _context.TratamientoEjercicios.AnyAsync(te => te.PlanTratamientoId == model.PlanTratamientoId && te.EjercicioId == model.EjercicioId && te.Estado))
                return (false, "El ejercicio ya está asignado a este plan.");
            if (model == null || model.PlanTratamientoId <= 0 || model.EjercicioId <= 0)
                return (false, "Asignación de ejercicio no válida.");

            var asignacion = new TratamientoEjercicio
            {
                PlanTratamientoId = model.PlanTratamientoId,
                EjercicioId = model.EjercicioId,
                Frecuencia = string.IsNullOrWhiteSpace(model.Frecuencia) ? "Diaria" : model.Frecuencia,
                Series = model.Series ?? 3,
                Repeticiones = model.Repeticiones ?? 12,
                Observaciones = model.Observaciones,
                Estado = true,
                FechaAsignacion = DateTime.Now
            };

            _context.TratamientoEjercicios.Add(asignacion);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> FinalizarSesionAsync(FinalizarSesionFormModel model, int fisioterapeutaId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (model != null && (model.NivelDolor < 0 || model.NivelDolor > 10 || model.GradoRecuperacion < 0 || model.GradoRecuperacion > 100)) return (false, "Dolor debe estar entre 0 y 10 y recuperación entre 0 y 100.");
            if (model != null)
            {
                var planValido = await _context.PlanesTratamiento.AnyAsync(p => p.PlanTratamientoId == model.PlanTratamientoId && p.PacienteId == model.PacienteId && p.FisioterapeutaId == fisioterapeutaId && p.Estado == "ACTIVO");
                var citaValida = await _context.Citas.AnyAsync(c => c.CitaId == model.CitaId && c.PacienteId == model.PacienteId && c.FisioterapeutaId == fisioterapeutaId && c.Estado == CitaEstados.EnAtencion);
                if (!planValido || !citaValida) return (false, "La cita o el plan no corresponden a esta atención.");
                if (await _context.SesionesRehabilitacion.AnyAsync(s => s.CitaId == model.CitaId)) return (false, "Esta cita ya tiene una sesión registrada.");
            }
            if (model == null || model.CitaId <= 0 || model.PlanTratamientoId <= 0)
                return (false, "Sesión no válida.");

            var numSesiones = await _context.SesionesRehabilitacion.CountAsync(s => s.PlanTratamientoId == model.PlanTratamientoId);

            var sesion = new SesionRehabilitacion
            {
                PlanTratamientoId = model.PlanTratamientoId,
                CitaId = model.CitaId,
                FisioterapeutaId = fisioterapeutaId,
                NumeroSesion = numSesiones + 1,
                FechaSesion = DateTime.Today,
                ActividadesRealizadas = string.IsNullOrWhiteSpace(model.ActividadesRealizadas) ? "Sesión de terapia aplicada" : model.ActividadesRealizadas,
                Observaciones = model.Observaciones,
                Resultados = model.Resultados,
                Estado = "REALIZADA",
                FechaRegistro = DateTime.Now
            };

            _context.SesionesRehabilitacion.Add(sesion);
            await _context.SaveChangesAsync();

            var seguimiento = new Seguimiento
            {
                SesionId = sesion.SesionId,
                NivelDolor = model.NivelDolor,
                MovilidadArticular = model.MovilidadArticular,
                FuerzaMuscular = model.FuerzaMuscular,
                GradoRecuperacion = model.GradoRecuperacion,
                Observaciones = model.Observaciones,
                FechaRegistro = DateTime.Now
            };

            _context.Seguimientos.Add(seguimiento);

            var cita = await _context.Citas.FindAsync(model.CitaId);
            if (cita != null)
            {
                cita.Estado = CitaEstados.Atendida;
                cita.FechaActualizacion = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> CancelarCitaAsync(int citaId, int fisioterapeutaId)
        {
            var cita = await _context.Citas.FindAsync(citaId);
            if (cita == null) return (false, "Cita no encontrada.");
            if (cita.FisioterapeutaId != fisioterapeutaId) return (false, "La cita no pertenece a tu agenda.");
            if (cita.Estado == CitaEstados.Atendida || cita.Estado == CitaEstados.EnAtencion) return (false, "Una cita atendida o en atención no puede cancelarse.");
            var factura = await _context.Facturas.Include(f => f.DetallesFactura).FirstOrDefaultAsync(f => f.DetallesFactura.Any(d => d.CitaId == citaId));
            if (factura?.Estado == PagoEstados.Pagado) return (false, "La cita está pagada. Administración debe procesar el reembolso antes de cancelarla.");
            await using var transaction = await _context.Database.BeginTransactionAsync();
            cita.Estado = CitaEstados.Cancelada;
            cita.FechaActualizacion = DateTime.Now;
            if (factura?.Estado == PagoEstados.Pendiente) factura.Estado = PagoEstados.Cancelado;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> AgendarCitaAsync(FisioNuevaCitaFormViewModel model, int fisioterapeutaId)
        {
            if (model == null || model.PacienteId <= 0 || model.ServicioId <= 0 || model.DuracionMinutos < 15)
                return (false, "Los datos de la cita no son válidos.");
            if (model.Fecha.Date < DateTime.Today || (model.Fecha.Date == DateTime.Today && model.HoraInicio <= DateTime.Now.TimeOfDay))
                return (false, "La cita debe programarse para una fecha y hora futuras.");

            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.PacienteId == model.PacienteId && p.Estado);
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.ServicioId == model.ServicioId && s.Estado);
            if (paciente == null || servicio == null) return (false, "El paciente o servicio seleccionado no está disponible.");
            if (!await TieneAccesoPacienteAsync(model.PacienteId, fisioterapeutaId)) return (false, "El paciente no pertenece a tu cartera clínica.");

            var horaFin = model.HoraInicio.Add(TimeSpan.FromMinutes(model.DuracionMinutos));
            byte diaSemana = (byte)(model.Fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)model.Fecha.DayOfWeek);
            if (!await _context.DisponibilidadesFisioterapeuta.AnyAsync(d => d.FisioterapeutaId == fisioterapeutaId && d.DiaSemana == diaSemana && d.Estado && model.HoraInicio >= d.HoraInicio && horaFin <= d.HoraFin))
                return (false, "El horario está fuera de tu disponibilidad configurada.");
            var citasDia = await _context.Citas.Where(c => c.FisioterapeutaId == fisioterapeutaId && c.Fecha == model.Fecha.Date && c.Estado != CitaEstados.Cancelada && c.HoraInicio < horaFin).Select(c => new { c.HoraInicio, c.HoraFin }).ToListAsync();
            var traslape = citasDia.Any(c => model.HoraInicio < (c.HoraFin ?? c.HoraInicio.Add(TimeSpan.FromHours(1))));
            if (traslape) return (false, "Ya existe otra cita que se traslapa con ese horario.");
            var citasPaciente = await _context.Citas.Where(c => c.PacienteId == model.PacienteId && c.Fecha == model.Fecha.Date && c.Estado != CitaEstados.Cancelada && c.HoraInicio < horaFin).Select(c => new { c.HoraInicio, c.HoraFin }).ToListAsync();
            if (citasPaciente.Any(c => model.HoraInicio < (c.HoraFin ?? c.HoraInicio.Add(TimeSpan.FromHours(1))))) return (false, "El paciente ya tiene otra cita en ese horario.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var cita = new Cita
            {
                PacienteId = paciente.PacienteId,
                FisioterapeutaId = fisioterapeutaId,
                ServicioId = servicio.ServicioId,
                Fecha = model.Fecha.Date,
                HoraInicio = model.HoraInicio,
                HoraFin = horaFin,
                MotivoConsulta = string.IsNullOrWhiteSpace(model.MotivoConsulta) ? servicio.Nombre : model.MotivoConsulta.Trim(),
                Observaciones = model.Observaciones?.Trim(),
                Estado = CitaEstados.PendientePago,
                FechaRegistro = DateTime.Now
            };
            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();

            var factura = new Factura
            {
                NumeroFactura = $"FAC-{DateTime.Now:yyyyMMddHHmmss}-{paciente.PacienteId}",
                PacienteId = paciente.PacienteId,
                FisioterapeutaId = fisioterapeutaId,
                UsuarioId = paciente.UsuarioId,
                Monto = servicio.Precio,
                Fecha = DateTime.Today,
                Estado = PagoEstados.Pendiente,
                FechaRegistro = DateTime.Now
            };
            _context.Facturas.Add(factura);
            await _context.SaveChangesAsync();
            _context.DetallesFactura.Add(new DetalleFactura
            {
                FacturaId = factura.FacturaId,
                CitaId = cita.CitaId,
                ServicioId = servicio.ServicioId,
                Concepto = servicio.Nombre,
                Cantidad = 1,
                PrecioUnitario = servicio.Precio,
                Subtotal = servicio.Precio,
                FechaRegistro = DateTime.Now
            });
            await _context.SaveChangesAsync();
            await AsignarPacienteAsync(paciente.PacienteId, fisioterapeutaId);
            await transaction.CommitAsync();
            return (true, null);
        }

        public async Task<bool> TieneAccesoPacienteAsync(int pacienteId, int fisioterapeutaId)
        {
            return await _context.AsignacionesPaciente.AsNoTracking().AnyAsync(a => a.PacienteId == pacienteId && a.FisioterapeutaId == fisioterapeutaId && a.Estado)
                || await _context.Citas.AsNoTracking().AnyAsync(c => c.PacienteId == pacienteId && c.FisioterapeutaId == fisioterapeutaId && c.Estado != CitaEstados.Cancelada);
        }

        private IQueryable<int> PacientesAsignados(int fisioterapeutaId)
        {
            var asignados = _context.AsignacionesPaciente.Where(a => a.FisioterapeutaId == fisioterapeutaId && a.Estado).Select(a => a.PacienteId);
            var conCita = _context.Citas.Where(c => c.FisioterapeutaId == fisioterapeutaId && c.Estado != CitaEstados.Cancelada).Select(c => c.PacienteId);
            return asignados.Union(conCita);
        }

        private Task<bool> EsFisioterapeutaActivoAsync(int usuarioId) => _context.Usuarios.Include(u => u.Rol)
            .AnyAsync(u => u.UsuarioId == usuarioId && u.Estado && u.Rol.Nombre == Roles.Fisioterapeuta);

        private async Task AsignarPacienteAsync(int pacienteId, int fisioterapeutaId)
        {
            if (!await EsFisioterapeutaActivoAsync(fisioterapeutaId)) return;
            var existentes = await _context.AsignacionesPaciente.Where(a => a.PacienteId == pacienteId).ToListAsync();
            foreach (var existente in existentes)
            {
                existente.Estado = existente.FisioterapeutaId == fisioterapeutaId;
                existente.FechaFin = existente.Estado ? null : DateTime.Now;
            }
            var asignacion = existentes.FirstOrDefault(a => a.FisioterapeutaId == fisioterapeutaId);
            if (asignacion == null)
                _context.AsignacionesPaciente.Add(new AsignacionPaciente { PacienteId = pacienteId, FisioterapeutaId = fisioterapeutaId, Estado = true, FechaAsignacion = DateTime.Now });
            else if (!asignacion.Estado)
            {
                asignacion.Estado = true;
                asignacion.FechaAsignacion = DateTime.Now;
                asignacion.FechaFin = null;
            }
            await _context.SaveChangesAsync();
        }

        private static string CategoriaEjercicio(string texto)
        {
            texto = (texto ?? string.Empty).ToLowerInvariant();
            foreach (var categoria in new[] { "Columna", "Hombro", "Caderas", "Cuello", "Rodilla", "Tobillo", "Respiración" })
                if (texto.Contains(categoria.ToLowerInvariant())) return categoria;
            return "General";
        }

        private static string MapEstadoChip(string estado)
        {
            if (estado == CitaEstados.Atendida) return "Atendida";
            if (estado == CitaEstados.EnAtencion) return "En atención";
            if (string.Equals(estado, "CANCELADA", StringComparison.OrdinalIgnoreCase)) return "Cancelada";
            return CitaEstados.Etiqueta(estado);
        }

        private static string MapChipClass(string estado)
        {
            if (estado == CitaEstados.Atendida) return "completada";
            if (estado == CitaEstados.EnAtencion) return "en-curso";
            if (string.Equals(estado, "CANCELADA", StringComparison.OrdinalIgnoreCase)) return "cancelada";
            return "pendiente";
        }
    }
}
