using Administration.Data;
using Administration.Filters;
using Administration.Helpers;
using Administration.Models;
using Administration.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Administration.Controllers
{
    [SessionAuthorize("RH")]
    public class RHController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RHController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= DASHBOARD =================
        public IActionResult Dashboard()
        {
            var stats = new DashboardStatsViewModel
            {
                TotalOffres  = _context.OffresEmploi.Count(),
                TotalCvs     = _context.Cvs.Count(),
                TotalMatches = _context.Matches.Count()
            };
            return View(stats);
        }

        // ================= LISTE DES POSTES =================
        public IActionResult Postes(string? search)
        {
            var query = _context.OffresEmploi.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.Titre.Contains(search) || o.Departement.Contains(search));

            ViewBag.Search = search;
            return View(query.ToList());
        }

        // ================= DÉTAIL D'UN POSTE =================
        // ✅ ACTION AJOUTÉE
        public async Task<IActionResult> DetailPoste(int id)
        {
            await MatchIntegrationHelper.EnsureMatchesForOffreAsync(_context, id);

            var offre = await _context.OffresEmploi
                .Include(o => o.Cvs)
                    .ThenInclude(c => c.Utilisateur)
                .Include(o => o.Cvs)
                    .ThenInclude(c => c.Matches)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offre == null)
            {
                return NotFound();
            }

            var candidats = offre.Cvs
                .Where(cv => cv.Utilisateur != null)
                .Select(cv => cv.Utilisateur)
                .DistinctBy(u => u.Id)
                .OrderByDescending(u => u.DateCreation)
                .ToList();

            var cvByUserId = offre.Cvs
                .GroupBy(cv => cv.UtilisateurId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(cv => cv.UploadDate).First().Id);

            var scoreByUserId = offre.Cvs
                .GroupBy(cv => cv.UtilisateurId)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(cv => cv.Matches).OrderByDescending(m => m.GlobalScore).FirstOrDefault()?.GlobalScore ?? 0f
                );

            var viewModel = new PosteDetailViewModel
            {
                Offre = offre,
                Candidats = candidats
            };

            ViewBag.CvByUserId = cvByUserId;
            ViewBag.ScoreByUserId = scoreByUserId;

            return View(viewModel);
        }

        // ================= CRÉER UN POSTE =================
        [HttpGet]
        public IActionResult CreatePoste()
        {
            // Get departments from database
            ViewBag.Departements = _context.Departements
                .Where(d => d.IsActive)
                .OrderBy(d => d.Nom)
                .Select(d => d.Nom)
                .ToList();
            return View(new OffreEmploi());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePoste(OffreEmploi model)
        {
            if (ModelState.IsValid)
            {
                var userId        = int.Parse(HttpContext.Session.GetString("UserId")!);
                model.IdResponsable = userId;
                model.DateCreation  = DateTime.Now;
                model.Statut        = "ACTIF";

                _context.OffresEmploi.Add(model);
                _context.SaveChanges();
                TempData["Success"] = "Poste créé avec succès.";
                return RedirectToAction("Postes");
            }
            
            // Re-populate departments on validation error
            ViewBag.Departements = _context.Departements
                .Where(d => d.IsActive)
                .OrderBy(d => d.Nom)
                .Select(d => d.Nom)
                .ToList();
            return View(model);
        }

        // ================= MODIFIER UN POSTE =================
        [HttpGet]
        public IActionResult EditPoste(int id)
        {
            var offre = _context.OffresEmploi.Find(id);
            if (offre == null) return NotFound();
            
            // Get departments from database
            ViewBag.Departements = _context.Departements
                .Where(d => d.IsActive)
                .OrderBy(d => d.Nom)
                .Select(d => d.Nom)
                .ToList();
            return View(offre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPoste(OffreEmploi model)
        {
            if (ModelState.IsValid)
            {
                _context.OffresEmploi.Update(model);
                _context.SaveChanges();
                TempData["Success"] = "Poste modifié avec succès.";
                return RedirectToAction("Postes");
            }
            
            // Re-populate departments on validation error
            ViewBag.Departements = _context.Departements
                .Where(d => d.IsActive)
                .OrderBy(d => d.Nom)
                .Select(d => d.Nom)
                .ToList();
            return View(model);
        }

        // ================= SUPPRIMER UN POSTE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePoste(int id)
        {
            try
            {
                if (!OffreDeletionHelper.TryDeleteOffre(_context, id, out var err))
                {
                    TempData["Error"] = err ?? "Impossible de supprimer le poste.";
                    return RedirectToAction("Postes");
                }

                TempData["Success"] = "Poste supprimé avec succès.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Erreur lors de la suppression du poste.";
            }

            return RedirectToAction("Postes");
        }

        // ================= SUPPRIMER PLUSIEURS POSTES =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePostesSelection(string selectedIds)
        {
            if (string.IsNullOrEmpty(selectedIds))
            {
                TempData["Error"] = "Aucun poste sélectionné.";
                return RedirectToAction("Postes");
            }

            var ids = selectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.Parse(s))
                .ToList();

            var deleted = 0;
            foreach (var id in ids)
            {
                if (OffreDeletionHelper.TryDeleteOffre(_context, id, out _))
                    deleted++;
            }

            if (deleted > 0)
                TempData["Success"] = $"{deleted} poste(s) supprimé(s) avec succès.";
            else
                TempData["Error"] = "Aucun poste n'a pu être supprimé.";

            return RedirectToAction("Postes");
        }

        // ================= RÉSULTATS CV =================
        public IActionResult ResultatsCV(int? offreId)
        {
            var offres = _context.OffresEmploi.ToList();
            ViewBag.Offres = offres;

            if (offreId.HasValue)
            {
                MatchIntegrationHelper.EnsureMatchesForOffreAsync(_context, offreId.Value).GetAwaiter().GetResult();

                var matches = _context.Matches
                    .Include(m => m.Cv)
                        .ThenInclude(c => c.DonneesCv)
                    .Include(m => m.Cv)
                        .ThenInclude(c => c.Offre)
                    .Where(m => m.Cv.OffreId == offreId.Value)
                    .OrderByDescending(m => m.GlobalScore)
                    .ToList();
                return View(matches);
            }

            return View(new List<Match>());
        }

        // ================= RÉSULTAT DÉTAILLÉ D'UN CANDIDAT =================
        public IActionResult CvResult(int offreId, int cvId)
        {
            MatchIntegrationHelper.EnsureMatchForCvAsync(_context, offreId, cvId).GetAwaiter().GetResult();
            _context.SaveChanges();

            var match = _context.Matches
                .Include(m => m.Cv)
                    .ThenInclude(c => c.DonneesCv)
                .Include(m => m.Offre)
                .FirstOrDefault(m => m.OffreId == offreId && m.CvId == cvId);

            if (match == null) return NotFound();
            return View("~/Views/Admin/CvResult.cshtml", match);
        }

        // ================= PROFIL CANDIDAT =================
        [HttpGet]
        public async Task<IActionResult> ProfilCandidat(int id)
        {
            var candidat = await _context.Utilisateurs.FindAsync(id);
            if (candidat == null || candidat.Role != "Candidat")
            {
                return NotFound();
            }

            var latestCv = await _context.Cvs
                .Include(c => c.Matches)
                .Where(c => c.UtilisateurId == id)
                .OrderByDescending(c => c.UploadDate)
                .FirstOrDefaultAsync();

            var latestMatch = latestCv?.Matches
                .OrderByDescending(m => m.GlobalScore)
                .FirstOrDefault();

            if (latestMatch != null)
            {
                return RedirectToAction(nameof(CvResult), new { offreId = latestMatch.OffreId, cvId = latestMatch.CvId });
            }

            return View(candidat);
        }

        // ================= PROFIL =================
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user   = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

            var vm = new ProfileEditViewModel
            {
                Id             = user.Id,
                NomUtilisateur = user.NomUtilisateur,
                Email          = user.Email,
                CurrentPhotoUrl = user.PhotoUrl
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Utilisateurs.Find(model.Id);
                if (user == null) return NotFound();

                if (_context.Utilisateurs.Any(u => u.NomUtilisateur == model.NomUtilisateur && u.Id != model.Id))
                {
                    ModelState.AddModelError("NomUtilisateur", "Ce nom d'utilisateur est déjà utilisé.");
                    return View(model);
                }

                if (_context.Utilisateurs.Any(u => u.Email == model.Email && u.Id != model.Id))
                {
                    ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                    return View(model);
                }

                user.NomUtilisateur = model.NomUtilisateur;
                user.Email = model.Email;

                var passwordChanged = false;

                // Handle password change
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    // Verify current password
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        ModelState.AddModelError("CurrentPassword", "Le mot de passe actuel est requis pour changer le mot de passe.");
                        return View(model);
                    }

                    // Verify current password using BCrypt
                    if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.MotPasse))
                    {
                        ModelState.AddModelError("CurrentPassword", "Le mot de passe actuel est incorrect.");
                        return View(model);
                    }

                    // Hash and save new password
                    user.MotPasse = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    passwordChanged = true;
                }

                // Handle profile image upload
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generate unique filename
                    var uniqueFileName = $"{user.Id}_{Guid.NewGuid()}{Path.GetExtension(model.ProfileImage.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(fileStream);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(user.PhotoUrl))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.PhotoUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save new image path
                    user.PhotoUrl = $"/uploads/profiles/{uniqueFileName}";
                }

                _context.SaveChanges();

                HttpContext.Session.SetString("Username", user.NomUtilisateur);
                HttpContext.Session.SetString("UserProfileImage", user.PhotoUrl ?? "");

                TempData["Success"] = passwordChanged
                    ? "Mot de passe modifié avec succès."
                    : "Profil mis à jour avec succès.";
                return RedirectToAction("Profile");
            }
            return View(model);
        }
    }
}