using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Cliente;
using FisioSalud_Proyecto.Models.Contacto;
using FisioSalud_Proyecto.Models.Entities;
using Microsoft.EntityFrameworkCore;

using FisioSalud_Proyecto.Models.Clinical;

namespace FisioSalud_Proyecto.Services
{
    public interface ICitaService
    {
        Task<ClienteDashboardViewModel> GetDashboardAsync(string identificacion, string correo, string nombreCliente);
        Task<CitasListViewModel> GetCitasClienteAsync(string identificacion, string correo);
        Task<CitasPageViewModel> GetCitasPageAsync(string identificacion, string correo, string nombreCliente, DateTime? fecha, int? fisioterapeutaId, string hora, string servicio, string vista, DateTime? mes);
        Task<(bool Success, string Error)> AgendarCitaAsync(string identificacion, string correo, NuevaCitaFormViewModel form);
        Task<HistorialPageViewModel> GetHistorialAsync(string identificacion, string correo, string nombreCliente, string filtro, int? citaId);
        Task<ProgresoPageViewModel> GetProgresoAsync(string identificacion, string correo, string nombreCliente);
        Task<MensajesPageViewModel> GetMensajesAsync(string identificacion, string correo, string nombreCliente, int? fisioterapeutaId);
        Task<PerfilFormViewModel> GetPerfilAsync(string identificacion, string correo, string nombreCliente);
        Task<(bool Success, string Error)> UpdatePerfilAsync(string identificacion, string correo, PerfilFormViewModel model);
        Task<ConfiguracionPageViewModel> GetConfiguracionAsync(string identificacion, string correo, string nombreCliente);
        Task<FacturasPageViewModel> GetFacturasClienteAsync(string identificacion, string correo, string nombreCliente);
        Task<(bool Success, string Error)> RegistrarPagoAsync(RegistrarPagoFormModel model);
        Task<(bool Success, string Error)> MarcarCumplimientoEjercicioAsync(CumplimientoEjercicioFormModel model);
        Task<EvolucionPacienteViewModel> GetEvolucionPacienteAsync(int pacienteId);
        Task<(bool Success, string Error)> CancelarCitaAsync(int citaId, string identificacion, string correo);
    }

    public class CitaService : ICitaService
    {
        private static readonly CultureInfo CulturaEs = new CultureInfo("es-ES");
        private readonly FisioSaludDbContext _context;

        public CitaService(FisioSaludDbContext context)
        {
            _context = context;
        }

        public async Task<ClienteDashboardViewModel> GetDashboardAsync(string identificacion, string correo, string nombreCliente)
        {
            var ctx = await LoadContextoAsync(identificacion, correo, nombreCliente);
            var model = new ClienteDashboardViewModel();
            FillHeader(model, ctx);

            if (ctx.Paciente == null)
                return model;

            var citas = ctx.Citas;
            var hoy = DateTime.Today;

            model.TotalCitas = citas.Count;
            model.CitasProgramadas = citas.Count(c => c.Estado == CitaEstados.Programada);
            model.CitasAtendidas = citas.Count(c => c.Estado == CitaEstados.Atendida);
            model.CitasCanceladas = citas.Count(c => c.Estado == CitaEstados.Cancelada);
            model.AsistenciaPorcentaje = CalcularAsistencia(citas);
            model.EvolucionSemanal = ConstruirEvolucion(citas);
            model.FechaInicioTratamiento = FechaInicio(ctx.Paciente, citas);

            model.ProximaCita = citas
                .Where(c => c.Fecha.Date >= hoy && c.Estado == CitaEstados.Programada)
                .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
                .FirstOrDefault();

            model.UltimaCita = citas
                .Where(c => c.Fecha.Date < hoy || c.Estado == CitaEstados.Atendida)
                .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.HoraInicio)
                .FirstOrDefault();

            model.ProximasCitas = citas
                .Where(c => c.Fecha.Date >= hoy && c.Estado == CitaEstados.Programada)
                .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
                .Take(4)
                .ToList();

            model.CitasRecientes = citas.Take(5).ToList();

            var terapeuta = TerapeutaPrincipal(citas);
            if (terapeuta != null)
            {
                model.TerapeutaAsignado = terapeuta.Fisioterapeuta;
                model.TerapeutaIniciales = terapeuta.FisioterapeutaIniciales;
            }

            model.UltimaNotaClinica = citas
                .Where(c => !string.IsNullOrWhiteSpace(c.Observaciones))
                .OrderByDescending(c => c.Fecha)
                .Select(c => c.Observaciones)
                .FirstOrDefault();

            return model;
        }

        public async Task<CitasListViewModel> GetCitasClienteAsync(string identificacion, string correo)
        {
            var paciente = await FindPacienteAsync(identificacion, correo);
            if (paciente == null)
                return new CitasListViewModel();

            var citas = await GetCitasQuery(paciente.PacienteId)
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.HoraInicio)
                .ToListAsync();

            return new CitasListViewModel { Citas = citas };
        }

        public async Task<CitasPageViewModel> GetCitasPageAsync(
            string identificacion,
            string correo,
            string nombreCliente,
            DateTime? fecha,
            int? fisioterapeutaId,
            string hora,
            string servicio,
            string vista,
            DateTime? mes)
        {
            var ctx = await LoadContextoAsync(identificacion, correo, nombreCliente);
            var model = new CitasPageViewModel
            {
                Vista = string.IsNullOrWhiteSpace(vista) ? "nueva" : vista,
                Servicios = ReservarCitaViewModel.ServiciosDisponibles.ToList(),
                Formulario = new NuevaCitaFormViewModel
                {
                    Fecha = (fecha ?? DateTime.Today).Date,
                    FisioterapeutaId = fisioterapeutaId,
                    Hora = hora,
                    Servicio = servicio
                }
            };
            FillHeader(model, ctx);

            var mesVisible = (mes ?? model.Formulario.Fecha).Date;
            mesVisible = new DateTime(mesVisible.Year, mesVisible.Month, 1);
            model.MesVisible = mesVisible;
            model.MesEtiqueta = mesVisible.ToString("MMMM yyyy", CulturaEs);
            if (!string.IsNullOrEmpty(model.MesEtiqueta))
                model.MesEtiqueta = char.ToUpper(model.MesEtiqueta[0]) + model.MesEtiqueta.Substring(1);

            var fisios = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Estado && u.Rol.Nombre == Roles.Fisioterapeuta)
                .OrderBy(u => u.Nombres)
                .ToListAsync();

            model.Especialistas = fisios.Select(u => new EspecialistaOpcionViewModel
            {
                UsuarioId = u.UsuarioId,
                NombreCompleto = u.NombreCompleto,
                Iniciales = InicialesDe(u.NombreCompleto),
                Especialidad = string.IsNullOrWhiteSpace(u.Rol?.Descripcion) ? "Fisioterapia" : u.Rol.Descripcion
            }).ToList();

            if (!model.Formulario.FisioterapeutaId.HasValue && model.Especialistas.Any())
                model.Formulario.FisioterapeutaId = model.Especialistas[0].UsuarioId;

            var especialista = model.Especialistas.FirstOrDefault(e => e.UsuarioId == model.Formulario.FisioterapeutaId);
            model.EspecialistaNombre = especialista?.NombreCompleto;

            var ocupadas = new HashSet<string>();
            if (model.Formulario.FisioterapeutaId.HasValue)
            {
                var ocupadasDia = await _context.Citas
                    .Where(c => c.FisioterapeutaId == model.Formulario.FisioterapeutaId
                                && c.Fecha == model.Formulario.Fecha
                                && c.Estado != CitaEstados.Cancelada)
                    .Select(c => c.HoraInicio)
                    .ToListAsync();
                ocupadas = ocupadasDia.Select(h => h.ToString(@"hh\:mm")).ToHashSet();
            }

            for (var h = 8; h <= 18; h++)
            {
                var etiqueta = $"{h:00}:00";
                model.Horarios.Add(new HorarioSlotViewModel
                {
                    Hora = etiqueta,
                    Ocupado = ocupadas.Contains(etiqueta),
                    Seleccionado = model.Formulario.Hora == etiqueta && !ocupadas.Contains(etiqueta)
                });
            }

            model.Calendario = ConstruirCalendario(mesVisible, model.Formulario.Fecha);

            if (ctx.Paciente != null)
            {
                model.Citas = ctx.Citas
                    .OrderByDescending(c => c.Fecha)
                    .ThenByDescending(c => c.HoraInicio)
                    .ToList();
            }

            return model;
        }

        public async Task<(bool Success, string Error)> AgendarCitaAsync(string identificacion, string correo, NuevaCitaFormViewModel form)
        {
            var paciente = await FindPacienteAsync(identificacion, correo);
            if (paciente == null)
                return (false, "No encontramos tu expediente de paciente. Contacta a recepción.");

            if (!form.FisioterapeutaId.HasValue)
                return (false, "Selecciona un especialista.");

            if (string.IsNullOrWhiteSpace(form.Servicio))
                return (false, "Selecciona el tipo de servicio.");

            if (string.IsNullOrWhiteSpace(form.Hora) || !TimeSpan.TryParse(form.Hora, out var horaInicio))
                return (false, "Selecciona un horario disponible.");

            if (form.Fecha.Date < DateTime.Today)
                return (false, "No puedes agendar una cita en una fecha pasada.");

            var fisio = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == form.FisioterapeutaId && u.Estado);

            if (fisio == null || fisio.Rol?.Nombre != Roles.Fisioterapeuta)
                return (false, "El especialista seleccionado no está disponible.");

            // Validar disponibilidad horaria configurada del fisioterapeuta
            byte diaSemana = (byte)(form.Fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)form.Fecha.DayOfWeek);
            var disponibilidades = await _context.DisponibilidadesFisioterapeuta
                .Where(d => d.FisioterapeutaId == form.FisioterapeutaId.Value && d.DiaSemana == diaSemana && d.Estado)
                .ToListAsync();

            if (disponibilidades.Any())
            {
                bool dentroHorario = disponibilidades.Any(d => horaInicio >= d.HoraInicio && horaInicio < d.HoraFin);
                if (!dentroHorario)
                    return (false, "El especialista no atiende en el horario seleccionado.");
            }

            // Validar que el fisioterapeuta no esté ocupado
            var ocupadaFisio = await _context.Citas.AnyAsync(c =>
                c.FisioterapeutaId == form.FisioterapeutaId
                && c.Fecha == form.Fecha.Date
                && c.HoraInicio == horaInicio
                && c.Estado != CitaEstados.Cancelada);

            if (ocupadaFisio)
                return (false, "El especialista ya tiene una cita agendada en ese horario. Elige otro.");

            // Validar que el paciente no tenga otra cita al mismo tiempo
            var ocupadaPaciente = await _context.Citas.AnyAsync(c =>
                c.PacienteId == paciente.PacienteId
                && c.Fecha == form.Fecha.Date
                && c.HoraInicio == horaInicio
                && c.Estado != CitaEstados.Cancelada);

            if (ocupadaPaciente)
                return (false, "Ya tienes una cita agendada en esta misma fecha y hora.");

            // Obtener Servicio
            var servicioObj = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == form.Servicio && s.Estado)
                               ?? await _context.Servicios.FirstOrDefaultAsync(s => s.Estado);

            var nuevaCita = new Cita
            {
                PacienteId = paciente.PacienteId,
                FisioterapeutaId = form.FisioterapeutaId.Value,
                ServicioId = servicioObj?.ServicioId,
                Fecha = form.Fecha.Date,
                HoraInicio = horaInicio,
                HoraFin = horaInicio.Add(TimeSpan.FromHours(1)),
                MotivoConsulta = form.Servicio.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(form.Mensaje) ? null : form.Mensaje.Trim(),
                Estado = CitaEstados.PendientePago,
                FechaRegistro = DateTime.Now
            };

            _context.Citas.Add(nuevaCita);
            await _context.SaveChangesAsync();

            // Generar Factura en estado Pendiente
            decimal montoFactura = servicioObj?.Precio ?? 35.00m;
            var nuevaFactura = new Factura
            {
                NumeroFactura = "FAC-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                PacienteId = paciente.PacienteId,
                FisioterapeutaId = form.FisioterapeutaId.Value,
                Monto = montoFactura,
                Fecha = DateTime.Today,
                Estado = PagoEstados.Pendiente,
                FechaRegistro = DateTime.Now,
                UsuarioId = paciente.UsuarioId
            };

            _context.Facturas.Add(nuevaFactura);
            await _context.SaveChangesAsync();

            _context.DetallesFactura.Add(new DetalleFactura
            {
                FacturaId = nuevaFactura.FacturaId,
                CitaId = nuevaCita.CitaId,
                ServicioId = servicioObj?.ServicioId,
                Concepto = servicioObj != null ? servicioObj.Nombre : form.Servicio,
                Cantidad = 1,
                PrecioUnitario = montoFactura,
                Subtotal = montoFactura,
                FechaRegistro = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> RegistrarPagoAsync(RegistrarPagoFormModel model)
        {
            if (model == null || model.FacturaId <= 0)
                return (false, "Factura no válida.");

            var factura = await _context.Facturas
                .Include(f => f.DetallesFactura)
                .FirstOrDefaultAsync(f => f.FacturaId == model.FacturaId);

            if (factura == null)
                return (false, "Factura no encontrada.");

            if (factura.Estado == PagoEstados.Pagado)
                return (false, "La factura ya se encuentra pagada.");

            var pago = new Pago
            {
                FacturaId = factura.FacturaId,
                MetodoPago = model.MetodoPago ?? "Transferencia",
                Monto = model.Monto > 0 ? model.Monto : factura.Monto,
                Estado = PagoEstados.Pagado,
                Referencia = model.Referencia,
                FechaPago = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            _context.Pagos.Add(pago);
            factura.Estado = PagoEstados.Pagado;

            // Confirmar citas asociadas a la factura
            foreach (var detalle in factura.DetallesFactura)
            {
                if (detalle.CitaId.HasValue)
                {
                    var cita = await _context.Citas.FindAsync(detalle.CitaId.Value);
                    if (cita != null && (cita.Estado == CitaEstados.PendientePago || cita.Estado == CitaEstados.Solicitada))
                    {
                        cita.Estado = CitaEstados.Confirmada;
                        cita.FechaActualizacion = DateTime.Now;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> MarcarCumplimientoEjercicioAsync(CumplimientoEjercicioFormModel model)
        {
            if (model == null || model.TratamientoEjercicioId <= 0)
                return (false, "Asignación de ejercicio no válida.");

            var asignacion = await _context.TratamientoEjercicios
                .Include(t => t.PlanTratamiento)
                .FirstOrDefaultAsync(t => t.TratamientoEjercicioId == model.TratamientoEjercicioId);

            if (asignacion == null)
                return (false, "Ejercicio asignado no encontrado.");

            int pacienteId = model.PacienteId > 0 ? model.PacienteId : asignacion.PlanTratamiento.PacienteId;

            var registro = new EjercicioRealizado
            {
                TratamientoEjercicioId = model.TratamientoEjercicioId,
                PacienteId = pacienteId,
                FechaRealizacion = DateTime.Now,
                SeriesRealizadas = model.SeriesRealizadas ?? asignacion.Series,
                RepeticionesRealizadas = model.RepeticionesRealizadas ?? asignacion.Repeticiones,
                Dolor = model.Dolor,
                Estado = "REALIZADO",
                Comentarios = model.Comentarios
            };

            _context.EjerciciosRealizados.Add(registro);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<EvolucionPacienteViewModel> GetEvolucionPacienteAsync(int pacienteId)
        {
            var paciente = await _context.Pacientes.FindAsync(pacienteId);
            if (paciente == null) return new EvolucionPacienteViewModel();

            var evalInicial = await _context.EvaluacionesIniciales
                .Where(e => e.PacienteId == pacienteId)
                .OrderBy(e => e.FechaEvaluacion)
                .FirstOrDefaultAsync();

            var sesiones = await _context.SesionesRehabilitacion
                .Include(s => s.PlanTratamiento)
                .Include(s => s.Seguimientos)
                .Where(s => s.PlanTratamiento.PacienteId == pacienteId)
                .OrderByDescending(s => s.FechaSesion)
                .ToListAsync();

            var ejerciciosRealizados = await _context.EjerciciosRealizados
                .Include(e => e.TratamientoEjercicio)
                .ThenInclude(te => te.Ejercicio)
                .Where(e => e.PacienteId == pacienteId)
                .OrderByDescending(e => e.FechaRealizacion)
                .ToListAsync();

            var ultSeguimiento = sesiones.SelectMany(s => s.Seguimientos).OrderByDescending(s => s.FechaRegistro).FirstOrDefault();

            return new EvolucionPacienteViewModel
            {
                PacienteId = paciente.PacienteId,
                PacienteNombre = paciente.NombreCompleto,
                Identificacion = paciente.Identificacion,
                DolorInicial = evalInicial?.DolorInicial,
                DolorActual = ultSeguimiento?.NivelDolor ?? evalInicial?.DolorInicial,
                SesionesRealizadas = sesiones.Count,
                Sesiones = sesiones.Select(s => {
                    var seg = s.Seguimientos.FirstOrDefault();
                    return new SesionEvolucionItem
                    {
                        SesionId = s.SesionId,
                        Fecha = s.FechaSesion,
                        NumeroSesion = s.NumeroSesion,
                        Actividades = s.ActividadesRealizadas,
                        Dolor = seg?.NivelDolor,
                        Movilidad = seg?.MovilidadArticular,
                        Fuerza = seg?.FuerzaMuscular,
                        Recuperacion = seg?.GradoRecuperacion,
                        Observaciones = s.Observaciones
                    };
                }).ToList(),
                EjerciciosCumplidos = ejerciciosRealizados.Select(e => new EjercicioCumplimientoItem
                {
                    EjercicioNombre = e.TratamientoEjercicio?.Ejercicio?.Nombre ?? "Ejercicio",
                    Fecha = e.FechaRealizacion,
                    Series = e.SeriesRealizadas,
                    Repeticiones = e.RepeticionesRealizadas,
                    Dolor = e.Dolor,
                    Comentarios = e.Comentarios
                }).ToList()
            };
        }

        public async Task<(bool Success, string Error)> CancelarCitaAsync(int citaId, string identificacion, string correo)
        {
            var paciente = await FindPacienteAsync(identificacion, correo);
            if (paciente == null)
                return (false, "Expediente de paciente no encontrado.");

            var cita = await _context.Citas.FirstOrDefaultAsync(c => c.CitaId == citaId && c.PacienteId == paciente.PacienteId);
            if (cita == null)
                return (false, "Cita no encontrada.");

            if (cita.Estado == CitaEstados.Atendida)
                return (false, "No se puede cancelar una cita que ya fue atendida.");

            cita.Estado = CitaEstados.Cancelada;
            cita.FechaActualizacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<HistorialPageViewModel> GetHistorialAsync(string identificacion, string correo, string nombreCliente, string filtro, int? citaId)
        {
            var ctx = await LoadContextoAsync(identificacion, correo, nombreCliente);
            var model = new HistorialPageViewModel { Filtro = (filtro ?? "TODAS").ToUpperInvariant() };
            FillHeader(model, ctx);

            if (ctx.Paciente == null)
                return model;

            var citas = ctx.Citas;
            IEnumerable<CitaClienteViewModel> filtradas = citas;

            if (model.Filtro == "COMPLETADA")
                filtradas = citas.Where(c => c.Estado == CitaEstados.Atendida);
            else if (model.Filtro == "PROGRAMADA")
                filtradas = citas.Where(c => c.Estado == CitaEstados.Programada);
            else if (model.Filtro == "CANCELADA")
                filtradas = citas.Where(c => c.Estado == CitaEstados.Cancelada);

            model.Citas = filtradas.ToList();
            model.CitasAtendidas = citas.Count(c => c.Estado == CitaEstados.Atendida);
            model.CitasProgramadas = citas.Count(c => c.Estado == CitaEstados.Programada);
            model.CitasCanceladas = citas.Count(c => c.Estado == CitaEstados.Cancelada);
            model.AsistenciaPorcentaje = CalcularAsistencia(citas);

            var atendidas = citas.Where(c => c.Estado == CitaEstados.Atendida).ToList();
            model.HorasTerapia = (int)Math.Round(atendidas.Sum(c => Math.Max(c.DuracionMinutos, 60)) / 60.0);
            model.SemanasPlan = Math.Max(model.SemanaRehabilitacion, 10);

            var seleccion = model.Citas.FirstOrDefault(c => c.CitaId == citaId) ?? model.Citas.FirstOrDefault();
            if (seleccion != null)
            {
                model.CitaSeleccionadaId = seleccion.CitaId;
                model.Detalle = new CitaDetalleViewModel
                {
                    CitaId = seleccion.CitaId,
                    Fecha = seleccion.Fecha,
                    HoraInicio = seleccion.HoraInicio,
                    HoraFin = seleccion.HoraFin,
                    Fisioterapeuta = seleccion.Fisioterapeuta,
                    FisioterapeutaIniciales = seleccion.FisioterapeutaIniciales,
                    MotivoConsulta = seleccion.MotivoConsulta,
                    Estado = seleccion.Estado,
                    Observaciones = seleccion.Observaciones,
                    DuracionMinutos = seleccion.DuracionMinutos
                };
            }

            return model;
        }

        public async Task<ProgresoPageViewModel> GetProgresoAsync(string identificacion, string correo, string nombreCliente)
        {
            var ctx = await LoadContextoAsync(identificacion, correo, nombreCliente);
            var model = new ProgresoPageViewModel();
            FillHeader(model, ctx);

            if (ctx.Paciente == null)
                return model;

            var citas = ctx.Citas;
            model.TotalCitas = citas.Count;
            model.CitasAtendidas = citas.Count(c => c.Estado == CitaEstados.Atendida);
            model.CitasProgramadas = citas.Count(c => c.Estado == CitaEstados.Programada);
            model.AsistenciaPorcentaje = CalcularAsistencia(citas);
            model.CompletadasPorcentaje = citas.Count == 0 ? 0 : (int)Math.Round(100.0 * model.CitasAtendidas / citas.Count);
            model.EvolucionSemanal = ConstruirEvolucion(citas);
            model.CitasRecientes = citas.Take(8).ToList();
            model.FechaInicioTratamiento = FechaInicio(ctx.Paciente, citas);
            return model;
        }

        public async Task<MensajesPageViewModel> GetMensajesAsync(string identificacion, string correo, string nombreCliente, int? fisioterapeutaId)
        {
            var ctx = await LoadContextoAsync(identificacion, correo, nombreCliente);
            var model = new MensajesPageViewModel();
            FillHeader(model, ctx);

            model.Conversaciones = ctx.Citas
                .GroupBy(c => new { c.FisioterapeutaId, c.Fisioterapeuta, c.FisioterapeutaIniciales })
                .Select(g =>
                {
                    var ultima = g.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.HoraInicio).First();
                    return new ConversacionResumenViewModel
                    {
                        FisioterapeutaId = g.Key.FisioterapeutaId,
                        Nombre = g.Key.Fisioterapeuta,
                        Iniciales = g.Key.FisioterapeutaIniciales,
                        UltimoMotivo = ultima.MotivoConsulta,
                        UltimaFecha = ultima.Fecha,
                        UltimaNota = ultima.Observaciones
                    };
                })
                .OrderByDescending(c => c.UltimaFecha)
                .ToList();

            model.ConversacionActiva = model.Conversaciones.FirstOrDefault(c => c.FisioterapeutaId == fisioterapeutaId)
                                       ?? model.Conversaciones.FirstOrDefault();

            if (model.ConversacionActiva != null)
                model.ConversacionActiva.Seleccionada = true;

            return model;
        }

        public async Task<PerfilFormViewModel> GetPerfilAsync(string identificacion, string correo, string nombreCliente)
        {
            var ctx = await LoadContextoAsync(identificacion, correo, nombreCliente);
            var model = new PerfilFormViewModel();
            FillHeader(model, ctx);

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
                u.Identificacion == identificacion || u.Correo == correo);

            if (ctx.Paciente != null)
            {
                model.PacienteId = ctx.Paciente.PacienteId;
                model.Nombres = ctx.Paciente.Nombres;
                model.Apellidos = ctx.Paciente.Apellidos;
                model.CorreoPerfil = ctx.Paciente.Correo ?? correo;
                model.TelefonoPerfil = ctx.Paciente.Telefono;
                model.FechaNacimiento = ctx.Paciente.FechaNacimiento;
                model.Sexo = ctx.Paciente.Sexo;
                model.Direccion = ctx.Paciente.Direccion;
                model.Identificacion = ctx.Paciente.Identificacion;
                model.FechaRegistro = ctx.Paciente.FechaRegistro;
                model.DiasActivo = Math.Max(1, (int)(DateTime.Today - ctx.Paciente.FechaRegistro.Date).TotalDays);
                model.SesionesCompletadas = ctx.Citas.Count(c => c.Estado == CitaEstados.Atendida);
                model.SesionesTotales = ctx.Citas.Count;
                model.ProgresoPorcentaje = CalcularAsistencia(ctx.Citas);
                model.ProximaCita = ctx.Citas
                    .Where(c => c.Fecha.Date >= DateTime.Today && c.Estado == CitaEstados.Programada)
                    .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
                    .FirstOrDefault();

                var terapeuta = TerapeutaPrincipal(ctx.Citas);
                if (terapeuta != null)
                {
                    model.TerapeutaAsignado = terapeuta.Fisioterapeuta;
                    model.TerapeutaIniciales = terapeuta.FisioterapeutaIniciales;
                    model.TerapeutaDesde = ctx.Citas
                        .Where(c => c.FisioterapeutaId == terapeuta.FisioterapeutaId)
                        .Min(c => c.Fecha);
                }
            }
            else if (usuario != null)
            {
                model.Nombres = usuario.Nombres;
                model.Apellidos = usuario.Apellidos;
                model.CorreoPerfil = usuario.Correo;
                model.TelefonoPerfil = usuario.Telefono;
                model.Identificacion = usuario.Identificacion;
                model.FechaNacimiento = DateTime.Today.AddYears(-18);
                model.Sexo = "O";
                model.FechaRegistro = usuario.FechaCreacion;
            }

            return model;
        }

        public async Task<(bool Success, string Error)> UpdatePerfilAsync(string identificacion, string correo, PerfilFormViewModel model)
        {
            var paciente = await FindPacienteAsync(identificacion, correo);
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
                u.Identificacion == identificacion || u.Correo == correo);

            if (paciente == null && usuario == null)
                return (false, "No se encontró el perfil.");

            if (paciente != null)
            {
                paciente.Nombres = model.Nombres.Trim();
                paciente.Apellidos = model.Apellidos.Trim();
                paciente.Correo = model.CorreoPerfil?.Trim().ToLower();
                paciente.Telefono = model.TelefonoPerfil?.Trim();
                paciente.FechaNacimiento = model.FechaNacimiento;
                paciente.Sexo = string.IsNullOrWhiteSpace(model.Sexo) ? "O" : model.Sexo.Trim().Substring(0, 1).ToUpper();
                paciente.Direccion = model.Direccion?.Trim();
                paciente.FechaActualizacion = DateTime.Now;
            }

            if (usuario != null)
            {
                usuario.Nombres = model.Nombres.Trim();
                usuario.Apellidos = model.Apellidos.Trim();
                usuario.Telefono = model.TelefonoPerfil?.Trim();
                usuario.FechaActualizacion = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<ConfiguracionPageViewModel> GetConfiguracionAsync(string identificacion, string correo, string nombreCliente)
        {
            var ctx = await LoadContextoAsync(identificacion, correo, nombreCliente);
            var model = new ConfiguracionPageViewModel();
            FillHeader(model, ctx);
            model.FechaRegistroFormato = ctx.Paciente != null
                ? ctx.Paciente.FechaRegistro.ToString("d 'de' MMMM 'de' yyyy", CulturaEs)
                : null;
            return model;
        }

        public async Task<FacturasPageViewModel> GetFacturasClienteAsync(string identificacion, string correo, string nombreCliente)
        {
            var ctx = await LoadContextoAsync(identificacion, correo, nombreCliente);
            var model = new FacturasPageViewModel();
            FillHeader(model, ctx);

            if (ctx.Paciente == null)
                return model;

            var facturas = await _context.Facturas
                .Include(f => f.Fisioterapeuta)
                .Where(f => f.PacienteId == ctx.Paciente.PacienteId)
                .OrderByDescending(f => f.Fecha)
                .ToListAsync();

            model.TotalPagado = facturas.Where(f => f.Estado == "PAGADA").Sum(f => f.Monto);
            model.TotalPendiente = facturas.Where(f => f.Estado == "PENDIENTE" || f.Estado == "VENCIDA").Sum(f => f.Monto);

            model.Facturas = facturas.Select(f => new FacturaClienteItemViewModel
            {
                FacturaId = f.FacturaId,
                NumeroFactura = f.NumeroFactura,
                Fecha = f.Fecha,
                Fisioterapeuta = f.Fisioterapeuta == null ? "—" : f.Fisioterapeuta.Nombres + " " + f.Fisioterapeuta.Apellidos,
                Monto = f.Monto,
                Estado = f.Estado,
                EstadoClass = f.Estado == "PAGADA" ? "ok" : f.Estado == "VENCIDA" ? "warn" : "pending"
            }).ToList();

            return model;
        }

        private async Task<PacienteContexto> LoadContextoAsync(string identificacion, string correo, string nombreCliente)
        {
            var paciente = await FindPacienteAsync(identificacion, correo);
            var citas = paciente == null
                ? new List<CitaClienteViewModel>()
                : await GetCitasQuery(paciente.PacienteId)
                    .OrderByDescending(c => c.Fecha)
                    .ThenByDescending(c => c.HoraInicio)
                    .ToListAsync();

            return new PacienteContexto
            {
                Paciente = paciente,
                Citas = citas,
                NombreFallback = nombreCliente,
                CorreoFallback = correo
            };
        }

        private static void FillHeader(ClientePageViewModel model, PacienteContexto ctx)
        {
            var nombre = ctx.Paciente?.NombreCompleto ?? ctx.NombreFallback ?? "Paciente";
            model.NombreCompleto = nombre;
            model.Iniciales = InicialesDe(nombre);
            model.Correo = ctx.Paciente?.Correo ?? ctx.CorreoFallback;
            model.Telefono = ctx.Paciente?.Telefono;
            model.Activo = ctx.Paciente?.Estado ?? true;
            model.TieneExpediente = ctx.Paciente != null;
            model.PacienteId = ctx.Paciente?.PacienteId ?? 0;
            model.FechaHoyFormato = Capitalizar(DateTime.Now.ToString("dddd, d 'de' MMMM 'de' yyyy", CulturaEs));
            model.SemanaRehabilitacion = CalcularSemana(ctx.Paciente, ctx.Citas);
        }

        private static int CalcularSemana(Paciente paciente, List<CitaClienteViewModel> citas)
        {
            var inicio = FechaInicio(paciente, citas);
            if (!inicio.HasValue)
                return 1;
            var semanas = (int)Math.Ceiling((DateTime.Today - inicio.Value.Date).TotalDays / 7.0);
            return Math.Max(1, semanas);
        }

        private static DateTime? FechaInicio(Paciente paciente, List<CitaClienteViewModel> citas)
        {
            if (citas != null && citas.Any())
            {
                var minCita = citas.Min(c => c.Fecha.Date);
                if (paciente != null)
                    return paciente.FechaRegistro.Date < minCita ? paciente.FechaRegistro.Date : minCita;
                return minCita;
            }
            return paciente?.FechaRegistro.Date;
        }

        private static int CalcularAsistencia(List<CitaClienteViewModel> citas)
        {
            var relevantes = citas.Where(c => c.Estado == CitaEstados.Atendida || c.Estado == CitaEstados.Cancelada).ToList();
            if (!relevantes.Any())
            {
                if (!citas.Any()) return 0;
                return (int)Math.Round(100.0 * citas.Count(c => c.Estado == CitaEstados.Atendida) / citas.Count);
            }
            return (int)Math.Round(100.0 * relevantes.Count(c => c.Estado == CitaEstados.Atendida) / relevantes.Count);
        }

        private static List<SemanaChartPunto> ConstruirEvolucion(List<CitaClienteViewModel> citas)
        {
            var puntos = new List<SemanaChartPunto>();
            var inicioSemana = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
            for (var i = 5; i >= 0; i--)
            {
                var desde = inicioSemana.AddDays(-7 * i);
                var hasta = desde.AddDays(7);
                var deSemana = citas.Where(c => c.Fecha.Date >= desde && c.Fecha.Date < hasta).ToList();
                puntos.Add(new SemanaChartPunto
                {
                    Etiqueta = $"Sem {6 - i}",
                    Atendidas = deSemana.Count(c => c.Estado == CitaEstados.Atendida),
                    Programadas = deSemana.Count(c => c.Estado == CitaEstados.Programada)
                });
            }
            return puntos;
        }

        private static List<DiaCalendarioViewModel> ConstruirCalendario(DateTime mesVisible, DateTime seleccionado)
        {
            var dias = new List<DiaCalendarioViewModel>();
            var first = mesVisible;
            var offset = ((int)first.DayOfWeek + 6) % 7;
            var cursor = first.AddDays(-offset);
            for (var i = 0; i < 42; i++)
            {
                var fecha = cursor.AddDays(i);
                dias.Add(new DiaCalendarioViewModel
                {
                    Dia = fecha.Day,
                    Fecha = fecha,
                    FueraDeMes = fecha.Month != mesVisible.Month,
                    EsHoy = fecha.Date == DateTime.Today,
                    Seleccionado = fecha.Date == seleccionado.Date,
                    EsPasado = fecha.Date < DateTime.Today,
                    TieneDisponibilidad = fecha.Date >= DateTime.Today && fecha.DayOfWeek != DayOfWeek.Sunday
                });
            }
            return dias;
        }

        private static CitaClienteViewModel TerapeutaPrincipal(List<CitaClienteViewModel> citas)
        {
            return citas
                .GroupBy(c => c.FisioterapeutaId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.First())
                .FirstOrDefault();
        }

        private static string InicialesDe(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "FS";
            var partes = nombre.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1)
                return partes[0].Substring(0, Math.Min(2, partes[0].Length)).ToUpper();
            return (partes[0].Substring(0, 1) + partes[partes.Length - 1].Substring(0, 1)).ToUpper();
        }

        private static string Capitalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return texto;
            return char.ToUpper(texto[0]) + texto.Substring(1);
        }

        private async Task<Paciente> FindPacienteAsync(string identificacion, string correo)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo || u.Identificacion == identificacion);
            var usuarioId = usuario?.UsuarioId;

            return await _context.Pacientes.FirstOrDefaultAsync(p =>
                (usuarioId.HasValue && p.UsuarioId == usuarioId.Value) ||
                p.Identificacion == identificacion ||
                (p.Correo != null && p.Correo == correo));
        }

        private IQueryable<CitaClienteViewModel> GetCitasQuery(int pacienteId)
        {
            return _context.Citas
                .Include(c => c.Fisioterapeuta)
                .Where(c => c.PacienteId == pacienteId)
                .Select(c => new CitaClienteViewModel
                {
                    CitaId = c.CitaId,
                    Fecha = c.Fecha,
                    HoraInicio = c.HoraInicio,
                    HoraFin = c.HoraFin,
                    FisioterapeutaId = c.FisioterapeutaId,
                    Fisioterapeuta = c.Fisioterapeuta.Nombres + " " + c.Fisioterapeuta.Apellidos,
                    FisioterapeutaIniciales = (c.Fisioterapeuta.Nombres.Substring(0, 1) + c.Fisioterapeuta.Apellidos.Substring(0, 1)).ToUpper(),
                    MotivoConsulta = c.MotivoConsulta,
                    Observaciones = c.Observaciones,
                    Estado = c.Estado,
                    DuracionMinutos = c.HoraFin.HasValue
                        ? (int)(c.HoraFin.Value - c.HoraInicio).TotalMinutes
                        : 60
                });
        }

        private class PacienteContexto
        {
            public Paciente Paciente { get; set; }
            public List<CitaClienteViewModel> Citas { get; set; }
            public string NombreFallback { get; set; }
            public string CorreoFallback { get; set; }
        }
    }
}
