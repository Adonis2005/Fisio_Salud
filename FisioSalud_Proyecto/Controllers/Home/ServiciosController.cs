using Microsoft.AspNetCore.Mvc;

namespace FisioSalud_Proyecto.Controllers.Servicios
{
    public class ServiciosController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Home/ServiciosIndex.cshtml");
        }
    }
}
