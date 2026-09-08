using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Controllers
{
    [Authorize(Roles = Roles.Administrador + "," + Roles.Cliente)]
    public class CobrosController : Controller
    {
        private readonly FisioSaludDbContext db;
        private readonly IAdminPanelService admin;
        public CobrosController(FisioSaludDbContext db, IAdminPanelService admin) { this.db = db; this.admin = admin; }
        public async Task<IActionResult> Index()
        {
            var query = db.Pagos.AsNoTracking().Include(p => p.Factura).ThenInclude(f => f.Paciente).AsQueryable();
            if (!User.IsInRole(Roles.Administrador)) { var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); query = query.Where(p => p.Factura.Paciente.UsuarioId == id); }
            else ViewBag.Chrome = await admin.GetChromeAsync(User.Identity.Name);
            return View(await query.OrderByDescending(p => p.FechaRegistro).ToListAsync());
        }
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Verificar(int pagoId, bool aprobar)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var pago = await db.Pagos.Include(p => p.Factura).FirstOrDefaultAsync(p => p.PagoId == pagoId);
            if (pago == null || pago.Estado != PagoEstados.Pendiente) { TempData["Error"] = "El comprobante ya fue procesado o no existe."; return RedirectToAction(nameof(Index)); }
            if (!aprobar || pago.Factura.Estado != PagoEstados.Pendiente)
            {
                pago.Estado = PagoEstados.Cancelado;
                await db.SaveChangesAsync(); await transaction.CommitAsync();
                TempData["Success"] = "Comprobante rechazado. Si la factura sigue pendiente, el cliente puede reportarlo nuevamente.";
            }
            else
            {
                // Un único guardado confirma comprobante, factura y citas vinculadas.
                pago.Estado = PagoEstados.Pagado; pago.FechaPago = System.DateTime.Now; pago.Factura.Estado = PagoEstados.Pagado;
                var ids = db.DetallesFactura.Where(d => d.FacturaId == pago.FacturaId && d.CitaId.HasValue).Select(d => d.CitaId.Value);
                var citas = await db.Citas.Where(c => ids.Contains(c.CitaId) && (c.Estado == CitaEstados.Solicitada || c.Estado == CitaEstados.PendientePago)).ToListAsync();
                foreach (var cita in citas) { cita.Estado = CitaEstados.Confirmada; cita.FechaActualizacion = System.DateTime.Now; }
                await db.SaveChangesAsync(); await transaction.CommitAsync(); TempData["Success"] = "Pago validado; factura pagada y cita confirmada.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
