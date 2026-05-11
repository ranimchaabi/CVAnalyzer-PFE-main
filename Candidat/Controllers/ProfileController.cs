using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvParsing.Data;
using CvParsing.Helpers;
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
    private const long MaxImageFileSize = 2 * 1024 * 1024; // 2MB

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
<<<<<<< HEAD

=======
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        var appsQuery = _context.Cvs
            .AsNoTracking()
            .Include(c => c.Offre)
            .Include(c => c.Matches)
            .Where(c => c.UtilisateurId == userId);

<<<<<<< HEAD
        // ← Appliquer le filtre statut
        if (statusNorm != null)
        {
            if (statusNorm == "Pending")
            {
                appsQuery = appsQuery.Where(c => c.ValidationStatus == "Pending" || c.ValidationStatus == "DirectorApproved" || c.ValidationStatus == "InterviewScheduled");
            }
            else
            {
                appsQuery = appsQuery.Where(c => c.ValidationStatus == statusNorm);
            }
        }

=======
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        var totalApps = await appsQuery.CountAsync();
        var cvs = await appsQuery
            .OrderByDescending(c => c.UploadDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var applications = cvs.Select(c => {
            var match = c.Matches.FirstOrDefault(m => m.OffreId == c.OffreId);
            return new ApplicationRowViewModel
            {
                CvId = c.Id,
                OffreId = c.OffreId,
                TitrePoste = c.Offre?.Titre ?? "—",
                DepartementOuEntreprise = c.Offre?.Departement ?? "—",
                DateCandidature = c.UploadDate,
<<<<<<< HEAD
                Statut = c.ValidationStatus ?? "Pending", // ← vrai statut
=======
                Statut = "Pending",
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
                GlobalScore = match?.GlobalScore ?? 0,
                CompetenceScore = match?.CompetenceScore ?? 0,
                DiplomeScore = match?.DiplomeScore ?? 0,
                ExperienceScore = match?.ExperienceScore ?? 0
            };
        }).ToList();

        var vm = new ProfilePageViewModel
        {
            NomComplet = utilisateur.NomUtilisateur ?? "",
            Email = utilisateur.Email ?? "",
            PhotoUrl = utilisateur.PhotoUrl,
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
            .Include(c => c.Matches)
            .Where(c => c.UtilisateurId == userId);

<<<<<<< HEAD
        // ← Appliquer le filtre statut
        if (statusNorm != null)
        {
            if (statusNorm == "Pending")
            {
                appsQuery = appsQuery.Where(c => c.ValidationStatus == "Pending" || c.ValidationStatus == "DirectorApproved" || c.ValidationStatus == "InterviewScheduled");
            }
            else
            {
                appsQuery = appsQuery.Where(c => c.ValidationStatus == statusNorm);
            }
        }

=======
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        var totalApps = await appsQuery.CountAsync();
        var cvs = await appsQuery
            .OrderByDescending(c => c.UploadDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var applications = cvs.Select(c => {
            var match = c.Matches.FirstOrDefault(m => m.OffreId == c.OffreId);
            return new ApplicationRowViewModel
            {
                CvId = c.Id,
                OffreId = c.OffreId,
                TitrePoste = c.Offre?.Titre ?? "—",
                DepartementOuEntreprise = c.Offre?.Departement ?? "—",
                DateCandidature = c.UploadDate,
<<<<<<< HEAD
                Statut = c.ValidationStatus ?? "Pending", // ← vrai statut
=======
                Statut = "Pending",
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
                GlobalScore = match?.GlobalScore ?? 0,
                CompetenceScore = match?.CompetenceScore ?? 0,
                DiplomeScore = match?.DiplomeScore ?? 0,
                ExperienceScore = match?.ExperienceScore ?? 0
            };
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

    [HttpGet]
<<<<<<< HEAD
    [Route("Profile/offer-details")]
    public async Task<IActionResult> DetailPoste(int offreId)
    {
        var offre = await _context.OffresEmploi
            .FirstOrDefaultAsync(o => o.Id == offreId);

        if (offre == null)
            return NotFound();

        var userIdStr = HttpContext.Session.GetString("UserId");
        ViewBag.IsLoggedIn = !string.IsNullOrEmpty(userIdStr);

        if (int.TryParse(userIdStr, out var userId))
        {
            ViewBag.HasCvSubmitted = await _context.Cvs
                .AnyAsync(c => c.UtilisateurId == userId && c.OffreId == offreId);
        }

        ViewData["OffreId"] = offreId;

        return View("~/Views/Offre/offer-details.cshtml", offre);
=======
    public async Task<IActionResult> CvResult(int offreId, int cvId)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("CvResult", "Profile", new { offreId, cvId }) });

        var match = await _context.Matches
            .Include(m => m.Cv)
            .Include(m => m.Cv)
                .ThenInclude(c => c.Utilisateur)
            .Include(m => m.Offre)
            .FirstOrDefaultAsync(m => m.OffreId == offreId && m.CvId == cvId && m.Cv.UtilisateurId == userId);

        if (match == null)
            return NotFound();

        return View("~/Views/Profile/CvResult.cshtml", match);
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
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

<<<<<<< HEAD
=======
        // Handle photo upload
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        if (photo != null && photo.Length > 0)
        {
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
            {
                TempData["ProfileError"] = "Format d'image non valide. Utilisez JPG, JPEG ou PNG.";
                return RedirectToAction(nameof(Index), new { tab = "profile" });
            }

            if (photo.Length > MaxImageFileSize)
            {
                TempData["ProfileError"] = "La taille de l'image ne doit pas dépasser 2 MB.";
                return RedirectToAction(nameof(Index), new { tab = "profile" });
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

<<<<<<< HEAD
=======
            // Delete old photo if exists
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
            if (!string.IsNullOrEmpty(utilisateur.PhotoUrl))
            {
                var oldPath = Path.Combine(_env.WebRootPath, utilisateur.PhotoUrl.TrimStart('~').TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
                }
            }

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            utilisateur.PhotoUrl = $"/uploads/profiles/{uniqueFileName}";
        }

        await _context.SaveChangesAsync();
        TempData["ProfileSuccess"] = "Profil enregistré avec succès.";
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

        if (!PasswordRules.TryValidate(nouveauMotDePasse, out var pwdErr))
        {
            TempData["PasswordError"] = pwdErr ?? "Mot de passe invalide.";
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
<<<<<<< HEAD
}
=======
}
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
