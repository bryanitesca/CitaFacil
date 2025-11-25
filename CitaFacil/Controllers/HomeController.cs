using CitaFacil.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
// ¡ASEGÚRATE DE TENER ESTOS DOS USINGS!
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CitaFacil.Constants;

namespace CitaFacil.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // --- MODIFICA ESTA ACCIÓN ---
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var rol = User.FindFirstValue(ClaimTypes.Role);

                return rol switch
                {
                    Roles.Administrador => RedirectToAction("Index", "Admin"),
                    Roles.Doctor => RedirectToAction("Index", "Doctor"),
                    Roles.Paciente => RedirectToAction("Dashboard", "Pacientes"),
                    _ => RedirectToAction("Index", "Citas")
                };
            }

            return View();
        }

        // Esta vista de Privacy usar� el layout que le toque
        // (si no est�s logueado, usar� _LandingLayout)
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Esta es la vista opcional que te suger� para "Acceso Denegado"
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
