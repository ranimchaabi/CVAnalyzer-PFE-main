using Microsoft.AspNetCore.Mvc;

namespace Administration.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "RH" => RedirectToAction("Dashboard", "RH"),
                "Directeur" => RedirectToAction("Dashboard", "DirecteurDepartement"),
                _ => RedirectToAction("Login", "Account")
            };
        }
    }
}