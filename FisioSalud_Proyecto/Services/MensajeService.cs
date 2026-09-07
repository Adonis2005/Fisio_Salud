using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Services
{
    public interface IMensajeService
    {
        Task<IReadOnlyList<MensajeResumen>> ListarConversacionesAsync(int usuarioId);
        Task<IReadOnlyList<MensajeDto>> ObtenerMensajesAsync(int usuarioId, int otroUsuarioId);
        Task<(bool Success, string Error)> EnviarAsync(int remitenteId, int destinatarioId, string contenido);
        Task<bool> MarcarLeidosAsync(int usuarioId, int otroUsuarioId);
        Task<int> ContarNoLeidosAsync(int usuarioId);
        Task<bool> PuedeConversarAsync(int usuarioId, int otroUsuarioId);
    }

    public sealed class MensajeDto
    {
        public int MensajeId { get; set; }
        public int RemitenteId { get; set; }
        public int DestinatarioId { get; set; }
        public string RemitenteNombre { get; set; }
        public string Contenido { get; set; }
        public DateTime FechaEnvio { get; set; }
        public bool Leido { get; set; }
    }

    public sealed class MensajeResumen
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string UltimoMensaje { get; set; }
        public DateTime FechaUltimoMensaje { get; set; }
        public int NoLeidos { get; set; }
    }

    public class MensajeService : IMensajeService
    {
        private readonly FisioSaludDbContext _context;

        public MensajeService(FisioSaludDbContext context)
        {
            _context = context;
        }

        public async Task<bool> PuedeConversarAsync(int usuarioId, int otroUsuarioId)
        {
            if (usuarioId == otroUsuarioId) return false;

            var usuarios = await _context.Usuarios.AsNoTracking()
                .Where(u => u.UsuarioId == usuarioId || u.UsuarioId == otroUsuarioId)
                .Select(u => new { u.UsuarioId, Rol = u.Rol.Nombre, u.Estado })
                .ToListAsync();

            if (usuarios.Count != 2 || !usuarios.All(u => u.Estado) ||
                !((usuarios[0].Rol == Roles.Cliente && usuarios[1].Rol == Roles.Fisioterapeuta) ||
                  (usuarios[1].Rol == Roles.Cliente && usuarios[0].Rol == Roles.Fisioterapeuta)))
                return false;

            return await _context.Citas.AsNoTracking().AnyAsync(c =>
                c.Estado != "CANCELADA" &&
                ((c.Paciente.UsuarioId == usuarioId && c.FisioterapeutaId == otroUsuarioId) ||
                 (c.Paciente.UsuarioId == otroUsuarioId && c.FisioterapeutaId == usuarioId)));
        }

        public async Task<IReadOnlyList<MensajeResumen>> ListarConversacionesAsync(int usuarioId)
        {
            var mensajes = await _context.Mensajes.AsNoTracking()
                .Include(m => m.Remitente).Include(m => m.Destinatario)
                .Where(m => m.RemitenteId == usuarioId || m.DestinatarioId == usuarioId)
                .OrderByDescending(m => m.FechaEnvio).ToListAsync();

            var result = new List<MensajeResumen>();
            foreach (var grupo in mensajes.GroupBy(m => m.RemitenteId == usuarioId ? m.DestinatarioId : m.RemitenteId))
            {
                if (!await PuedeConversarAsync(usuarioId, grupo.Key)) continue;
                var ultimo = grupo.First();
                var otro = ultimo.RemitenteId == usuarioId ? ultimo.Destinatario : ultimo.Remitente;
                result.Add(new MensajeResumen
                {
                    UsuarioId = grupo.Key,
                    Nombre = otro.NombreCompleto,
                    Iniciales = Iniciales(otro.Nombres, otro.Apellidos),
                    UltimoMensaje = ultimo.Contenido,
                    FechaUltimoMensaje = ultimo.FechaEnvio,
                    NoLeidos = grupo.Count(m => m.DestinatarioId == usuarioId && !m.Leido)
                });
            }
            return result.OrderByDescending(x => x.FechaUltimoMensaje).ToList();
        }

        public async Task<IReadOnlyList<MensajeDto>> ObtenerMensajesAsync(int usuarioId, int otroUsuarioId)
        {
            if (!await PuedeConversarAsync(usuarioId, otroUsuarioId)) return Array.Empty<MensajeDto>();
            return await _context.Mensajes.AsNoTracking().Include(m => m.Remitente)
                .Where(m => (m.RemitenteId == usuarioId && m.DestinatarioId == otroUsuarioId) ||
                            (m.RemitenteId == otroUsuarioId && m.DestinatarioId == usuarioId))
                .OrderBy(m => m.FechaEnvio)
                .Select(m => new MensajeDto { MensajeId = m.MensajeId, RemitenteId = m.RemitenteId, DestinatarioId = m.DestinatarioId, RemitenteNombre = m.Remitente.NombreCompleto, Contenido = m.Contenido, FechaEnvio = m.FechaEnvio, Leido = m.Leido })
                .ToListAsync();
        }

        public async Task<(bool Success, string Error)> EnviarAsync(int remitenteId, int destinatarioId, string contenido)
        {
            contenido = contenido?.Trim();
            if (string.IsNullOrWhiteSpace(contenido)) return (false, "El mensaje no puede estar vacío.");
            if (contenido.Length > 2000) return (false, "El mensaje no puede superar 2000 caracteres.");
            if (!await PuedeConversarAsync(remitenteId, destinatarioId)) return (false, "No tienes autorización para esta conversación.");
            _context.Mensajes.Add(new Mensaje { RemitenteId = remitenteId, DestinatarioId = destinatarioId, Contenido = contenido, FechaEnvio = DateTime.Now, Leido = false });
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> MarcarLeidosAsync(int usuarioId, int otroUsuarioId)
        {
            if (!await PuedeConversarAsync(usuarioId, otroUsuarioId)) return false;
            var mensajes = await _context.Mensajes.Where(m => m.RemitenteId == otroUsuarioId && m.DestinatarioId == usuarioId && !m.Leido).ToListAsync();
            foreach (var mensaje in mensajes) mensaje.Leido = true;
            if (mensajes.Count > 0) await _context.SaveChangesAsync();
            return true;
        }

        public Task<int> ContarNoLeidosAsync(int usuarioId) => _context.Mensajes.AsNoTracking().CountAsync(m => m.DestinatarioId == usuarioId && !m.Leido);

        private static string Iniciales(string nombres, string apellidos) => $"{(nombres ?? "").FirstOrDefault()}{(apellidos ?? "").FirstOrDefault()}".ToUpperInvariant();
    }
}
