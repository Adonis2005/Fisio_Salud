using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Cliente;
using FisioSalud_Proyecto.Models.Entities;
using FisioSalud_Proyecto.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Controllers
{
    public class AjustesViewModel : ClientePageViewModel
    {
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Especialidad { get; set; }
        public string Consultorio { get; set; }
        public string ClinicaNombre { get; set; } = "FisioSalud";
        public string ClinicaCorreo { get; set; }
        public string ClinicaTelefono { get; set; }
        public string ClinicaDireccion { get; set; }
    }
    [Authorize]
    public class AjustesController : Controller
    {
        private readonly FisioSaludDbContext db;
        private readonly IAuthService auth;
        private readonly IAdminPanelService admin;
        public AjustesController(FisioSaludDbContext db, IAuthService auth, IAdminPanelService admin) { this.db=db; this.auth=auth; this.admin=admin; }
        private int Id => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        private async Task<string> Leer(string clave) => await db.AjustesSistema.Where(a => a.Clave == clave).Select(a => a.Valor).FirstOrDefaultAsync();
        private async Task Guardar(string clave, string valor) { var a = await db.AjustesSistema.FindAsync(clave); if(a == null) db.AjustesSistema.Add(new Ajuste { Clave=clave, Valor=valor ?? "" }); else a.Valor=valor ?? ""; }
        public async Task<IActionResult> Index()
        {
            var u=await db.Usuarios.FindAsync(Id);
            var m=new AjustesViewModel { Nombres=u.Nombres, Apellidos=u.Apellidos, NombreCompleto=u.NombreCompleto, Correo=u.Correo, Telefono=u.Telefono, Activo=u.Estado, Iniciales=string.Concat(u.Nombres.Take(1))+string.Concat(u.Apellidos.Take(1)), FechaHoyFormato=DateTime.Today.ToString("dd/MM/yyyy") };
            var p=await db.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == Id); m.TieneExpediente=p!=null; m.PacienteId=p?.PacienteId ?? 0;
            m.Especialidad=await Leer($"usuario:{Id}:especialidad"); m.Consultorio=await Leer($"usuario:{Id}:consultorio");
            m.ClinicaNombre=await Leer("clinica:nombre") ?? "FisioSalud"; m.ClinicaCorreo=await Leer("clinica:correo"); m.ClinicaTelefono=await Leer("clinica:telefono"); m.ClinicaDireccion=await Leer("clinica:direccion");
            if(User.IsInRole(Roles.Administrador)) ViewBag.Chrome=await admin.GetChromeAsync(u.NombreCompleto);
            return View(m);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(string nombres, string apellidos, string correo, string telefono, string especialidad, string consultorio)
        {
            if(string.IsNullOrWhiteSpace(nombres) || nombres.Length>80 || string.IsNullOrWhiteSpace(apellidos) || apellidos.Length>80 || string.IsNullOrWhiteSpace(correo) || correo.Length>120 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(correo) || telefono?.Length>20 || especialidad?.Length>150 || consultorio?.Length>200)
            { TempData["Error"]="Revisa nombre, correo y teléfono."; return RedirectToAction(nameof(Index)); }
            correo=correo.Trim().ToLowerInvariant();
            if(await db.Usuarios.AnyAsync(u => u.Correo==correo && u.UsuarioId!=Id)) { TempData["Error"]="Ese correo pertenece a otra cuenta."; return RedirectToAction(nameof(Index)); }
            var u=await db.Usuarios.Include(u=>u.Rol).FirstAsync(u=>u.UsuarioId==Id);
            u.Nombres=nombres.Trim(); u.Apellidos=apellidos.Trim(); u.Correo=correo; u.Telefono=telefono?.Trim();
            var p=await db.Pacientes.FirstOrDefaultAsync(p=>p.UsuarioId==Id);
            if(p!=null){p.Nombres=u.Nombres;p.Apellidos=u.Apellidos;p.Correo=u.Correo;p.Telefono=u.Telefono;}
            if(User.IsInRole(Roles.Fisioterapeuta)){await Guardar($"usuario:{Id}:especialidad",especialidad);await Guardar($"usuario:{Id}:consultorio",consultorio);}
            await db.SaveChangesAsync();
            await HttpContext.SignInAsync("CookieAuth",auth.CreateClaimsPrincipal(u));
            TempData["Success"]="Perfil actualizado."; return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Password(string actual,string nueva,string confirmacion)
        {
            if(nueva!=confirmacion || string.IsNullOrEmpty(nueva) || nueva.Length<8 || nueva.Length>100 || !nueva.Any(char.IsUpper) || !nueva.Any(char.IsLower) || !nueva.Any(char.IsDigit))
            {TempData["Error"]="La contraseña debe tener 8 a 100 caracteres, mayúsculas, minúsculas y números; la confirmación debe coincidir.";return RedirectToAction(nameof(Index));}
            var r=await auth.ChangePasswordAsync(Id,actual ?? "",nueva);TempData[r.Success?"Success":"Error"]=r.Success?"Contraseña actualizada.":r.Error;return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles=Roles.Administrador)]
        public async Task<IActionResult> Clinica(string nombre,string correo,string telefono,string direccion)
        {
            if(string.IsNullOrWhiteSpace(nombre) || nombre.Length>150 || correo?.Length>120 || telefono?.Length>30 || direccion?.Length>250) {TempData["Error"]="Revisa los datos de la clínica.";return RedirectToAction(nameof(Index));}
            await Guardar("clinica:nombre",nombre.Trim());await Guardar("clinica:correo",correo);await Guardar("clinica:telefono",telefono);await Guardar("clinica:direccion",direccion);await db.SaveChangesAsync();TempData["Success"]="Datos de la clínica guardados.";return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> DescargarMisDatos()
        {
            var usuario=await db.Usuarios.Where(u=>u.UsuarioId==Id).Select(u=>new {u.Nombres,u.Apellidos,u.Correo,u.Identificacion,u.Telefono}).FirstAsync();
            var paciente=await db.Pacientes.AsNoTracking().Where(p=>p.UsuarioId==Id).Select(p=>new {p.PacienteId,p.FechaNacimiento,p.Sexo,p.Direccion}).FirstOrDefaultAsync();
            var pacienteId=paciente?.PacienteId ?? 0;
            var citas=await db.Citas.Where(c=>c.PacienteId==pacienteId).Select(c=>new {c.Fecha,c.HoraInicio,c.Estado,c.MotivoConsulta}).ToListAsync();
            var facturas=await db.Facturas.Where(f=>f.PacienteId==pacienteId).Select(f=>new {f.NumeroFactura,f.Fecha,f.Monto,f.Estado}).ToListAsync();
            return File(JsonSerializer.SerializeToUtf8Bytes(new {usuario,paciente,citas,facturas},new JsonSerializerOptions{WriteIndented=true}),"application/json","mis-datos-fisiosalud.json");
        }
    }
}
