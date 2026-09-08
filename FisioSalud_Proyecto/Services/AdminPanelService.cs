using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Administrador;
using FisioSalud_Proyecto.Models.Entities;
using Microsoft.EntityFrameworkCore;

using FisioSalud_Proyecto.Models.Clinical;

namespace FisioSalud_Proyecto.Services
{
    public interface IAdminPanelService
    {
        Task<AdminChromeViewModel> GetChromeAsync(string nombreAdmin);
        Task<AdminDashboardPageViewModel> GetDashboardAsync();
        Task<AdminClinicaPageViewModel> GetClinicaAsync();
        Task<AdminTerapeutasPageViewModel> GetTerapeutasAsync(string busqueda, int? seleccionadoId);
        Task<AdminPacientesPageViewModel> GetPacientesAsync(string busqueda, string filtro);
        Task<AdminPlanesPageViewModel> GetPlanesAsync(string tab);
        Task<AdminReportesPageViewModel> GetReportesAsync();
        Task<(string FileName, string Content)> GenerarReporteCsvAsync(string tipo, DateTime inicio, DateTime fin, IEnumerable<string> metricas);
        Task<AdminAnaliticaPageViewModel> GetAnaliticaAsync(string periodo);
        Task<AdminConfiguracionPageViewModel> GetConfiguracionAsync(int usuarioId, string seccion, string busqueda, int? rolId, bool? estado);
        Task<AdminBusquedaPageViewModel> BuscarAsync(string q);
        Task<AdminCitasPageViewModel> GetCitasAsync(string busqueda, string estado, DateTime? fechaDesde, DateTime? fechaHasta);
        Task<(bool Success, string Error)> CambiarEstadoCitaAsync(int citaId, string nuevoEstado, string observacion);
        Task<AdminFacturasPageViewModel> GetFacturasAsync(string busqueda, string estado, DateTime? fechaDesde, DateTime? fechaHasta);
        Task<(bool Success, string Error)> RegistrarPagoAdminAsync(int facturaId, string metodoPago, string referencia);
        Task<(bool Success, string Error)> CambiarEstadoFacturaAsync(int facturaId, string nuevoEstado);
        Task<List<Servicio>> GetServiciosAsync();
        Task<(bool Success, string Error)> SaveServicioAsync(ServicioFormViewModel model);
        Task<List<DisponibilidadFisioterapeuta>> GetDisponibilidadesAsync();
        Task<(bool Success, string Error)> SaveDisponibilidadAsync(DisponibilidadFormViewModel model);
        Task<List<Ejercicio>> GetEjerciciosCatalogoAsync();
        Task<(bool Success, string Error)> SaveEjercicioCatalogoAsync(Ejercicio model);
        Task<(bool Success, string Error)> ToggleEjercicioCatalogoAsync(int ejercicioId);
        Task<List<Patologia>> GetPatologiasAsync();
        Task<(bool Success, string Error)> SavePatologiaAsync(Patologia model);
        Task<(bool Success, string Error)> TogglePatologiaAsync(int patologiaId);
    }

    public class AdminPanelService : IAdminPanelService
    {
        private static readonly CultureInfo Es = new CultureInfo("es-ES");
        private static readonly string[] AvatarColors = { "#4A6CF7", "#0096e6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6", "#06b6d4", "#d946ef" };
        private const int CapacidadBase = 18;

        private readonly FisioSaludDbContext _context;
        private readonly IUsuarioService _usuarioService;

        public AdminPanelService(FisioSaludDbContext context, IUsuarioService usuarioService)
        {
            _context = context;
            _usuarioService = usuarioService;
        }

        public async Task<AdminChromeViewModel> GetChromeAsync(string nombreAdmin)
        {
            var hoy = DateTime.Today;
            var terapeutasActivos = await _context.Usuarios.CountAsync(u => u.Estado && u.Rol.Nombre == Roles.Fisioterapeuta);
            var pacientesActivos = await _context.Pacientes.CountAsync(p => p.Estado);
            var alertas = await BuildAlertasAsync();
            var riesgo = await ContarPacientesRiesgoAsync();

            return new AdminChromeViewModel
            {
                NombreAdmin = string.IsNullOrWhiteSpace(nombreAdmin) ? "Administrador" : nombreAdmin,
                Iniciales = Iniciales(nombreAdmin),
                FechaLarga = Capitalizar(hoy.ToString("dddd d 'de' MMMM, yyyy", Es)),
                PacientesActivos = pacientesActivos,
                TerapeutasActivos = terapeutasActivos,
                Alertas = alertas,
                AlertasCount = alertas.Count,
                PacientesRiesgo = riesgo
            };
        }

        public async Task<AdminDashboardPageViewModel> GetDashboardAsync()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioMesAnt = inicioMes.AddMonths(-1);

            var pacientes = await _context.Pacientes.ToListAsync();
            var terapeutas = await TerapeutasQuery().ToListAsync();
            var citas = await _context.Citas
                .Include(c => c.Paciente)
                .Include(c => c.Fisioterapeuta)
                .ToListAsync();

            var altasMes = pacientes.Count(p => p.FechaRegistro >= inicioMes);
            var altasMesAnt = pacientes.Count(p => p.FechaRegistro >= inicioMesAnt && p.FechaRegistro < inicioMes);
            var terapeutasHoy = citas
                .Where(c => c.Fecha.Date == hoy && c.Estado != CitaEstados.Cancelada)
                .Select(c => c.FisioterapeutaId)
                .Distinct()
                .Count();

            var asistidas = citas.Count(c => c.Estado == CitaEstados.Atendida);
            var conResultado = citas.Count(c => c.Estado == CitaEstados.Atendida || c.Estado == CitaEstados.Cancelada);
            var asistencia = conResultado == 0 ? 0 : (int)Math.Round(asistidas * 100.0 / conResultado);

            var carga = BuildCarga(terapeutas, citas);
            var semana = BuildSemana(citas, hoy);
            var mensual = BuildMensual(citas, hoy, 7);
            var planes = BuildPlanes(citas).Take(4).ToList();

            var proximas = citas
                .Where(c => c.Fecha.Date >= hoy && c.Estado == CitaEstados.Programada)
                .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
                .Take(5)
                .Select(MapCita)
                .ToList();

            return new AdminDashboardPageViewModel
            {
                Kpis = new List<AdminKpiViewModel>
                {
                    new AdminKpiViewModel
                    {
                        Valor = pacientes.Count.ToString(),
                        Titulo = "Total de pacientes",
                        Subtitulo = $"+{altasMes} este mes" + (altasMesAnt > 0 ? $" · {Delta(altasMes, altasMesAnt)} vs mes ant." : ""),
                        Icono = "bi-people-fill",
                        Color = "blue"
                    },
                    new AdminKpiViewModel
                    {
                        Valor = terapeutas.Count.ToString(),
                        Titulo = "Total de terapeutas",
                        Subtitulo = $"{terapeutasHoy} con citas hoy · {terapeutas.Count(t => t.Estado)} activos",
                        Icono = "bi-heart-pulse-fill",
                        Color = "cyan"
                    },
                    new AdminKpiViewModel
                    {
                        Valor = asistencia + "%",
                        Titulo = "Índice de asistencia",
                        Subtitulo = conResultado == 0 ? "Aún no hay citas cerradas" : $"{asistidas} atendidas / {conResultado} cerradas",
                        Icono = "bi-star-fill",
                        Color = "green"
                    },
                    new AdminKpiViewModel
                    {
                        Valor = altasMes.ToString(),
                        Titulo = "Nuevas altas este mes",
                        Subtitulo = altasMesAnt == 0 ? "Primer mes con registros" : $"{Delta(altasMes, altasMesAnt)} vs mes anterior",
                        Icono = "bi-person-plus-fill",
                        Color = "yellow"
                    }
                },
                CargaTerapeutas = carga,
                CitasSemana = semana,
                ActividadMensual = mensual,
                MaxCitasDia = Math.Max(1, semana.Max(s => s.Total)),
                MaxActividadMes = Math.Max(1, mensual.Max(m => m.Total)),
                Alertas = await BuildAlertasAsync(),
                ProximasCitas = proximas,
                Planes = planes
            };
        }

        public async Task<AdminClinicaPageViewModel> GetClinicaAsync()
        {
            var hoy = DateTime.Today;
            var ahora = DateTime.Now.TimeOfDay;
            var terapeutas = await TerapeutasQuery().ToListAsync();
            var citasHoy = await _context.Citas
                .Include(c => c.Paciente)
                .Include(c => c.Fisioterapeuta)
                .Where(c => c.Fecha == hoy)
                .ToListAsync();

            var salas = new List<AdminSalaViewModel>();
            var i = 1;
            foreach (var t in terapeutas)
            {
                var delTerapeuta = citasHoy
                    .Where(c => c.FisioterapeutaId == t.UsuarioId && c.Estado != CitaEstados.Cancelada)
                    .OrderBy(c => c.HoraInicio)
                    .ToList();

                var enCurso = delTerapeuta.FirstOrDefault(c => EstaEnCurso(c, ahora));
                var siguiente = delTerapeuta.FirstOrDefault(c => c.HoraInicio > ahora);

                if (enCurso != null)
                {
                    salas.Add(new AdminSalaViewModel
                    {
                        Nombre = $"Consultorio {i}",
                        Estado = "Ocupado",
                        EstadoClass = "ocupado",
                        Paciente = enCurso.Paciente?.NombreCompleto ?? "Paciente",
                        Terapeuta = Prefijo(t.NombreCompleto),
                        Horario = FormatoRango(enCurso),
                        Motivo = Texto(enCurso.MotivoConsulta, "Sesión de fisioterapia"),
                        PuedeAsignar = false
                    });
                }
                else
                {
                    salas.Add(new AdminSalaViewModel
                    {
                        Nombre = $"Consultorio {i}",
                        Estado = "Libre",
                        EstadoClass = "libre",
                        Paciente = siguiente == null ? "Sin citas pendientes hoy" : $"Siguiente: {siguiente.Paciente?.NombreCompleto}",
                        Terapeuta = Prefijo(t.NombreCompleto),
                        Horario = siguiente == null ? "—" : FormatoRango(siguiente),
                        Motivo = siguiente?.MotivoConsulta,
                        PuedeAsignar = true
                    });
                }

                i++;
            }

            var ocupadas = salas.Count(s => s.EstadoClass == "ocupado");
            var libres = salas.Count(s => s.EstadoClass == "libre");
            var ocupacion = salas.Count == 0 ? 0 : (int)Math.Round(ocupadas * 100.0 / salas.Count);

            return new AdminClinicaPageViewModel
            {
                OcupacionPorcentaje = ocupacion,
                SalasLibres = libres,
                PacientesEnClinica = ocupadas,
                CitasHoy = citasHoy.Count(c => c.Estado != CitaEstados.Cancelada),
                Salas = salas,
                CargaHoy = BuildCargaHoy(terapeutas, citasHoy)
            };
        }

        public async Task<AdminTerapeutasPageViewModel> GetTerapeutasAsync(string busqueda, int? seleccionadoId)
        {
            var terapeutas = await TerapeutasQuery().ToListAsync();
            var citas = await _context.Citas.Include(c => c.Paciente).ToListAsync();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLowerInvariant();
                terapeutas = terapeutas.Where(t =>
                    t.NombreCompleto.ToLowerInvariant().Contains(term) ||
                    (t.Correo ?? "").ToLowerInvariant().Contains(term)).ToList();
            }

            var rows = terapeutas.Select((t, idx) => MapTerapeutaRow(t, citas, idx)).ToList();
            var selected = seleccionadoId.HasValue
                ? rows.FirstOrDefault(r => r.UsuarioId == seleccionadoId.Value)
                : rows.FirstOrDefault();

            AdminTerapeutaDetalleViewModel detalle = null;
            if (selected != null)
            {
                var asignados = citas
                    .Where(c => c.FisioterapeutaId == selected.UsuarioId)
                    .GroupBy(c => c.PacienteId)
                    .Select(g =>
                    {
                        var last = g.OrderByDescending(c => c.Fecha).ThenByDescending(c => c.HoraInicio).First();
                        var p = last.Paciente;
                        var total = g.Count();
                        var att = g.Count(c => c.Estado == CitaEstados.Atendida);
                        return new AdminPacienteAsignadoViewModel
                        {
                            PacienteId = g.Key,
                            Nombre = p?.NombreCompleto ?? "Paciente",
                            Iniciales = Iniciales(p?.NombreCompleto),
                            Color = ColorDe(g.Key),
                            Diagnostico = Texto(last.MotivoConsulta, "Tratamiento en curso"),
                            Progreso = total == 0 ? 0 : (int)Math.Round(att * 100.0 / total)
                        };
                    })
                    .OrderByDescending(p => p.Progreso)
                    .Take(6)
                    .ToList();

                detalle = new AdminTerapeutaDetalleViewModel
                {
                    UsuarioId = selected.UsuarioId,
                    Nombre = selected.Nombre,
                    Iniciales = selected.Iniciales,
                    Color = selected.Color,
                    Correo = selected.Correo,
                    Pacientes = selected.Pacientes,
                    Capacidad = selected.Capacidad,
                    Porcentaje = selected.Porcentaje,
                    Desde = selected.Desde,
                    PacientesAsignados = asignados
                };
            }

            return new AdminTerapeutasPageViewModel
            {
                Busqueda = busqueda,
                SeleccionadoId = selected?.UsuarioId,
                Terapeutas = rows,
                Detalle = detalle
            };
        }

        public async Task<AdminPacientesPageViewModel> GetPacientesAsync(string busqueda, string filtro)
        {
            filtro = string.IsNullOrWhiteSpace(filtro) ? "Todos" : filtro;
            var pacientes = await _context.Pacientes.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToListAsync();
            var citas = await _context.Citas.Include(c => c.Fisioterapeuta).ToListAsync();
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            var rows = pacientes.Select((p, idx) =>
            {
                var pc = citas.Where(c => c.PacienteId == p.PacienteId).ToList();
                var estado = ClasificarPaciente(p, pc, hoy);
                var prox = pc.Where(c => c.Fecha.Date >= hoy && c.Estado == CitaEstados.Programada)
                    .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio).FirstOrDefault();
                var terapeuta = pc.OrderByDescending(c => c.Fecha).ThenByDescending(c => c.HoraInicio)
                    .Select(c => c.Fisioterapeuta).FirstOrDefault();
                var last = pc.OrderByDescending(c => c.Fecha).FirstOrDefault();
                var att = pc.Count(c => c.Estado == CitaEstados.Atendida);
                var progreso = pc.Count == 0 ? 0 : (int)Math.Round(att * 100.0 / pc.Count);

                return new AdminPacienteRowViewModel
                {
                    PacienteId = p.PacienteId,
                    Nombre = p.NombreCompleto,
                    Iniciales = Iniciales(p.NombreCompleto),
                    Color = ColorDe(p.PacienteId),
                    Edad = Edad(p.FechaNacimiento),
                    Diagnostico = Texto(last?.MotivoConsulta, "Sin diagnóstico registrado"),
                    Terapeuta = terapeuta == null ? "Sin asignar" : Prefijo(terapeuta.NombreCompleto),
                    TerapeutaIniciales = terapeuta == null ? "—" : Iniciales(terapeuta.NombreCompleto),
                    Progreso = progreso,
                    BarClass = BarClass(progreso),
                    Sesiones = att,
                    ProximaCita = prox == null ? "Sin cita" : Relativa(prox.Fecha.Date, hoy),
                    Estado = estado.Texto,
                    EstadoClass = estado.Css
                };
            }).ToList();

            var enRiesgo = rows.Count(r => r.EstadoClass == "riesgo");
            var nuevos = pacientes.Count(p => p.FechaRegistro >= inicioMes);
            var activos = rows.Count(r => r.EstadoClass == "activo");

            IEnumerable<AdminPacienteRowViewModel> filtrados = rows;
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLowerInvariant();
                filtrados = filtrados.Where(r =>
                    r.Nombre.ToLowerInvariant().Contains(term) ||
                    r.Diagnostico.ToLowerInvariant().Contains(term) ||
                    r.Terapeuta.ToLowerInvariant().Contains(term));
            }

            switch (filtro)
            {
                case "Activos": filtrados = filtrados.Where(r => r.EstadoClass == "activo"); break;
                case "Riesgo": filtrados = filtrados.Where(r => r.EstadoClass == "riesgo"); break;
                case "Nuevos": filtrados = filtrados.Where(r => r.EstadoClass == "nuevo"); break;
                case "Inactivos": filtrados = filtrados.Where(r => r.EstadoClass == "pendiente"); break;
            }

            return new AdminPacientesPageViewModel
            {
                Busqueda = busqueda,
                Filtro = filtro,
                Total = pacientes.Count,
                Activos = activos,
                EnRiesgo = enRiesgo,
                Nuevos = nuevos,
                Pacientes = filtrados.ToList()
            };
        }

        public async Task<AdminPlanesPageViewModel> GetPlanesAsync(string tab)
        {
            tab = string.IsNullOrWhiteSpace(tab) ? "ejercicios" : tab;
            var citas = await _context.Citas.Include(c => c.Fisioterapeuta).ToListAsync();
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var terapeutasActivos = await _context.Usuarios.CountAsync(u => u.Estado && u.Rol.Nombre == Roles.Fisioterapeuta);

            var planes = BuildPlanes(citas);
            var ejercicios = await _context.Ejercicios.AsNoTracking().OrderBy(e => e.Nombre).ToListAsync();
            var patologias = await _context.Patologias.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();
            if (tab == "tratamientos")
                planes = planes.OrderByDescending(p => p.Usos).ToList();

            return new AdminPlanesPageViewModel
            {
                Tab = tab,
                TotalPlanes = ejercicios.Count + patologias.Count,
                UsosMes = citas.Count(c => c.Fecha >= inicioMes && !string.IsNullOrWhiteSpace(c.MotivoConsulta)),
                TerapeutasActivos = terapeutasActivos,
                Planes = planes.Select(p => new AdminPlanCardViewModel
                {
                    Titulo = p.Titulo,
                    Autor = p.Autor,
                    Usos = p.Usos,
                    Actualizado = p.Actualizado,
                    Icono = tab == "tratamientos" ? "bi-clipboard2-pulse" : "bi-file-earmark-text"
                }).ToList(),
                Ejercicios = ejercicios,
                Patologias = patologias
            };
        }

        public Task<AdminReportesPageViewModel> GetReportesAsync()
        {
            var hoy = DateTime.Today;
            var model = new AdminReportesPageViewModel
            {
                FechaInicio = new DateTime(hoy.Year, hoy.Month, 1),
                FechaFin = hoy,
                Historial = new List<AdminReporteHistorialViewModel>
                {
                    new AdminReporteHistorialViewModel
                    {
                        Titulo = "Actividad clínica — " + Capitalizar(hoy.ToString("MMMM yyyy", Es)),
                        Fecha = hoy.ToString("dd/MM/yyyy"),
                        Formato = "CSV",
                        TipoCss = "csv"
                    }
                }
            };
            return Task.FromResult(model);
        }

        public async Task<(string FileName, string Content)> GenerarReporteCsvAsync(string tipo, DateTime inicio, DateTime fin, IEnumerable<string> metricas)
        {
            if (tipo == "financiero")
            {
                var facturas = await _context.Facturas.AsNoTracking().Include(f => f.Paciente).Include(f => f.Fisioterapeuta).Where(f => f.Fecha >= inicio.Date && f.Fecha < fin.Date.AddDays(1)).OrderBy(f => f.Fecha).ToListAsync();
                var reporte = new StringBuilder("Fecha,Factura,Paciente,Fisioterapeuta,Monto,Estado\r\n");
                foreach(var f in facturas) reporte.AppendLine($"{f.Fecha:yyyy-MM-dd},{Csv(f.NumeroFactura)},{Csv(f.Paciente.NombreCompleto)},{Csv(f.Fisioterapeuta?.NombreCompleto)},{f.Monto.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Csv(f.Estado)}");
                reporte.AppendLine($"Total cobrado,,,,{facturas.Where(f=>f.Estado==PagoEstados.Pagado).Sum(f=>f.Monto).ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                reporte.AppendLine($"Total pendiente,,,,{facturas.Where(f=>f.Estado==PagoEstados.Pendiente).Sum(f=>f.Monto).ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                return ($"facturacion-{inicio:yyyyMMdd}-{fin:yyyyMMdd}.csv",reporte.ToString());
            }
            if (tipo == "cumplimiento")
            {
                var registros=await _context.EjerciciosRealizados.AsNoTracking().Include(e=>e.Paciente).Include(e=>e.TratamientoEjercicio).ThenInclude(t=>t.Ejercicio).Where(e=>e.FechaRealizacion>=inicio.Date && e.FechaRealizacion<fin.Date.AddDays(1)).OrderBy(e=>e.FechaRealizacion).ToListAsync();
                var reporte=new StringBuilder("Fecha,Paciente,Ejercicio,Series,Repeticiones,Dolor,Comentarios\r\n");
                foreach(var e in registros) reporte.AppendLine($"{e.FechaRealizacion:yyyy-MM-dd},{Csv(e.Paciente.NombreCompleto)},{Csv(e.TratamientoEjercicio.Ejercicio.Nombre)},{e.SeriesRealizadas},{e.RepeticionesRealizadas},{e.Dolor?.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Csv(e.Comentarios)}");
                return ($"cumplimiento-{inicio:yyyyMMdd}-{fin:yyyyMMdd}.csv",reporte.ToString());
            }
            var citas = await _context.Citas
                .Include(c => c.Paciente)
                .Include(c => c.Fisioterapeuta)
                .Where(c => c.Fecha >= inicio.Date && c.Fecha <= fin.Date)
                .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
                .ToListAsync();

            var pacientes = await _context.Pacientes.ToListAsync();
            var terapeutas = await TerapeutasQuery().ToListAsync();
            var seleccion = new HashSet<string>(metricas ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            sb.AppendLine("FisioSalud — Reporte administrativo");
            sb.AppendLine($"Tipo,{tipo}");
            sb.AppendLine($"Periodo,{inicio:yyyy-MM-dd} a {fin:yyyy-MM-dd}");
            sb.AppendLine();

            if (seleccion.Count == 0 || seleccion.Contains("resumen"))
            {
                sb.AppendLine("Resumen");
                sb.AppendLine($"Pacientes,{pacientes.Count}");
                sb.AppendLine($"Terapeutas,{terapeutas.Count}");
                sb.AppendLine($"Citas en periodo,{citas.Count}");
                sb.AppendLine($"Atendidas,{citas.Count(c => c.Estado == CitaEstados.Atendida)}");
                sb.AppendLine($"Programadas,{citas.Count(c => c.Estado == CitaEstados.Programada)}");
                sb.AppendLine($"Canceladas,{citas.Count(c => c.Estado == CitaEstados.Cancelada)}");
                sb.AppendLine();
            }

            if (seleccion.Count == 0 || seleccion.Contains("citas") || tipo == "clinico" || tipo == "operacional")
            {
                sb.AppendLine("Citas");
                sb.AppendLine("Fecha,Hora,Paciente,Terapeuta,Motivo,Estado");
                foreach (var c in citas)
                {
                    sb.AppendLine($"{c.Fecha:yyyy-MM-dd},{c.HoraInicio:hh\\:mm},{Csv(c.Paciente?.NombreCompleto)},{Csv(c.Fisioterapeuta?.NombreCompleto)},{Csv(c.MotivoConsulta)},{c.Estado}");
                }
                sb.AppendLine();
            }

            if (seleccion.Count == 0 || seleccion.Contains("terapeutas") || tipo == "operacional")
            {
                sb.AppendLine("Terapeutas");
                sb.AppendLine("Nombre,Correo,Pacientes,Citas periodo");
                foreach (var t in terapeutas)
                {
                    var pc = citas.Count(c => c.FisioterapeutaId == t.UsuarioId);
                    var pac = citas.Where(c => c.FisioterapeutaId == t.UsuarioId).Select(c => c.PacienteId).Distinct().Count();
                    sb.AppendLine($"{Csv(t.NombreCompleto)},{Csv(t.Correo)},{pac},{pc}");
                }
            }

            var name = $"reporte-fisiosalud-{tipo}-{inicio:yyyyMMdd}-{fin:yyyyMMdd}.csv";
            return (name, sb.ToString());
        }

        public async Task<AdminAnaliticaPageViewModel> GetAnaliticaAsync(string periodo)
        {
            periodo = string.IsNullOrWhiteSpace(periodo) ? "mes" : periodo;
            var hoy = DateTime.Today;
            var meses = periodo == "trimestre" ? 3 : periodo == "semana" ? 1 : 7;
            var citas = await _context.Citas.Include(c => c.Fisioterapeuta).Include(c => c.Paciente).ToListAsync();
            var terapeutas = await TerapeutasQuery().ToListAsync();
            var facturas = await _context.Facturas.Include(f => f.Paciente).Include(f => f.Fisioterapeuta).ToListAsync();

            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioMesAnt = inicioMes.AddMonths(-1);

            var facturasMes = facturas.Where(f => f.Fecha >= inicioMes).ToList();
            var ingresosMes = facturasMes.Where(f => f.Estado == PagoEstados.Pagado).Sum(f => f.Monto);
            var gastosMes = 0m;
            var facturasPendientesList = facturas.Where(f => f.Estado == "PENDIENTE" || f.Estado == "VENCIDA").ToList();
            var facturasPendientesMonto = facturasPendientesList.Sum(f => f.Monto);

            var pagadasCount = facturas.Count(f => f.Estado == PagoEstados.Pagado);
            var pendientesCount = facturas.Count(f => f.Estado == "PENDIENTE");
            var vencidasCount = facturas.Count(f => f.Estado == "VENCIDA");

            var facturasRows = facturas.OrderByDescending(f => f.Fecha).Take(6).Select(f => new AdminFacturaRowViewModel
            {
                FacturaId = f.FacturaId,
                NumeroFactura = f.NumeroFactura,
                Paciente = f.Paciente?.NombreCompleto ?? "Paciente",
                Terapeuta = f.Fisioterapeuta == null ? "—" : Prefijo(f.Fisioterapeuta.NombreCompleto),
                Fecha = f.Fecha.ToString("d MMM", Es),
                Monto = f.Monto,
                Estado = f.Estado,
                EstadoClass = f.Estado == PagoEstados.Pagado ? "pagada" : f.Estado == "VENCIDA" ? "vencida" : "pendiente"
            }).ToList();

            var citasMes = citas.Where(c => c.Fecha >= inicioMes).ToList();
            var citasMesAnt = citas.Count(c => c.Fecha >= inicioMesAnt && c.Fecha < inicioMes);
            var atendidas = citasMes.Count(c => c.Estado == CitaEstados.Atendida);
            var canceladas = citasMes.Count(c => c.Estado == CitaEstados.Cancelada);
            var programadas = citas.Count(c => c.Estado == CitaEstados.Programada && c.Fecha >= hoy);
            var cerradas = atendidas + canceladas;

            var serie = periodo == "semana" ? SemanaComoSerie(citas, hoy) : BuildMensual(citas, hoy, Math.Max(meses, 7));

            var sat = terapeutas.Select((t, idx) =>
            {
                var tc = citas.Where(c => c.FisioterapeutaId == t.UsuarioId).ToList();
                var att = tc.Count(c => c.Estado == CitaEstados.Atendida);
                var cerr = tc.Count(c => c.Estado == CitaEstados.Atendida || c.Estado == CitaEstados.Cancelada);
                return new AdminSatisfaccionTerapeutaViewModel
                {
                    Nombre = Prefijo(t.NombreCompleto),
                    Iniciales = Iniciales(t.NombreCompleto),
                    Color = ColorDe(idx),
                    Asistencia = cerr == 0 ? 0 : (int)Math.Round(att * 100.0 / cerr),
                    Pacientes = tc.Select(c => c.PacienteId).Distinct().Count(),
                    Citas = tc.Count
                };
            }).ToList();

            return new AdminAnaliticaPageViewModel
            {
                Periodo = periodo,
                IngresosMes = ingresosMes,
                GastosMes = gastosMes,
                GananciaNeta = ingresosMes - gastosMes,
                FacturasPendientesMonto = facturasPendientesMonto,
                FacturasAbiertasCount = facturasPendientesList.Count,
                FacturasPagadasCount = pagadasCount,
                FacturasPendientesCount = pendientesCount,
                FacturasVencidasCount = vencidasCount,
                FacturasRecientes = facturasRows,
                CitasMes = citasMes.Count,
                CitasMesAnterior = citasMesAnt,
                AtendidasMes = atendidas,
                CanceladasMes = canceladas,
                ProgramadasAbiertas = programadas,
                AsistenciaPorcentaje = cerradas == 0 ? 0 : (int)Math.Round(atendidas * 100.0 / cerradas),
                VariacionCitas = citasMesAnt == 0 ? "Sin mes anterior" : Delta(citasMes.Count, citasMesAnt) + " vs mes ant.",
                Serie = serie,
                Terapeutas = sat,
                CitasRecientes = citas.OrderByDescending(c => c.Fecha).ThenByDescending(c => c.HoraInicio).Take(6).Select(MapCita).ToList()
            };
        }

        public async Task<AdminConfiguracionPageViewModel> GetConfiguracionAsync(int usuarioId, string seccion, string busqueda, int? rolId, bool? estado)
        {
            var usuario = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
            var usuariosPage = seccion == "usuarios"
                ? await _usuarioService.GetUsuariosAsync(busqueda, rolId, estado)
                : null;

            return new AdminConfiguracionPageViewModel
            {
                Seccion = string.IsNullOrWhiteSpace(seccion) ? "general" : seccion,
                UsuarioId = usuario?.UsuarioId ?? 0,
                Nombre = usuario?.NombreCompleto ?? "Administrador",
                Correo = usuario?.Correo,
                Telefono = usuario?.Telefono,
                Rol = usuario?.Rol?.Nombre ?? Roles.Administrador,
                FechaCreacion = usuario?.FechaCreacion ?? DateTime.Now,
                UltimoAcceso = usuario?.UltimoAcceso,
                TotalUsuarios = await _context.Usuarios.CountAsync(),
                TotalPacientes = await _context.Pacientes.CountAsync(),
                TotalCitas = await _context.Citas.CountAsync(),
                Usuarios = usuariosPage
            };
        }

        public async Task<AdminBusquedaPageViewModel> BuscarAsync(string q)
        {
            var model = new AdminBusquedaPageViewModel { Q = q };
            if (string.IsNullOrWhiteSpace(q))
                return model;

            var term = q.Trim().ToLowerInvariant();
            var pacientes = await _context.Pacientes
                .Where(p => p.Nombres.ToLower().Contains(term) || p.Apellidos.ToLower().Contains(term) || p.Identificacion.Contains(term))
                .Take(8)
                .ToListAsync();

            var users = await _context.Usuarios.Include(u => u.Rol)
                .Where(u => u.Nombres.ToLower().Contains(term) || u.Apellidos.ToLower().Contains(term) || u.Correo.ToLower().Contains(term))
                .Take(8)
                .ToListAsync();

            foreach (var p in pacientes)
            {
                model.Resultados.Add(new AdminBusquedaItemViewModel
                {
                    Tipo = "Paciente",
                    Titulo = p.NombreCompleto,
                    Subtitulo = p.Identificacion,
                    Controlador = "Administrador",
                    Accion = "Pacientes",
                    Id = p.PacienteId
                });
            }

            foreach (var u in users)
            {
                var esFisio = u.Rol?.Nombre == Roles.Fisioterapeuta;
                model.Resultados.Add(new AdminBusquedaItemViewModel
                {
                    Tipo = u.Rol?.Nombre ?? "Usuario",
                    Titulo = u.NombreCompleto,
                    Subtitulo = u.Correo,
                    Controlador = "Administrador",
                    Accion = esFisio ? "Terapeutas" : "Usuarios",
                    Id = u.UsuarioId
                });
            }

            return model;
        }

        public async Task<AdminCitasPageViewModel> GetCitasAsync(string busqueda, string estado, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var query = _context.Citas.AsNoTracking()
                .Include(c => c.Paciente)
                .Include(c => c.Fisioterapeuta)
                .Include(c => c.Servicio)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim();
                query = query.Where(c => c.Paciente.Nombres.Contains(term) ||
                                         c.Paciente.Apellidos.Contains(term) ||
                                         c.Paciente.Identificacion.Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(c => c.Estado == estado.Trim().ToUpperInvariant());
            if (fechaDesde.HasValue)
                query = query.Where(c => c.Fecha >= fechaDesde.Value.Date);
            if (fechaHasta.HasValue)
                query = query.Where(c => c.Fecha < fechaHasta.Value.Date.AddDays(1));

            var citas = await query.OrderByDescending(c => c.Fecha).ThenBy(c => c.HoraInicio).ToListAsync();
            var conteos = await _context.Citas.AsNoTracking()
                .GroupBy(c => c.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Estado, x => x.Cantidad);

            int Count(string key) => conteos.TryGetValue(key, out var value) ? value : 0;
            return new AdminCitasPageViewModel
            {
                Busqueda = busqueda,
                Estado = estado,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                Citas = citas.Select(c => new AdminCitaGestionViewModel
                {
                    CitaId = c.CitaId,
                    PacienteNombre = c.Paciente?.NombreCompleto ?? "Paciente",
                    PacienteIdentificacion = c.Paciente?.Identificacion,
                    FisioterapeutaNombre = c.Fisioterapeuta?.NombreCompleto ?? "Sin asignar",
                    ServicioNombre = c.Servicio?.Nombre,
                    Fecha = c.Fecha,
                    HoraInicio = c.HoraInicio,
                    Estado = c.Estado
                }).ToList(),
                Estadisticas = new List<AdminEstadoEstadisticaViewModel>
                {
                    new AdminEstadoEstadisticaViewModel { Etiqueta = "Solicitadas", Cantidad = Count(CitaEstados.Solicitada), Color = "warning" },
                    new AdminEstadoEstadisticaViewModel { Etiqueta = "Confirmadas", Cantidad = Count(CitaEstados.Confirmada), Color = "info" },
                    new AdminEstadoEstadisticaViewModel { Etiqueta = "Atendidas", Cantidad = Count(CitaEstados.Atendida), Color = "success" },
                    new AdminEstadoEstadisticaViewModel { Etiqueta = "Canceladas", Cantidad = Count(CitaEstados.Cancelada), Color = "danger" },
                    new AdminEstadoEstadisticaViewModel { Etiqueta = "No asistieron", Cantidad = Count(CitaEstados.NoAsistio), Color = "secondary" }
                }
            };
        }

        public async Task<(bool Success, string Error)> CambiarEstadoCitaAsync(int citaId, string nuevoEstado, string observacion)
        {
            var estados = new[] { CitaEstados.Solicitada, CitaEstados.PendientePago, CitaEstados.Confirmada, CitaEstados.EnAtencion, CitaEstados.Atendida, CitaEstados.Cancelada, CitaEstados.NoAsistio };
            nuevoEstado = (nuevoEstado ?? string.Empty).Trim().ToUpperInvariant();
            if (!estados.Contains(nuevoEstado)) return (false, "El estado de la cita no es válido.");

            var cita = await _context.Citas.FindAsync(citaId);
            if (cita == null) return (false, "Cita no encontrada.");
            var transiciones = new Dictionary<string, string[]>
            {
                [CitaEstados.Solicitada] = new[] { CitaEstados.PendientePago, CitaEstados.Confirmada, CitaEstados.Cancelada },
                [CitaEstados.PendientePago] = new[] { CitaEstados.Confirmada, CitaEstados.Cancelada },
                [CitaEstados.Confirmada] = new[] { CitaEstados.EnAtencion, CitaEstados.Cancelada, CitaEstados.NoAsistio },
                [CitaEstados.EnAtencion] = new[] { CitaEstados.Atendida },
                [CitaEstados.Atendida] = Array.Empty<string>(), [CitaEstados.Cancelada] = Array.Empty<string>(), [CitaEstados.NoAsistio] = Array.Empty<string>()
            };
            if (cita.Estado != nuevoEstado && (!transiciones.TryGetValue(cita.Estado, out var permitidos) || !permitidos.Contains(nuevoEstado)))
                return (false, $"No se permite cambiar una cita de {CitaEstados.Etiqueta(cita.Estado)} a {CitaEstados.Etiqueta(nuevoEstado)}.");
            var factura = await _context.Facturas.Include(f => f.DetallesFactura).FirstOrDefaultAsync(f => f.DetallesFactura.Any(d => d.CitaId == citaId));
            if (nuevoEstado == CitaEstados.Confirmada && factura != null && factura.Estado != PagoEstados.Pagado)
                return (false, "La factura debe estar pagada antes de confirmar la cita.");
            if (nuevoEstado == CitaEstados.Cancelada && factura?.Estado == PagoEstados.Pagado)
                return (false, "Reembolse la factura antes de cancelar la cita.");
            cita.Estado = nuevoEstado;
            cita.FechaActualizacion = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(observacion))
                cita.Observaciones = string.IsNullOrWhiteSpace(cita.Observaciones)
                    ? observacion.Trim()
                    : cita.Observaciones + Environment.NewLine + observacion.Trim();
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<AdminFacturasPageViewModel> GetFacturasAsync(string busqueda, string estado, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var query = _context.Facturas.AsNoTracking()
                .Include(f => f.Paciente)
                .Include(f => f.Fisioterapeuta)
                .Include(f => f.Pagos)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim();
                query = query.Where(f => f.NumeroFactura.Contains(term) || f.Paciente.Nombres.Contains(term) || f.Paciente.Apellidos.Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(f => f.Estado == estado.Trim().ToUpperInvariant());
            if (fechaDesde.HasValue) query = query.Where(f => f.Fecha >= fechaDesde.Value.Date);
            if (fechaHasta.HasValue) query = query.Where(f => f.Fecha < fechaHasta.Value.Date.AddDays(1));

            var facturas = await query.OrderByDescending(f => f.Fecha).ThenByDescending(f => f.FacturaId).ToListAsync();
            var todas = await _context.Facturas.AsNoTracking().ToListAsync();
            return new AdminFacturasPageViewModel
            {
                Busqueda = busqueda,
                Estado = estado,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                TotalCobrado = todas.Where(f => f.Estado == PagoEstados.Pagado).Sum(f => f.Monto),
                TotalPendiente = todas.Where(f => f.Estado == PagoEstados.Pendiente).Sum(f => f.Monto),
                TotalCancelado = todas.Where(f => f.Estado == PagoEstados.Cancelado || f.Estado == PagoEstados.Reembolsado).Sum(f => f.Monto),
                Facturas = facturas.Select(f => new AdminFacturaGestionViewModel
                {
                    FacturaId = f.FacturaId,
                    NumeroFactura = f.NumeroFactura,
                    PacienteNombre = f.Paciente?.NombreCompleto ?? "Paciente",
                    FisioterapeutaNombre = f.Fisioterapeuta?.NombreCompleto,
                    Monto = f.Monto,
                    Fecha = f.Fecha,
                    Estado = f.Estado,
                    MetodoPago = f.Pagos.OrderByDescending(p => p.FechaRegistro).Select(p => p.MetodoPago).FirstOrDefault()
                }).ToList()
            };
        }

        public async Task<(bool Success, string Error)> RegistrarPagoAdminAsync(int facturaId, string metodoPago, string referencia)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (referencia?.Length > 100 || metodoPago?.Length > 30) return (false, "Método o referencia demasiado largos.");
            var factura = await _context.Facturas.Include(f => f.DetallesFactura).FirstOrDefaultAsync(f => f.FacturaId == facturaId);
            if (factura == null) return (false, "Factura no encontrada.");
            if (factura.Estado != PagoEstados.Pendiente) return (false, "Solo se pueden pagar facturas pendientes.");

            var pendiente = await _context.Pagos.FirstOrDefaultAsync(p => p.FacturaId == facturaId && p.Estado == PagoEstados.Pendiente);
            if (pendiente != null)
            {
                pendiente.Estado = PagoEstados.Pagado;
                pendiente.FechaPago = DateTime.Now;
            }
            else _context.Pagos.Add(new Pago
            {
                FacturaId = facturaId,
                MetodoPago = string.IsNullOrWhiteSpace(metodoPago) ? "EFECTIVO" : metodoPago.Trim().ToUpperInvariant(),
                Monto = factura.Monto,
                Estado = PagoEstados.Pagado,
                Referencia = referencia?.Trim(),
                FechaPago = DateTime.Now,
                FechaRegistro = DateTime.Now
            });
            factura.Estado = PagoEstados.Pagado;

            var citaIds = factura.DetallesFactura.Where(d => d.CitaId.HasValue).Select(d => d.CitaId.Value).ToList();
            var citas = await _context.Citas.Where(c => citaIds.Contains(c.CitaId)).ToListAsync();
            foreach (var cita in citas.Where(c => c.Estado == CitaEstados.Solicitada || c.Estado == CitaEstados.PendientePago))
            {
                cita.Estado = CitaEstados.Confirmada;
                cita.FechaActualizacion = DateTime.Now;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> CambiarEstadoFacturaAsync(int facturaId, string nuevoEstado)
        {
            nuevoEstado = (nuevoEstado ?? string.Empty).Trim().ToUpperInvariant();
            if (nuevoEstado != PagoEstados.Cancelado && nuevoEstado != PagoEstados.Reembolsado)
                return (false, "El estado solicitado no es válido.");

            var factura = await _context.Facturas.Include(f => f.Pagos).Include(f => f.DetallesFactura).FirstOrDefaultAsync(f => f.FacturaId == facturaId);
            if (factura == null) return (false, "Factura no encontrada.");
            if (nuevoEstado == PagoEstados.Cancelado && factura.Estado != PagoEstados.Pendiente)
                return (false, "Solo se pueden cancelar facturas pendientes.");
            if (nuevoEstado == PagoEstados.Reembolsado && factura.Estado != PagoEstados.Pagado)
                return (false, "Solo se pueden reembolsar facturas pagadas.");

            factura.Estado = nuevoEstado;
            if (nuevoEstado == PagoEstados.Reembolsado)
                foreach (var pago in factura.Pagos.Where(p => p.Estado == PagoEstados.Pagado)) pago.Estado = PagoEstados.Reembolsado;
            var citaIds = factura.DetallesFactura.Where(d => d.CitaId.HasValue).Select(d => d.CitaId.Value).ToList();
            var citas = await _context.Citas.Where(c => citaIds.Contains(c.CitaId)).ToListAsync();
            foreach (var cita in citas.Where(c => c.Estado != CitaEstados.Atendida && c.Estado != CitaEstados.EnAtencion))
            {
                cita.Estado = CitaEstados.Cancelada;
                cita.FechaActualizacion = DateTime.Now;
            }
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private IQueryable<Usuario> TerapeutasQuery()
        {
            return _context.Usuarios.Include(u => u.Rol)
                .Where(u => u.Rol.Nombre == Roles.Fisioterapeuta)
                .OrderBy(u => u.Apellidos).ThenBy(u => u.Nombres);
        }

        private async Task<List<AdminAlertaViewModel>> BuildAlertasAsync()
        {
            var hoy = DateTime.Today;
            var limite = hoy.AddDays(-21);
            var alertas = new List<AdminAlertaViewModel>();

            var inasistencias = await _context.Citas
                .Include(c => c.Paciente)
                .Where(c => c.Estado == CitaEstados.Programada && c.Fecha < hoy)
                .OrderBy(c => c.Fecha)
                .Take(3)
                .ToListAsync();

            foreach (var c in inasistencias)
            {
                alertas.Add(new AdminAlertaViewModel
                {
                    Tipo = "inasistencia",
                    CssDot = "red",
                    Mensaje = $"Paciente {c.Paciente?.NombreCompleto}: cita del {c.Fecha:dd/MM} sin atender."
                });
            }

            var sinActividad = await _context.Pacientes
                .Where(p => p.Estado)
                .ToListAsync();

            var citas = await _context.Citas.Select(c => new { c.PacienteId, c.Fecha }).ToListAsync();
            foreach (var p in sinActividad)
            {
                var last = citas.Where(c => c.PacienteId == p.PacienteId).Select(c => (DateTime?)c.Fecha).DefaultIfEmpty().Max();
                if (last.HasValue && last.Value.Date <= limite)
                {
                    alertas.Add(new AdminAlertaViewModel
                    {
                        Tipo = "inactividad",
                        CssDot = "orange",
                        Mensaje = $"{p.NombreCompleto}: sin citas desde hace {(hoy - last.Value.Date).Days} días."
                    });
                }
                if (alertas.Count >= 4) break;
            }

            return alertas.Take(4).ToList();
        }

        private async Task<int> ContarPacientesRiesgoAsync()
        {
            var hoy = DateTime.Today;
            var pacientes = await _context.Pacientes.ToListAsync();
            var citas = await _context.Citas.ToListAsync();
            return pacientes.Count(p => ClasificarPaciente(p, citas.Where(c => c.PacienteId == p.PacienteId).ToList(), hoy).Css == "riesgo");
        }

        private static List<AdminCargaTerapeutaViewModel> BuildCarga(List<Usuario> terapeutas, List<Cita> citas)
        {
            return terapeutas.Select((t, idx) =>
            {
                var pac = citas.Where(c => c.FisioterapeutaId == t.UsuarioId).Select(c => c.PacienteId).Distinct().Count();
                var pct = Math.Min(100, (int)Math.Round(pac * 100.0 / CapacidadBase));
                return new AdminCargaTerapeutaViewModel
                {
                    UsuarioId = t.UsuarioId,
                    Nombre = Prefijo(t.NombreCompleto),
                    Iniciales = Iniciales(t.NombreCompleto),
                    Color = ColorDe(idx),
                    Pacientes = pac,
                    Capacidad = CapacidadBase,
                    Porcentaje = pct,
                    BarClass = BarClass(pct)
                };
            }).ToList();
        }

        private static List<AdminCargaTerapeutaViewModel> BuildCargaHoy(List<Usuario> terapeutas, List<Cita> citasHoy)
        {
            const int slots = 8;
            return terapeutas.Select((t, idx) =>
            {
                var n = citasHoy.Count(c => c.FisioterapeutaId == t.UsuarioId && c.Estado != CitaEstados.Cancelada);
                var pct = Math.Min(100, (int)Math.Round(n * 100.0 / slots));
                return new AdminCargaTerapeutaViewModel
                {
                    UsuarioId = t.UsuarioId,
                    Nombre = Prefijo(t.NombreCompleto),
                    Iniciales = Iniciales(t.NombreCompleto),
                    Color = ColorDe(idx),
                    Pacientes = n,
                    Capacidad = slots,
                    Porcentaje = pct,
                    BarClass = BarClass(pct)
                };
            }).ToList();
        }

        private static AdminTerapeutaRowViewModel MapTerapeutaRow(Usuario t, List<Cita> citas, int idx)
        {
            var tc = citas.Where(c => c.FisioterapeutaId == t.UsuarioId).ToList();
            var pac = tc.Select(c => c.PacienteId).Distinct().Count();
            var pct = Math.Min(100, (int)Math.Round(pac * 100.0 / CapacidadBase));
            var att = tc.Count(c => c.Estado == CitaEstados.Atendida);
            var cerr = tc.Count(c => c.Estado == CitaEstados.Atendida || c.Estado == CitaEstados.Cancelada);
            return new AdminTerapeutaRowViewModel
            {
                UsuarioId = t.UsuarioId,
                Nombre = Prefijo(t.NombreCompleto),
                Iniciales = Iniciales(t.NombreCompleto),
                Color = ColorDe(idx),
                Correo = t.Correo,
                Telefono = t.Telefono,
                Desde = t.FechaCreacion.ToString("MMM yyyy", Es),
                Pacientes = pac,
                Capacidad = CapacidadBase,
                Porcentaje = pct,
                BarClass = BarClass(pct),
                AsistenciaPorcentaje = cerr == 0 ? 0 : (int)Math.Round(att * 100.0 / cerr),
                Estado = t.Estado,
                EstadoTexto = t.Estado ? "Activo" : "Inactivo",
                EstadoClass = t.Estado ? "activo" : "pendiente"
            };
        }

        private static List<AdminBarraDiaViewModel> BuildSemana(List<Cita> citas, DateTime hoy)
        {
            var start = hoy.AddDays(-(int)hoy.DayOfWeek + (hoy.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
            var list = new List<AdminBarraDiaViewModel>();
            for (var i = 0; i < 6; i++)
            {
                var d = start.AddDays(i);
                var day = citas.Where(c => c.Fecha.Date == d).ToList();
                list.Add(new AdminBarraDiaViewModel
                {
                    Dia = Capitalizar(d.ToString("ddd", Es)).Replace(".", ""),
                    Confirmadas = day.Count(c => c.Estado == CitaEstados.Atendida),
                    Pendientes = day.Count(c => c.Estado == CitaEstados.Programada),
                    Canceladas = day.Count(c => c.Estado == CitaEstados.Cancelada),
                    Total = day.Count
                });
            }
            return list;
        }

        private static List<AdminPuntoMesViewModel> BuildMensual(List<Cita> citas, DateTime hoy, int meses)
        {
            var list = new List<AdminPuntoMesViewModel>();
            var cursor = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-(meses - 1));
            for (var i = 0; i < meses; i++)
            {
                var start = cursor.AddMonths(i);
                var end = start.AddMonths(1);
                var m = citas.Where(c => c.Fecha >= start && c.Fecha < end).ToList();
                list.Add(new AdminPuntoMesViewModel
                {
                    Mes = Capitalizar(start.ToString("MMM", Es)).Replace(".", ""),
                    Atendidas = m.Count(c => c.Estado == CitaEstados.Atendida),
                    Programadas = m.Count(c => c.Estado == CitaEstados.Programada),
                    Total = m.Count
                });
            }
            return list;
        }

        private static List<AdminPuntoMesViewModel> SemanaComoSerie(List<Cita> citas, DateTime hoy)
        {
            return BuildSemana(citas, hoy).Select(d => new AdminPuntoMesViewModel
            {
                Mes = d.Dia,
                Atendidas = d.Confirmadas,
                Programadas = d.Pendientes,
                Total = d.Total
            }).ToList();
        }

        private static List<AdminPlanResumenViewModel> BuildPlanes(List<Cita> citas)
        {
            return citas
                .Where(c => !string.IsNullOrWhiteSpace(c.MotivoConsulta))
                .GroupBy(c => c.MotivoConsulta.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var last = g.OrderByDescending(c => c.Fecha).First();
                    return new AdminPlanResumenViewModel
                    {
                        Titulo = g.Key,
                        Autor = last.Fisioterapeuta == null ? "Equipo clínico" : Prefijo(last.Fisioterapeuta.NombreCompleto),
                        Usos = g.Count(),
                        Actualizado = last.Fecha.ToString("d MMM", Es)
                    };
                })
                .OrderByDescending(p => p.Usos)
                .ToList();
        }

        private static AdminCitaRecienteViewModel MapCita(Cita c)
        {
            return new AdminCitaRecienteViewModel
            {
                Paciente = c.Paciente?.NombreCompleto ?? "Paciente",
                Terapeuta = c.Fisioterapeuta == null ? "—" : Prefijo(c.Fisioterapeuta.NombreCompleto),
                Fecha = c.Fecha.ToString("dd/MM/yyyy"),
                Hora = c.HoraInicio.ToString(@"hh\:mm"),
                Motivo = Texto(c.MotivoConsulta, "Consulta"),
                Estado = CitaEstados.Etiqueta(c.Estado),
                EstadoClass = c.Estado == CitaEstados.Atendida ? "activo" : c.Estado == CitaEstados.Cancelada ? "riesgo" : "pendiente"
            };
        }

        private static (string Texto, string Css) ClasificarPaciente(Paciente p, List<Cita> pc, DateTime hoy)
        {
            if (!p.Estado) return ("Inactivo", "pendiente");
            if (p.FechaRegistro.Date >= new DateTime(hoy.Year, hoy.Month, 1)) return ("Nuevo", "nuevo");
            var last = pc.OrderByDescending(c => c.Fecha).FirstOrDefault();
            if (last == null) return ("Nuevo", "nuevo");
            if (last.Estado == CitaEstados.Cancelada || last.Fecha.Date <= hoy.AddDays(-21)) return ("Riesgo", "riesgo");
            return ("Activo", "activo");
        }

        private static bool EstaEnCurso(Cita c, TimeSpan ahora)
        {
            var fin = c.HoraFin ?? c.HoraInicio.Add(TimeSpan.FromMinutes(45));
            return c.HoraInicio <= ahora && ahora < fin;
        }

        private static string FormatoRango(Cita c)
        {
            var fin = c.HoraFin ?? c.HoraInicio.Add(TimeSpan.FromMinutes(45));
            return $"{c.HoraInicio:hh\\:mm}–{fin:hh\\:mm}";
        }

        private static string Relativa(DateTime fecha, DateTime hoy)
        {
            var d = (fecha.Date - hoy).Days;
            if (d == 0) return "Hoy";
            if (d == 1) return "Mañana";
            if (d < 7) return Capitalizar(fecha.ToString("ddd", Es)).Replace(".", "");
            return fecha.ToString("dd/MM");
        }

        private static int Edad(DateTime nacimiento)
        {
            var today = DateTime.Today;
            var age = today.Year - nacimiento.Year;
            if (nacimiento.Date > today.AddYears(-age)) age--;
            return Math.Max(0, age);
        }

        private static string BarClass(int pct)
        {
            if (pct >= 80) return "high";
            if (pct >= 55) return "mid";
            return "low";
        }

        private static string ColorDe(int seed) => AvatarColors[Math.Abs(seed) % AvatarColors.Length];

        private static string Iniciales(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "FS";
            var parts = nombre.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0][0].ToString() + parts[parts.Length - 1][0]).ToUpperInvariant();
        }

        private static string Prefijo(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "Terapeuta";
            if (nombre.StartsWith("Dr", StringComparison.OrdinalIgnoreCase) || nombre.StartsWith("Dra", StringComparison.OrdinalIgnoreCase))
                return nombre;
            return "Dr. " + nombre;
        }

        private static string Texto(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private static string Capitalizar(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return char.ToUpper(value[0], Es) + value.Substring(1);
        }

        private static string Delta(int actual, int previo)
        {
            var d = actual - previo;
            return d >= 0 ? $"+{d}" : d.ToString();
        }

        public async Task<List<Servicio>> GetServiciosAsync()
        {
            return await _context.Servicios.AsNoTracking().OrderBy(s => s.Nombre).ToListAsync();
        }

        public async Task<(bool Success, string Error)> SaveServicioAsync(ServicioFormViewModel model)
        {
            if (model != null && !string.IsNullOrWhiteSpace(model.Nombre))
            {
                var nombreServicio = model.Nombre.Trim();
                if (await _context.Servicios.AnyAsync(s => s.Nombre == nombreServicio && (!model.ServicioId.HasValue || s.ServicioId != model.ServicioId.Value)))
                    return (false, "Ya existe un servicio con ese nombre.");
            }
            if (model == null || string.IsNullOrWhiteSpace(model.Nombre) || model.Precio <= 0)
                return (false, "Nombre y precio válido son requeridos.");

            if (model.ServicioId.HasValue && model.ServicioId.Value > 0)
            {
                var s = await _context.Servicios.FindAsync(model.ServicioId.Value);
                if (s == null) return (false, "Servicio no encontrado.");
                s.Nombre = model.Nombre.Trim();
                s.Descripcion = model.Descripcion?.Trim();
                s.Precio = model.Precio;
                s.Tipo = string.IsNullOrWhiteSpace(model.Tipo) ? "TERAPIA" : model.Tipo.Trim();
                s.Estado = model.Estado;
            }
            else
            {
                var s = new Servicio
                {
                    Nombre = model.Nombre.Trim(),
                    Descripcion = model.Descripcion?.Trim(),
                    Precio = model.Precio,
                    Tipo = string.IsNullOrWhiteSpace(model.Tipo) ? "TERAPIA" : model.Tipo.Trim(),
                    Estado = model.Estado,
                    FechaRegistro = DateTime.Now
                };
                _context.Servicios.Add(s);
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<DisponibilidadFisioterapeuta>> GetDisponibilidadesAsync()
        {
            return await _context.DisponibilidadesFisioterapeuta
                .Include(d => d.Fisioterapeuta)
                .AsNoTracking()
                .OrderBy(d => d.FisioterapeutaId)
                .ThenBy(d => d.DiaSemana)
                .ToListAsync();
        }

        public async Task<(bool Success, string Error)> SaveDisponibilidadAsync(DisponibilidadFormViewModel model)
        {
            if (model != null)
            {
                var fisioValido = await _context.Usuarios.Include(u => u.Rol).AnyAsync(u => u.UsuarioId == model.FisioterapeutaId && u.Estado && u.Rol.Nombre == Roles.Fisioterapeuta);
                if (!fisioValido) return (false, "El fisioterapeuta seleccionado no está activo.");
                var seTraslapa = await _context.DisponibilidadesFisioterapeuta.AnyAsync(d =>
                    d.FisioterapeutaId == model.FisioterapeutaId && d.DiaSemana == model.DiaSemana && d.Estado &&
                    (!model.DisponibilidadId.HasValue || d.DisponibilidadId != model.DisponibilidadId.Value) &&
                    d.HoraInicio < model.HoraFin && model.HoraInicio < d.HoraFin);
                if (seTraslapa) return (false, "El horario se traslapa con otra disponibilidad activa.");
            }
            if (model == null || model.FisioterapeutaId <= 0 || model.DiaSemana < 1 || model.DiaSemana > 7)
                return (false, "Fisioterapeuta y día de la semana (1-7) son requeridos.");

            if (model.HoraFin <= model.HoraInicio)
                return (false, "La hora de fin debe ser posterior a la hora de inicio.");

            if (model.DisponibilidadId.HasValue && model.DisponibilidadId.Value > 0)
            {
                var d = await _context.DisponibilidadesFisioterapeuta.FindAsync(model.DisponibilidadId.Value);
                if (d == null) return (false, "Disponibilidad no encontrada.");
                d.FisioterapeutaId = model.FisioterapeutaId;
                d.DiaSemana = model.DiaSemana;
                d.HoraInicio = model.HoraInicio;
                d.HoraFin = model.HoraFin;
                d.Estado = model.Estado;
            }
            else
            {
                var d = new DisponibilidadFisioterapeuta
                {
                    FisioterapeutaId = model.FisioterapeutaId,
                    DiaSemana = model.DiaSemana,
                    HoraInicio = model.HoraInicio,
                    HoraFin = model.HoraFin,
                    Estado = model.Estado
                };
                _context.DisponibilidadesFisioterapeuta.Add(d);
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<Ejercicio>> GetEjerciciosCatalogoAsync()
        {
            return await _context.Ejercicios.AsNoTracking().OrderBy(e => e.Nombre).ToListAsync();
        }

        public async Task<(bool Success, string Error)> SaveEjercicioCatalogoAsync(Ejercicio model)
        {
            if (model != null && !string.IsNullOrWhiteSpace(model.Nombre) && await _context.Ejercicios.AnyAsync(e => e.Nombre == model.Nombre.Trim() && e.EjercicioId != model.EjercicioId))
                return (false, "Ya existe un ejercicio con ese nombre.");
            if (model == null || string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre del ejercicio es obligatorio.");

            if (model.EjercicioId > 0)
            {
                var ej = await _context.Ejercicios.FindAsync(model.EjercicioId);
                if (ej == null) return (false, "Ejercicio no encontrado.");
                ej.Nombre = model.Nombre.Trim();
                ej.Descripcion = model.Descripcion?.Trim();
                ej.DuracionMinutos = model.DuracionMinutos;
                ej.Recomendaciones = model.Recomendaciones?.Trim();
                ej.Estado = model.Estado;
            }
            else
            {
                model.Nombre = model.Nombre.Trim();
                model.Descripcion = model.Descripcion?.Trim();
                model.Recomendaciones = model.Recomendaciones?.Trim();
                model.FechaRegistro = DateTime.Now;
                _context.Ejercicios.Add(model);
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> ToggleEjercicioCatalogoAsync(int ejercicioId)
        {
            var ejercicio = await _context.Ejercicios.FindAsync(ejercicioId);
            if (ejercicio == null) return (false, "Ejercicio no encontrado.");
            ejercicio.Estado = !ejercicio.Estado;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<Patologia>> GetPatologiasAsync()
        {
            return await _context.Patologias.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();
        }

        public async Task<(bool Success, string Error)> SavePatologiaAsync(Patologia model)
        {
            if (model != null && !string.IsNullOrWhiteSpace(model.Nombre) && await _context.Patologias.AnyAsync(p => p.Nombre == model.Nombre.Trim() && p.PatologiaId != model.PatologiaId))
                return (false, "Ya existe una patología con ese nombre.");
            if (model == null || string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre de la patología es obligatorio.");

            if (model.PatologiaId > 0)
            {
                var pat = await _context.Patologias.FindAsync(model.PatologiaId);
                if (pat == null) return (false, "Patología no encontrada.");
                pat.Nombre = model.Nombre.Trim();
                pat.Descripcion = model.Descripcion?.Trim();
                pat.Estado = model.Estado;
            }
            else
            {
                model.Nombre = model.Nombre.Trim();
                model.Descripcion = model.Descripcion?.Trim();
                model.FechaRegistro = DateTime.Now;
                _context.Patologias.Add(model);
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> TogglePatologiaAsync(int patologiaId)
        {
            var patologia = await _context.Patologias.FindAsync(patologiaId);
            if (patologia == null) return (false, "Patología no encontrada.");
            patologia.Estado = !patologia.Estado;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.TrimStart().Length > 0 && "=+-@".Contains(value.TrimStart()[0])) value = "'" + value;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}

