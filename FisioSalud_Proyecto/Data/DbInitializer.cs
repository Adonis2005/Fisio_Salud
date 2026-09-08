using System;
using System.Linq;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Entities;
using FisioSalud_Proyecto.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FisioSalud_Proyecto.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FisioSaludDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<FisioSaludDbContext>>();

            try
            {
                await context.Database.ExecuteSqlRawAsync("IF OBJECT_ID('dbo.AjustesSistema','U') IS NULL CREATE TABLE dbo.AjustesSistema (Clave nvarchar(100) NOT NULL PRIMARY KEY, Valor nvarchar(max) NOT NULL);");
                await EnsurePasswordResetTableAsync(context, logger);
                await EnsureMensajesTableAsync(context);
                await EnsureAsignacionesPacienteTableAsync(context);
                await EnsureEquiposTablesAsync(context);
                await SeedRolesAsync(context);
                await SeedAdminAsync(context, passwordService);
                await SeedBusinessCatalogsAsync(context);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo inicializar datos de prueba. Verifique la conexión a SQL Server.");
            }
        }

        private static async Task EnsureEquiposTablesAsync(FisioSaludDbContext context)
        {
            const string sql = @"
                IF OBJECT_ID('EquiposTerapeuticos', 'U') IS NULL
                BEGIN
                    CREATE TABLE EquiposTerapeuticos (EquipoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EquiposTerapeuticos PRIMARY KEY, Nombre VARCHAR(120) NOT NULL, Codigo VARCHAR(30) NOT NULL, Tipo VARCHAR(50) NOT NULL, EstadoOperativo VARCHAR(20) NOT NULL DEFAULT 'DISPONIBLE', VelocidadMaxima DECIMAL(8,2) NULL, InclinacionMaxima DECIMAL(8,2) NULL, Estado BIT NOT NULL DEFAULT 1, FechaRegistro DATETIME2 NOT NULL DEFAULT SYSDATETIME(), CONSTRAINT UQ_EquiposTerapeuticos_Codigo UNIQUE (Codigo));
                END;
                IF OBJECT_ID('UsosEquipo', 'U') IS NULL
                BEGIN
                    CREATE TABLE UsosEquipo (UsoEquipoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsosEquipo PRIMARY KEY, EquipoId INT NOT NULL, PacienteId INT NOT NULL, FisioterapeutaId INT NOT NULL, CitaId INT NULL, Fecha DATE NOT NULL, HoraInicio TIME NOT NULL, HoraFin TIME NOT NULL, Velocidad DECIMAL(8,2) NULL, Inclinacion DECIMAL(8,2) NULL, Estado VARCHAR(20) NOT NULL DEFAULT 'PROGRAMADO', Indicaciones VARCHAR(1000) NULL, Resultado VARCHAR(1000) NULL, FechaRegistro DATETIME2 NOT NULL DEFAULT SYSDATETIME(), CONSTRAINT CK_UsosEquipo_Horas CHECK (HoraFin > HoraInicio), CONSTRAINT FK_UsosEquipo_Equipo FOREIGN KEY (EquipoId) REFERENCES EquiposTerapeuticos(EquipoId), CONSTRAINT FK_UsosEquipo_Paciente FOREIGN KEY (PacienteId) REFERENCES Pacientes(PacienteId), CONSTRAINT FK_UsosEquipo_Fisio FOREIGN KEY (FisioterapeutaId) REFERENCES Usuarios(UsuarioId), CONSTRAINT FK_UsosEquipo_Cita FOREIGN KEY (CitaId) REFERENCES Citas(CitaId));
                    CREATE INDEX IX_UsosEquipo_Agenda ON UsosEquipo(EquipoId, Fecha, HoraInicio, HoraFin);
                END;";
            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureAsignacionesPacienteTableAsync(FisioSaludDbContext context)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AsignacionesPaciente')
                BEGIN
                    CREATE TABLE AsignacionesPaciente (
                        AsignacionPacienteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AsignacionesPaciente PRIMARY KEY,
                        PacienteId INT NOT NULL,
                        FisioterapeutaId INT NOT NULL,
                        Estado BIT NOT NULL CONSTRAINT DF_AsignacionesPaciente_Estado DEFAULT 1,
                        FechaAsignacion DATETIME2 NOT NULL CONSTRAINT DF_AsignacionesPaciente_Fecha DEFAULT SYSDATETIME(),
                        FechaFin DATETIME2 NULL,
                        CONSTRAINT FK_AsignacionesPaciente_Pacientes FOREIGN KEY (PacienteId) REFERENCES Pacientes(PacienteId) ON DELETE CASCADE,
                        CONSTRAINT FK_AsignacionesPaciente_Fisioterapeuta FOREIGN KEY (FisioterapeutaId) REFERENCES Usuarios(UsuarioId),
                        CONSTRAINT UQ_AsignacionesPaciente UNIQUE (PacienteId, FisioterapeutaId)
                    );
                    CREATE INDEX IX_AsignacionesPaciente_FisioEstado ON AsignacionesPaciente(FisioterapeutaId, Estado);
                END";
            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task SeedBusinessCatalogsAsync(FisioSaludDbContext context)
        {
            if (!await context.Servicios.AnyAsync())
            {
                context.Servicios.AddRange(
                    new Servicio { Nombre = "Evaluación fisioterapéutica", Descripcion = "Valoración inicial y definición del tratamiento.", Precio = 30m, Tipo = "EVALUACION", Estado = true, FechaRegistro = DateTime.Now },
                    new Servicio { Nombre = "Sesión de rehabilitación", Descripcion = "Sesión individual de fisioterapia.", Precio = 25m, Tipo = "TERAPIA", Estado = true, FechaRegistro = DateTime.Now },
                    new Servicio { Nombre = "Terapia en caminadora", Descripcion = "Reeducación de marcha y acondicionamiento supervisado.", Precio = 28m, Tipo = "TERAPIA", Estado = true, FechaRegistro = DateTime.Now }
                );
            }
            if (!await context.Patologias.AnyAsync())
            {
                context.Patologias.AddRange(
                    new Patologia { Nombre = "Lumbalgia", Descripcion = "Dolor localizado en la región lumbar.", Estado = true, FechaRegistro = DateTime.Now },
                    new Patologia { Nombre = "Cervicalgia", Descripcion = "Dolor y limitación funcional cervical.", Estado = true, FechaRegistro = DateTime.Now },
                    new Patologia { Nombre = "Tendinopatía de hombro", Descripcion = "Afección de tendones del complejo del hombro.", Estado = true, FechaRegistro = DateTime.Now },
                    new Patologia { Nombre = "Rehabilitación de rodilla", Descripcion = "Recuperación funcional de rodilla traumática o posquirúrgica.", Estado = true, FechaRegistro = DateTime.Now }
                );
            }
            if (!await context.Ejercicios.AnyAsync())
            {
                context.Ejercicios.AddRange(
                    new Ejercicio { Nombre = "Puente lumbar", Descripcion = "Fortalecimiento de glúteos y estabilización lumbar.", DuracionMinutos = 10, Recomendaciones = "Realizar sin arquear la espalda.", Estado = true, FechaRegistro = DateTime.Now },
                    new Ejercicio { Nombre = "Movilidad cervical controlada", Descripcion = "Movilidad suave de cuello en rangos sin dolor.", DuracionMinutos = 8, Recomendaciones = "Detener ante mareo o dolor irradiado.", Estado = true, FechaRegistro = DateTime.Now },
                    new Ejercicio { Nombre = "Extensión de rodilla", Descripcion = "Fortalecimiento progresivo de cuádriceps.", DuracionMinutos = 12, Recomendaciones = "Mantener movimiento lento y controlado.", Estado = true, FechaRegistro = DateTime.Now },
                    new Ejercicio { Nombre = "Marcha terapéutica", Descripcion = "Reeducación del patrón de marcha en caminadora.", DuracionMinutos = 15, Recomendaciones = "Realizar únicamente con velocidad indicada por el fisioterapeuta.", Estado = true, FechaRegistro = DateTime.Now }
                );
            }
            await context.SaveChangesAsync();

            if (!await context.EquiposTerapeuticos.AnyAsync())
            {
                context.EquiposTerapeuticos.Add(new EquipoTerapeutico { Nombre = "Caminadora terapéutica 1", Codigo = "CINTA-01", Tipo = "CAMINADORA", EstadoOperativo = "DISPONIBLE", VelocidadMaxima = 16m, InclinacionMaxima = 15m, Estado = true, FechaRegistro = DateTime.Now });
                await context.SaveChangesAsync();
            }

            var fisioterapeutas = await context.Usuarios.Include(u => u.Rol)
                .Where(u => u.Estado && u.Rol.Nombre == Roles.Fisioterapeuta).Select(u => u.UsuarioId).ToListAsync();
            foreach (var fisioterapeutaId in fisioterapeutas)
            {
                if (await context.DisponibilidadesFisioterapeuta.AnyAsync(d => d.FisioterapeutaId == fisioterapeutaId)) continue;
                for (byte dia = 1; dia <= 6; dia++)
                    context.DisponibilidadesFisioterapeuta.Add(new DisponibilidadFisioterapeuta { FisioterapeutaId = fisioterapeutaId, DiaSemana = dia, HoraInicio = new TimeSpan(8, 0, 0), HoraFin = new TimeSpan(18, 0, 0), Estado = true });
            }
            if (fisioterapeutas.Any())
            {
                var responsable = fisioterapeutas.First();
                var pacientesSinAsignar = await context.Pacientes.Where(p => !context.AsignacionesPaciente.Any(a => a.PacienteId == p.PacienteId && a.Estado)).Select(p => p.PacienteId).ToListAsync();
                foreach (var pacienteId in pacientesSinAsignar)
                    context.AsignacionesPaciente.Add(new AsignacionPaciente { PacienteId = pacienteId, FisioterapeutaId = responsable, Estado = true, FechaAsignacion = DateTime.Now });
            }
            await context.SaveChangesAsync();
        }

        private static async Task EnsurePasswordResetTableAsync(FisioSaludDbContext context, ILogger logger)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResetTokens')
                BEGIN
                    CREATE TABLE PasswordResetTokens (
                        TokenId INT IDENTITY(1,1) NOT NULL,
                        UsuarioId INT NOT NULL,
                        Token VARCHAR(255) NOT NULL,
                        FechaExpiracion DATETIME2(0) NOT NULL,
                        Usado BIT NOT NULL DEFAULT 0,
                        FechaCreacion DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
                        CONSTRAINT PK_PasswordResetTokens PRIMARY KEY (TokenId),
                        CONSTRAINT FK_PasswordResetTokens_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId),
                        CONSTRAINT UQ_PasswordResetTokens_Token UNIQUE (Token)
                    );
                END";

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureMensajesTableAsync(FisioSaludDbContext context)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Mensajes')
                BEGIN
                    CREATE TABLE Mensajes (
                        MensajeId INT IDENTITY(1,1) NOT NULL,
                        RemitenteId INT NOT NULL,
                        DestinatarioId INT NOT NULL,
                        Contenido VARCHAR(2000) NOT NULL,
                        FechaEnvio DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
                        Leido BIT NOT NULL DEFAULT 0,
                        CONSTRAINT PK_Mensajes PRIMARY KEY (MensajeId),
                        CONSTRAINT FK_Mensajes_Remitente FOREIGN KEY (RemitenteId) REFERENCES Usuarios(UsuarioId),
                        CONSTRAINT FK_Mensajes_Destinatario FOREIGN KEY (DestinatarioId) REFERENCES Usuarios(UsuarioId)
                    );
                    CREATE INDEX IX_Mensajes_Conversacion ON Mensajes(RemitenteId, DestinatarioId, FechaEnvio);
                END";

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task SeedRolesAsync(FisioSaludDbContext context)
        {
            if (await context.Roles.AnyAsync()) return;

            context.Roles.AddRange(
                new Rol { Nombre = Roles.Administrador, Descripcion = "Administrador del sistema", Estado = true },
                new Rol { Nombre = Roles.Fisioterapeuta, Descripcion = "Profesional de fisioterapia", Estado = true },
                new Rol { Nombre = Roles.Cliente, Descripcion = "Cliente / Paciente", Estado = true }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedAdminAsync(FisioSaludDbContext context, IPasswordService passwordService)
        {
            if (await context.Usuarios.AnyAsync(u => u.Correo == "admin@fisiosalud.com")) return;

            var rolAdmin = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == Roles.Administrador);
            if (rolAdmin == null) return;

            context.Usuarios.Add(new Usuario
            {
                RolId = rolAdmin.RolId,
                Nombres = "Administrador",
                Apellidos = "Sistema",
                Identificacion = "0000000000",
                Correo = "admin@fisiosalud.com",
                Telefono = "0000000000",
                PasswordHash = passwordService.HashPassword("Adonis.2005"),
                Estado = true,
                FechaCreacion = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }
}
