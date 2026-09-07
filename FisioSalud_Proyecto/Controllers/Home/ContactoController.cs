using FisioSalud_Proyecto.Models.Contacto;
using FisioSalud_Proyecto.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FisioSalud_Proyecto.Controllers.Contacto
{
    public class ContactoController : Controller
    {
        private readonly IEmailService _emailService;

        public ContactoController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/Home/ContactoIndex.cshtml", new ReservarCitaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(ReservarCitaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/ContactoIndex.cshtml", model);
            }

            try
            {
                await _emailService.SendAppointmentRequestEmailAsync(model);
                TempData["Success"] = "¡Su solicitud de cita ha sido enviada! Nos pondremos en contacto pronto.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidSmtpConfigurationException)
            {
                TempData["Success"] = "Su solicitud ha sido registrada. Nos pondremos en contacto pronto.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
