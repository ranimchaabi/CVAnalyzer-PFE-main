using Administration.Data;
using Administration.Models;
using Administration.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Administration.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= REGISTER =================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_context.Utilisateurs.Any(u => u.NomUtilisateur == model.NomUtilisateur))
            {
                ModelState.AddModelError("", "Nom utilisateur existe déjà");
                return View(model);
            }

            var user = new Utilisateur
            {
                NomUtilisateur = model.NomUtilisateur,
                Email = model.Email,
                MotPasse = BCrypt.Net.BCrypt.HashPassword(model.MotPasse),
                Role = model.Role,
                IsActive = true,
                DateCreation = DateTime.Now
            };

            _context.Utilisateurs.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // ================= LOGIN =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Utilisateurs
                .FirstOrDefault(u => u.NomUtilisateur == model.NomUtilisateur);

            if (user == null)
            {
                ModelState.AddModelError("", "Utilisateur introuvable");
                return View(model);
            }

            if (!BCrypt.Net.BCrypt.Verify(model.MotPasse, user.MotPasse))
            {
                ModelState.AddModelError("", "Mot de passe incorrect");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Compte désactivé");
                return View(model);
            }

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("Username", user.NomUtilisateur);
            HttpContext.Session.SetString("UserProfileImage", user.PhotoUrl ?? "");

            user.DateDerniereConnexion = DateTime.Now;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home");
        }

        // ================= LOGOUT =================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}