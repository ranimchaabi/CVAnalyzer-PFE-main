using Administration.Data;
using Administration.Models;
using Administration.Services;
using Administration.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Administration.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IDataProtector _protector;

        public AccountController(ApplicationDbContext context, IEmailSender emailSender, IDataProtectionProvider provider)
        {
            _context = context;
            _emailSender = emailSender;
            _protector = provider.CreateProtector("AdministrationPasswordReset");
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email) || !(new EmailAddressAttribute().IsValid(email)))
            {
                TempData["Error"] = "Veuillez saisir un email valide.";
                return View();
            }

            var user = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email == email &&
                                          (u.Role == "Admin" || u.Role == "RH" || u.Role == "Directeur"), ct);

            if (user == null)
            {
                TempData["Error"] = "Aucun compte Administration (Admin/RH/Directeur) trouvé avec cet email.";
                return View();
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(20).Ticks;
            var token = _protector.Protect($"{user.Id}|{expiresAt}");
            var resetUrl = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);
            if (string.IsNullOrWhiteSpace(resetUrl))
            {
                TempData["Error"] = "Impossible de générer le lien de réinitialisation.";
                return View();
            }

            var sent = await _emailSender.SendPasswordResetAsync(user.Email, resetUrl, ct);
            if (!sent)
            {
                TempData["Error"] = "Erreur lors de l'envoi de l'email. Vérifiez la configuration SMTP.";
                return View();
            }

            TempData["Success"] = "Un lien de réinitialisation a été envoyé à votre adresse email.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(Login));
            }
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["Error"] = "Données invalides.";
                ViewBag.Token = token;
                return View();
            }

            if (!IsPasswordValid(newPassword))
            {
                TempData["Error"] = "Le mot de passe doit contenir au moins 8 caractères, une lettre et un chiffre.";
                ViewBag.Token = token;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Les mots de passe ne correspondent pas.";
                ViewBag.Token = token;
                return View();
            }

            try
            {
                var raw = _protector.Unprotect(token);
                var parts = raw.Split('|');
                if (parts.Length != 2)
                {
                    TempData["Error"] = "Lien invalide.";
                    ViewBag.Token = token;
                    return View();
                }

                var userId = int.Parse(parts[0]);
                var expiresTicks = long.Parse(parts[1]);
                if (DateTime.UtcNow > new DateTime(expiresTicks, DateTimeKind.Utc))
                {
                    TempData["Error"] = "Lien expiré.";
                    ViewBag.Token = token;
                    return View();
                }

                var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == userId, ct);
                if (user == null || !(user.Role == "Admin" || user.Role == "RH" || user.Role == "Directeur"))
                {
                    TempData["Error"] = "Utilisateur introuvable.";
                    ViewBag.Token = token;
                    return View();
                }

                user.MotPasse = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _context.SaveChangesAsync(ct);
                TempData["Success"] = "Mot de passe mis à jour.";
                return RedirectToAction(nameof(Login));
            }
            catch
            {
                TempData["Error"] = "Lien invalide ou corrompu.";
                ViewBag.Token = token;
                return View();
            }
        }

        private static bool IsPasswordValid(string password)
        {
            if (password.Length < 8)
            {
                return false;
            }

            var hasLetter = Regex.IsMatch(password, "[A-Za-z]");
            var hasDigit = Regex.IsMatch(password, "[0-9]");
            return hasLetter && hasDigit;
        }
    }
}