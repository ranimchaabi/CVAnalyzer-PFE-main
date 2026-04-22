using Administration.Data;
using Administration.Filters;
using Administration.Helpers;
using Administration.Models;
using Administration.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Administration.Controllers
{
    [SessionAuthorize("Directeur")]
    public class DirecteurDepartementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DirecteurDepartementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= DASHBOARD =================
        public IActionResult Dashboard()
        {
            // Get current user's department
            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = _context.Utilisateurs.Find(userId);
            
            var userDepartments = user?.Departements?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(d => d.Trim())
                                                   .ToList() ?? new List<string>();

            // Get department-specific stats
            var departmentOffres = _context.OffresEmploi
                .Where(o => userDepartments.Contains(o.Departement))
                .ToList();

            var offreIds = departmentOffres.Select(o => o.Id).ToList();
            
            var departmentCvs = _context.Cvs
                .Where(c => offreIds.Contains(c.OffreId))
                .ToList();

            var cvIds = departmentCvs.Select(c => c.Id).ToList();
            
            var departmentMatches = _context.Matches
                .Where(m => cvIds.Contains(m.CvId))
                .ToList();

            var stats = new DashboardStatsViewModel
            {
                TotalOffres  = departmentOffres.Count,
                TotalCvs     = departmentCvs.Count,
                TotalMatches = departmentMatches.Count,
                TotalUsers   = userDepartments.Count  // Using this to pass department count
            };

            ViewBag.UserDepartments = userDepartments;
            ViewBag.AcceptedCount = 0;  // Will be updated when application status is implemented
            ViewBag.RejectedCount = 0;
            return View(stats);
        }

        // ================= LISTE DES POSTES (avec recherche et filtre par département) =================
        public IActionResult Postes(string? search)
        {
            // Get current user's department
            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = _context.Utilisateurs.Find(userId);
            
            if (user == null || string.IsNullOrEmpty(user.Departements))
            {
                return View(new List<OffreEmploi>());
            }

            // Parse departments (comma-separated)
            var userDepartments = user.Departements.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(d => d.Trim())
                                                   .ToList();

            var query = _context.OffresEmploi
                .Where(o => userDepartments.Contains(o.Departement))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.Titre.Contains(search) || o.Departement.Contains(search));

            ViewBag.Search = search;
            ViewBag.UserDepartments = userDepartments;
            return View(query.ToList());
        }

        // ================= DÉTAIL D'UN POSTE + CANDIDATS =================
        public IActionResult DetailsPoste(int id)
        {
            MatchIntegrationHelper.EnsureMatchesForOffreAsync(_context, id).GetAwaiter().GetResult();

            var offre = _context.OffresEmploi
                .Include(o => o.Cvs)
                    .ThenInclude(c => c.DonneesCv)
                .Include(o => o.Cvs)
                    .ThenInclude(c => c.Matches)
                .FirstOrDefault(o => o.Id == id);

            if (offre == null) return NotFound();
            return View(offre);
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
            return View(match);
        }

        // ================= CRÉER UN POSTE =================
        [HttpGet]
        public IActionResult CreatePoste() => View(new OffreEmploi());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePoste(OffreEmploi model)
        {
            if (ModelState.IsValid)
            {
                model.DateCreation  = DateTime.Now;
                model.Statut        = "ACTIF";
                model.IdResponsable = 0;
                _context.OffresEmploi.Add(model);
                _context.SaveChanges();
                TempData["Success"] = "Poste créé avec succès.";
                return RedirectToAction("Postes");
            }
            return View(model);
        }

        // ================= MODIFIER UN POSTE =================
        [HttpGet]
        public IActionResult EditPoste(int id)
        {
            var offre = _context.OffresEmploi.Find(id);
            if (offre == null) return NotFound();
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

                ViewBag.SelectedOffreId = offreId.Value;
                return View(matches);
            }

            return View(new List<Match>());
        }

        // ================= PROFIL =================
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

            var vm = new ProfileEditViewModel
            {
                Id = user.Id,
                NomUtilisateur = user.NomUtilisateur,
                Email = user.Email,
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