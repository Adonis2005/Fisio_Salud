using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Models.Administrador;
using FisioSalud_Proyecto.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Services
{
    public interface IUsuarioService
    {
        Task<AdminDashboardViewModel> GetDashboardAsync();
        Task<UsuarioFilterViewModel> GetUsuariosAsync(string busqueda, int? rolId, bool? estado);
        Task<UsuarioFormViewModel> GetUsuarioFormAsync(int? id);
        Task<(bool Success, string Error)> CreateAsync(UsuarioFormViewModel model);
        Task<(bool Success, string Error)> UpdateAsync(UsuarioFormViewModel model);
        Task<(bool Success, string Error)> ToggleEstadoAsync(int id);
        Task<(bool Success, string Error, bool SoftDelete)> DeleteAsync(int id);
        Task<List<RolSelectItem>> GetRolesAsync();
        Task<List<Usuario>> GetUsuariosPorRolAsync(string rolNombre);
    }

    public class UsuarioService : IUsuarioService
    {
        private readonly FisioSaludDbContext _context;
        private readonly IPasswordService _passwordService;

        public UsuarioService(FisioSaludDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync()
        {
            var usuarios = await _context.Usuarios.Include(u => u.Rol).ToListAsync();

            return new AdminDashboardViewModel
            {
                TotalUsuarios = usuarios.Count,
                UsuariosActivos = usuarios.Count(u => u.Estado),
                UsuariosBloqueados = usuarios.Count(u => !u.Estado),
                TotalClientes = usuarios.Count(u => u.Rol.Nombre == Helpers.Roles.Cliente),
                TotalFisioterapeutas = usuarios.Count(u => u.Rol.Nombre == Helpers.Roles.Fisioterapeuta),
                ActividadReciente = usuarios
                    .OrderByDescending(u => u.UltimoAcceso ?? u.FechaCreacion)
                    .Take(5)
                    .Select(MapToList)
                    .ToList()
            };
        }

        public async Task<UsuarioFilterViewModel> GetUsuariosAsync(string busqueda, int? rolId, bool? estado)
        {
            var query = _context.Usuarios.Include(u => u.Rol).AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLower();
                query = query.Where(u =>
                    u.Nombres.ToLower().Contains(term) ||
                    u.Apellidos.ToLower().Contains(term) ||
                    u.Correo.ToLower().Contains(term) ||
                    u.Identificacion.Contains(term));
            }

            if (rolId.HasValue)
                query = query.Where(u => u.RolId == rolId.Value);

            if (estado.HasValue)
                query = query.Where(u => u.Estado == estado.Value);

            var usuarios = (await query
                .OrderBy(u => u.Apellidos)
                .ThenBy(u => u.Nombres)
                .ToListAsync())
                .Select(MapToList)
                .ToList();

            return new UsuarioFilterViewModel
            {
                Busqueda = busqueda,
                RolId = rolId,
                Estado = estado,
                Usuarios = usuarios,
                RolesDisponibles = await GetRolesAsync()
            };
        }

        public async Task<UsuarioFormViewModel> GetUsuarioFormAsync(int? id)
        {
            var roles = await GetRolesAsync();

            if (!id.HasValue)
            {
                return new UsuarioFormViewModel
                {
                    Estado = true,
                    RolesDisponibles = roles
                };
            }

            var usuario = await _context.Usuarios.FindAsync(id.Value);
            if (usuario == null) return null;

            return new UsuarioFormViewModel
            {
                UsuarioId = usuario.UsuarioId,
                Nombres = usuario.Nombres,
                Apellidos = usuario.Apellidos,
                Identificacion = usuario.Identificacion,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono,
                RolId = usuario.RolId,
                Estado = usuario.Estado,
                RolesDisponibles = roles
            };
        }

        public async Task<(bool Success, string Error)> CreateAsync(UsuarioFormViewModel model)
        {
            var validation = await ValidateUniqueAsync(model.Correo, model.Identificacion, null);
            if (!validation.Success) return validation;

            if (string.IsNullOrWhiteSpace(model.Password))
                return (false, "La contraseña es obligatoria al crear un usuario.");

            var rolValido = await _context.Roles.AnyAsync(r => r.RolId == model.RolId && r.Estado);
            if (!rolValido)
                return (false, "El rol seleccionado no es válido.");

            var usuario = new Usuario
            {
                Nombres = model.Nombres.Trim(),
                Apellidos = model.Apellidos.Trim(),
                Identificacion = model.Identificacion.Trim(),
                Correo = model.Correo.Trim().ToLower(),
                Telefono = model.Telefono?.Trim(),
                RolId = model.RolId,
                Estado = model.Estado,
                PasswordHash = _passwordService.HashPassword(model.Password),
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> UpdateAsync(UsuarioFormViewModel model)
        {
            var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
            if (usuario == null)
                return (false, "Usuario no encontrado.");

            var validation = await ValidateUniqueAsync(model.Correo, model.Identificacion, model.UsuarioId);
            if (!validation.Success) return validation;

            var rolValido = await _context.Roles.AnyAsync(r => r.RolId == model.RolId && r.Estado);
            if (!rolValido)
                return (false, "El rol seleccionado no es válido.");

            usuario.Nombres = model.Nombres.Trim();
            usuario.Apellidos = model.Apellidos.Trim();
            usuario.Identificacion = model.Identificacion.Trim();
            usuario.Correo = model.Correo.Trim().ToLower();
            usuario.Telefono = model.Telefono?.Trim();
            usuario.RolId = model.RolId;
            usuario.Estado = model.Estado;
            usuario.FechaActualizacion = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(model.Password))
                usuario.PasswordHash = _passwordService.HashPassword(model.Password);

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error)> ToggleEstadoAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return (false, "Usuario no encontrado.");

            usuario.Estado = !usuario.Estado;
            usuario.FechaActualizacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string Error, bool SoftDelete)> DeleteAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return (false, "Usuario no encontrado.", false);

            var tieneRelaciones = await HasRelationsAsync(id);
            if (tieneRelaciones)
            {
                usuario.Estado = false;
                usuario.FechaActualizacion = DateTime.Now;
                await _context.SaveChangesAsync();
                return (true, "El usuario fue desactivado porque tiene registros relacionados en el sistema.", true);
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return (true, null, false);
        }

        public async Task<List<RolSelectItem>> GetRolesAsync()
        {
            return await _context.Roles
                .Where(r => r.Estado)
                .OrderBy(r => r.Nombre)
                .Select(r => new RolSelectItem { RolId = r.RolId, Nombre = r.Nombre })
                .ToListAsync();
        }

        private async Task<bool> HasRelationsAsync(int usuarioId)
        {
            var tieneCitas = await _context.Citas.AnyAsync(c => c.FisioterapeutaId == usuarioId);
            return tieneCitas;
        }

        private async Task<(bool Success, string Error)> ValidateUniqueAsync(string correo, string identificacion, int? excludeId)
        {
            var correoExiste = await _context.Usuarios
                .AnyAsync(u => u.Correo == correo.Trim().ToLower() && u.UsuarioId != excludeId);
            if (correoExiste)
                return (false, "El correo electrónico ya está registrado.");

            var idExiste = await _context.Usuarios
                .AnyAsync(u => u.Identificacion == identificacion.Trim() && u.UsuarioId != excludeId);
            if (idExiste)
                return (false, "La identificación ya está registrada.");

            return (true, null);
        }

        public async Task<List<Usuario>> GetUsuariosPorRolAsync(string rolNombre)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Estado && u.Rol.Nombre == rolNombre)
                .OrderBy(u => u.Nombres)
                .AsNoTracking()
                .ToListAsync();
        }

        private static UsuarioListViewModel MapToList(Usuario u)
        {
            return new UsuarioListViewModel
            {
                UsuarioId = u.UsuarioId,
                Nombres = u.Nombres,
                Apellidos = u.Apellidos,
                Identificacion = u.Identificacion,
                Correo = u.Correo,
                Telefono = u.Telefono,
                Rol = u.Rol?.Nombre,
                Estado = u.Estado,
                FechaCreacion = u.FechaCreacion,
                UltimoAcceso = u.UltimoAcceso
            };
        }
    }
}
