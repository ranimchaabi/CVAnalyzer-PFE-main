using Microsoft.AspNetCore.Mvc;
using CvParsing.Data;
using CvParsing.Services;
using CvParsing.Models;

namespace CvParsing.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;

    public AccountController(AppDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            ViewBag.PasswordError = "Veuillez entrer le mot de passe";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var user = _context.Utilisateurs
            .FirstOrDefault(u => u.NomUtilisateur == username || u.Email == username);

        if (user != null && _passwordService.Verify(user.MotPasse, password))
        {
            if (!user.IsActive)
            {
                ViewBag.Error = "Compte désactivé.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            user.DateDerniereConnexion = DateTime.Now;
            _context.SaveChanges();

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.NomUtilisateur);
            HttpContext.Session.SetString("UserEmail", user.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Nom d'utilisateur ou mot de passe incorrect.";
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    public IActionResult Register(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public IActionResult Register(string username, string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Veuillez remplir tous les champs.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        if (_context.Utilisateurs.Any(u => u.Email == email))
        {
            ViewBag.Error = "Cet email est déjà utilisé.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var newUser = new Utilisateur
        {
            NomUtilisateur = username,
            Email = email,
            MotPasse = _passwordService.Hash(password),
            DateCreation = DateTime.Now,
            Role = "Candidat",
            IsActive = true
        };
        _context.Utilisateurs.Add(newUser);
        _context.SaveChanges();

        var adminIds = _context.Utilisateurs.Where(u => u.Role == "Admin").Select(u => u.Id).ToList();
        foreach (var adminId in adminIds)
        {
            _context.Notifications.Add(new Notification
            {
                RecipientUserId = adminId,
                Title = "Nouveau candidat",
                Message = $"Nouveau candidat inscrit: {newUser.NomUtilisateur}",
                Type = "CandidateRegistration",
                LinkUrl = $"/Admin/UserDetails/{newUser.Id}",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
        }
        _context.SaveChanges();

        HttpContext.Session.SetString("UserId", newUser.Id.ToString());
        HttpContext.Session.SetString("UserName", username);
        HttpContext.Session.SetString("UserEmail", email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet("Account/ForgotPassword")]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");
        ViewBag.Token = token;
        return View("~/Views/Account/ResetPassword.cshtml");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}