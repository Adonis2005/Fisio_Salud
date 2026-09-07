using Microsoft.AspNetCore.Mvc;

namespace FisioSalud_Proyecto.Controllers.Nosotros
{
    public class NosotrosController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Home/NosotrosIndex.cshtml");
        }
    }
}
