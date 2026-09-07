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
                await EnsurePasswordResetTableAsync(context, logger);
                await SeedRolesAsync(context);
                await SeedAdminAsync(context, passwordService);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo inicializar datos de prueba. Verifique la conexión a SQL Server.");
            }
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
