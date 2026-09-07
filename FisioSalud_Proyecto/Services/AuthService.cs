using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Auth;
using FisioSalud_Proyecto.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Error, Usuario User)> LoginAsync(LoginViewModel model);
        Task<(bool Success, string Error)> RegisterAsync(RegisterViewModel model);
        Task<(bool Success, string Error, string Token)> RequestPasswordResetAsync(string correo);
        Task<(bool Success, string Error)> ResetPasswordAsync(ResetPasswordViewModel model);
        Task<(bool Success, string Error)> ChangePasswordAsync(int usuarioId, string passwordActual, string passwordNueva);
        ClaimsPrincipal CreateClaimsPrincipal(Usuario usuario);
    }

    public class AuthService : IAuthService
    {
        private readonly FisioSaludDbContext _context;
        private readonly IPasswordService _passwordService;

        public AuthService(FisioSaludDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<(bool Success, string Error, Usuario User)> LoginAsync(LoginViewModel model)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Correo == model.Correo);

            if (usuario == null)
                return (false, "Credenciales incorrectas.", null);

            if (!usuario.Estado)
                return (false, "Su cuenta está bloqueada. Contacte al administrador.", null);

            if (!_passwordService.VerifyPassword(model.Password, usuario.PasswordHash))
                return (false, "Credenciales incorrectas.", null);

            usuario.UltimoAcceso = DateTime.Now;
            await _context.SaveChangesAsync();

            return (true, null, usuario);
        }

        public async Task<(bool Success, string Error)> RegisterAsync(RegisterViewModel model)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Correo == model.Correo))
                return (false, "El correo electrónico ya está registrado.");

            if (await _context.Usuarios.AnyAsync(u => u.Identificacion == model.Identificacion))
                return (false, "La identificación ya está registrada.");

            var rolCliente = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == Roles.Cliente);
            if (rolCliente == null)
                return (false, "No se encontró el rol Cliente en el sistema.");

            var usuario = new Usuario
            {
                RolId = rolCliente.RolId,
                Nombres = model.Nombres.Trim(),
                Apellidos = model.Apellidos.Trim(),
                Identificacion = model.Identificacion.Trim(),
                Correo = model.Correo.Trim().ToLower(),
                Telefono = model.Telefono?.Trim(),
                PasswordHash = _passwordService.HashPassword(model.Password),
                Estado = true,
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(usuario);

            var pacienteExistente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Identificacion == model.Identificacion);

            if (pacienteExistente == null)
            {
                _context.Pacientes.Add(new Paciente
                {
                    Nombres = model.Nombres.Trim(),
                    Apellidos = model.Apellidos.Trim(),
                    Identificacion = model.Identificacion.Trim(),
                    FechaNacimiento = DateTime.Today.AddYears(-18),
                    Sexo = "O",
                    Telefono = model.Telefono?.Trim(),
                    Correo = model.Correo.Trim().ToLower(),
                    Estado = true,
                    FechaRegistro = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error, string Token)> RequestPasswordResetAsync(string correo)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
            if (usuario == null)
                return (true, null, null);

            var token = Guid.NewGuid().ToString("N");
            var resetToken = new PasswordResetToken
            {
                UsuarioId = usuario.UsuarioId,
                Token = token,
                FechaExpiracion = DateTime.Now.AddHours(1),
                Usado = false,
                FechaCreacion = DateTime.Now
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            return (true, null, token);
        }

        public async Task<(bool Success, string Error)> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.Usuario)
                .FirstOrDefaultAsync(t => t.Token == model.Token && !t.Usado);

            if (resetToken == null)
                return (false, "El enlace de restablecimiento no es válido o ya fue utilizado.");

            if (resetToken.FechaExpiracion < DateTime.Now)
                return (false, "El enlace de restablecimiento ha expirado.");

            resetToken.Usuario.PasswordHash = _passwordService.HashPassword(model.Password);
            resetToken.Usuario.FechaActualizacion = DateTime.Now;
            resetToken.Usado = true;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> ChangePasswordAsync(int usuarioId, string passwordActual, string passwordNueva)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
            if (usuario == null)
                return (false, "No se encontró la cuenta.");

            if (!_passwordService.VerifyPassword(passwordActual, usuario.PasswordHash))
                return (false, "La contraseña actual no es correcta.");

            usuario.PasswordHash = _passwordService.HashPassword(passwordNueva);
            usuario.FechaActualizacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public ClaimsPrincipal CreateClaimsPrincipal(Usuario usuario)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol.Nombre),
                new Claim("Identificacion", usuario.Identificacion)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            return new ClaimsPrincipal(identity);
        }
    }
}
