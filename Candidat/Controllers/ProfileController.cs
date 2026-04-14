using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvParsing.Data;
using CvParsing.Models;
using CvParsing.Models.ViewModels;
using CvParsing.Services;

namespace CvParsing.Controllers;

public class ProfileController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly PasswordService _passwordService;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedStatuses = { "Accepted", "Rejected", "Pending" };

    public ProfileController(AppDbContext context, IWebHostEnvironment env, PasswordService passwordService)
    {
        _context = context;
        _env = env;
        _passwordService = passwordService;
    }

    public async Task<IActionResult> Index(int page = 1, string? status = null, string? tab = null)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Profile") });

        var utilisateur = await _context.Utilisateurs.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (utilisateur == null)
            return RedirectToAction("Logout", "Account");

        var deptOptions = await _context.OffresEmploi
            .AsNoTracking()
            .Where(o => o.Departement != null && o.Departement != "")
            .Select(o => o.Departement!)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        if (deptOptions.Count == 0)
            deptOptions = new List<string> { "Design", "Développement", "RH", "Marketing", "Commercial" };

        const int pageSize = 8;
        page = page < 1 ? 1 : page;

        var statusNorm = NormalizeStatusFilter(status);
        var appsQuery = _context.Cvs
            .AsNoTracking()
            .Include(c => c.Offre)
            .Where(c => c.UtilisateurId == userId);

        var totalApps = await appsQuery.CountAsync();
        var cvs = await appsQuery
            .OrderByDescending(c => c.UploadDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var applications = cvs.Select(c => new ApplicationRowViewModel
        {
            CvId = c.Id,
            OffreId = c.OffreId,
            TitrePoste = c.Offre?.Titre ?? "—",
            DepartementOuEntreprise = c.Offre?.Departement ?? "—",
            DateCandidature = c.UploadDate,
            Statut = "Pending"
        }).ToList();

        var vm = new ProfilePageViewModel
        {
            NomComplet = utilisateur.NomUtilisateur ?? "",
            Email = utilisateur.Email ?? "",
            DepartementOptions = deptOptions,
            DesignationOptions = DefaultDesignations,
            Applications = applications,
            ApplicationsTotal = totalApps,
            ApplicationsPage = page,
            ApplicationsPageSize = pageSize,
            StatusFilter = statusNorm
        };

        ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "profile" : tab.Trim().ToLowerInvariant();
        ViewData["Title"] = "Mon profil";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationsPartial(int page = 1, string? status = null, string? tab = null)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        const int pageSize = 8;
        page = page < 1 ? 1 : page;

        var statusNorm = NormalizeStatusFilter(status);

        var appsQuery = _context.Cvs
            .AsNoTracking()
            .Include(c => c.Offre)
            .Where(c => c.UtilisateurId == userId);

        var totalApps = await appsQuery.CountAsync();
        var cvs = await appsQuery
            .OrderByDescending(c => c.UploadDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var applications = cvs.Select(c => new ApplicationRowViewModel
        {
            CvId = c.Id,
            OffreId = c.OffreId,
            TitrePoste = c.Offre?.Titre ?? "—",
            DepartementOuEntreprise = c.Offre?.Departement ?? "—",
            DateCandidature = c.UploadDate,
            Statut = "Pending"
        }).ToList();

        var vm = new ProfilePageViewModel
        {
            Applications = applications,
            ApplicationsTotal = totalApps,
            ApplicationsPage = page,
            ApplicationsPageSize = pageSize,
            StatusFilter = statusNorm
        };

        ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "profile" : tab.Trim().ToLowerInvariant();
        return PartialView("~/Views/Profile/_ApplicationsTable.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        string nomComplet,
        string email,
        string? telephone,
        string? departement,
        string? designation,
        string? langues,
        string? bio,
        IFormFile? photo)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Profile") });

        var utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == userId);
        if (utilisateur == null)
            return RedirectToAction("Logout", "Account");

        if (string.IsNullOrWhiteSpace(nomComplet) || string.IsNullOrWhiteSpace(email))
        {
            TempData["ProfileError"] = "Le nom complet et l'email sont obligatoires.";
            return RedirectToAction(nameof(Index), new { tab = "profile" });
        }

        var emailTaken = await _context.Utilisateurs.AnyAsync(u => u.Email == email && u.Id != userId);
        if (emailTaken)
        {
            TempData["ProfileError"] = "Cet email est déjà utilisé par un autre compte.";
            return RedirectToAction(nameof(Index), new { tab = "profile" });
        }

        utilisateur.NomUtilisateur = nomComplet.Trim();
        utilisateur.Email = email.Trim();
        HttpContext.Session.SetString("UserName", utilisateur.NomUtilisateur);
        HttpContext.Session.SetString("UserEmail", utilisateur.Email);

        // Pas de table Candidat dans la BD, donc certains champs ne peuvent pas être sauvegardés ici
        // sans modifier la base de données..

        await _context.SaveChangesAsync();
        TempData["ProfileSuccess"] = "Profil enregistré (Seuls le nom et l'email sont persistants dans la DB).";
        return RedirectToAction(nameof(Index), new { tab = "profile" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string motDePasseActuel, string nouveauMotDePasse, string confirmation)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return RedirectToAction("Login", "Account");

        var utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == userId);
        if (utilisateur == null)
            return RedirectToAction("Logout", "Account");

        if (string.IsNullOrEmpty(nouveauMotDePasse) || nouveauMotDePasse != confirmation)
        {
            TempData["PasswordError"] = "Le nouveau mot de passe et la confirmation ne correspondent pas.";
            return RedirectToAction(nameof(Index), new { tab = "password" });
        }

        if (nouveauMotDePasse.Length < 6)
        {
            TempData["PasswordError"] = "Le mot de passe doit contenir au moins 6 caractères.";
            return RedirectToAction(nameof(Index), new { tab = "password" });
        }

        if (!_passwordService.Verify(utilisateur.MotPasse, motDePasseActuel))
        {
            TempData["PasswordError"] = "Mot de passe actuel incorrect.";
            return RedirectToAction(nameof(Index), new { tab = "password" });
        }

        utilisateur.MotPasse = _passwordService.Hash(nouveauMotDePasse);
        await _context.SaveChangesAsync();
        TempData["PasswordSuccess"] = "Mot de passe mis à jour.";
        return RedirectToAction(nameof(Index), new { tab = "password" });
    }

    private static string? NormalizeStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase))
            return null;
        return AllowedStatuses.Contains(status) ? status : null;
    }

    private static readonly List<string> DefaultDesignations = new List<string>
    {
        "UI UX Designer", "Développeur Web", "Développeur Full Stack", "Chef de projet", "Data Analyst", "RH", "Autre"
    };
}
