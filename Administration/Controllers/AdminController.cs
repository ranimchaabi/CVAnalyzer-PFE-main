using Administration.Data;
using Administration.Filters;
using Administration.Helpers;
using Administration.Models;
using Administration.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Administration.Controllers
{
    [SessionAuthorize("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ================= DASHBOARD =================
        public IActionResult Dashboard()
        {
            var stats = new DashboardStatsViewModel
            {
                TotalUsers      = _context.Utilisateurs.Count(),
                TotalRH         = _context.Utilisateurs.Count(u => u.Role == "RH"),
                TotalDirecteurs = _context.Utilisateurs.Count(u => u.Role == "Directeur"),
                TotalOffres     = _context.OffresEmploi.Count(),
                TotalCvs        = _context.Cvs.Count(),
                TotalMatches    = _context.Matches.Count()
            };
            return View(stats);
        }

        // ================= LISTE UTILISATEURS (avec recherche et filtres) =================
        public IActionResult Users(string? search, string? role, string? departement)
        {
            // Get current logged-in admin ID from session
            var currentUserId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
            
            var query = _context.Utilisateurs
                .Where(u => u.Id != currentUserId) // Exclude current logged-in admin
                .AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.NomUtilisateur.Contains(search) ||
                                         u.Email.Contains(search) ||
                                         u.Role.Contains(search));
            }
            
            ViewBag.Search = search;
            ViewBag.RoleFilter = role;
            ViewBag.DepartementFilter = departement;
            
            // Get unique departments from existing directors
            var departementsList = _context.Utilisateurs
                .Where(u => u.Role == "Directeur" && u.Departements != null)
                .Select(u => u.Departements)
                .AsEnumerable()
                .SelectMany(d => d.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d)
                .ToList();
            
            // Also include departments from the Departement table
            var dbDepartements = _context.Departements
                .Where(d => d.IsActive)
                .Select(d => d.Nom)
                .ToList();
            
            var allDepartements = departementsList
                .Union(dbDepartements, StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d)
                .ToList();
            
            ViewBag.DepartementsList = allDepartements;
            ViewBag.CandidatPublicBaseUrl = _configuration["CandidatMedia:PublicBaseUrl"] ?? "";

            return View(query.OrderBy(u => u.Id).ToList());
        }

        // ================= DÉTAIL UTILISATEUR =================
        public IActionResult UserDetails(int id)
        {
            var user = _context.Utilisateurs.Find(id);
            if (user == null) return NotFound();
            ViewBag.CandidatPublicBaseUrl = _configuration["CandidatMedia:PublicBaseUrl"] ?? "";
            return View(user);
        }

        // ================= AFFICHER FORMULAIRE DE CRÉATION =================
        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewBag.Roles = new List<string> { "Admin", "RH", "Directeur" };
            ViewBag.Departements = _context.Departements
                .Where(d => d.IsActive)
                .OrderBy(d => d.Nom)
                .Select(d => d.Nom)
                .ToList();
            return View(new CreateUserViewModel());
        }

        // ================= CRÉER UTILISATEUR (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(CreateUserViewModel model)
        {
            // Validation conditionnelle : Directeur doit avoir au moins un département
            if (model.Role == "Directeur" && string.IsNullOrWhiteSpace(model.Departements))
                ModelState.AddModelError("Departements", "Veuillez indiquer au moins un département.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new List<string> { "Admin", "RH", "Directeur" };
                ViewBag.Departements = _context.Departements
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Nom)
                    .Select(d => d.Nom)
                    .ToList();
                return View(model);
            }

            if (_context.Utilisateurs.Any(u => u.NomUtilisateur == model.NomUtilisateur))
            {
                ModelState.AddModelError("", "Ce nom d'utilisateur existe déjà.");
                ViewBag.Roles = new List<string> { "Admin", "RH", "Directeur" };
                ViewBag.Departements = _context.Departements
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Nom)
                    .Select(d => d.Nom)
                    .ToList();
                return View(model);
            }

            var user = new Utilisateur
            {
                NomUtilisateur = model.NomUtilisateur,
                Email          = model.Email,
                MotPasse       = BCrypt.Net.BCrypt.HashPassword(model.MotPasse),
                Role           = model.Role,
                IsActive       = true,
                DateCreation   = DateTime.Now,
                Departements   = model.Role == "Directeur" ? model.Departements : null
            };

            _context.Utilisateurs.Add(user);
            _context.SaveChanges();
            TempData["Success"] = "Utilisateur créé avec succès.";
            return RedirectToAction("Users");
        }

        // ================= MODIFIER UTILISATEUR (GET) =================
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Utilisateurs.Find(id);
            if (user == null) return NotFound();

            ViewBag.Roles = new List<string> { "Admin", "RH", "Directeur" };
            ViewBag.PhotoUrl = user.PhotoUrl;
            ViewBag.UserInitial = user.NomUtilisateur.Substring(0, 1).ToUpper();
            
            // Avatar color based on role
            ViewBag.AvatarColor = user.Role switch
            {
                "Admin" => "#ef4444",
                "RH" => "#10b981",
                "Directeur" => "#f59e0b",
                _ => "#6366f1"
            };
            
            return View(new UserEditViewModel
            {
                Id             = user.Id,
                NomUtilisateur = user.NomUtilisateur,
                Email          = user.Email,
                Role           = user.Role,
                IsActive       = user.IsActive,
                Departements   = user.Departements
            });
        }

        // ================= MODIFIER UTILISATEUR (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(UserEditViewModel model)
        {
            if (model.Role == "Directeur" && string.IsNullOrWhiteSpace(model.Departements))
                ModelState.AddModelError("Departements", "Veuillez indiquer au moins un département.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new List<string> { "Admin", "RH", "Directeur" };
                
                // Re-populate viewbag data for the view
                var userForViewBag = _context.Utilisateurs.Find(model.Id);
                if (userForViewBag != null)
                {
                    ViewBag.PhotoUrl = userForViewBag.PhotoUrl;
                    ViewBag.UserInitial = userForViewBag.NomUtilisateur.Substring(0, 1).ToUpper();
                    ViewBag.AvatarColor = userForViewBag.Role switch
                    {
                        "Admin" => "#ef4444",
                        "RH" => "#10b981",
                        "Directeur" => "#f59e0b",
                        _ => "#6366f1"
                    };
                }
                
                return View(model);
            }

            var user = _context.Utilisateurs.Find(model.Id);
            if (user == null) return NotFound();

            user.NomUtilisateur = model.NomUtilisateur;
            user.Email          = model.Email;
            user.Role           = model.Role;
            user.IsActive       = model.IsActive;
            user.Departements   = model.Role == "Directeur" ? model.Departements : null;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (!PasswordRules.TryValidate(model.NewPassword, out var pwdErr))
                {
                    ModelState.AddModelError(nameof(model.NewPassword), pwdErr ?? "Mot de passe invalide.");
                    ViewBag.Roles = new List<string> { "Admin", "RH", "Directeur" };
                    var userForViewBag = _context.Utilisateurs.Find(model.Id);
                    if (userForViewBag != null)
                    {
                        ViewBag.PhotoUrl = userForViewBag.PhotoUrl;
                        ViewBag.UserInitial = userForViewBag.NomUtilisateur.Substring(0, 1).ToUpper();
                        ViewBag.AvatarColor = userForViewBag.Role switch
                        {
                            "Admin" => "#ef4444",
                            "RH" => "#10b981",
                            "Directeur" => "#f59e0b",
                            _ => "#6366f1"
                        };
                    }
                    return View(model);
                }

                user.MotPasse = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            _context.SaveChanges();
            TempData["Success"] = "Utilisateur modifié avec succès.";
            return RedirectToAction("Users");
        }

        // ================= ACTIVER / DÉSACTIVER =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int id)
        {
            var user = _context.Utilisateurs.Find(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _context.SaveChanges();
                var status = user.IsActive ? "activé" : "désactivé";
                TempData["Success"] = $"Utilisateur {status} avec succès.";
            }
            else
            {
                TempData["Error"] = "Utilisateur non trouvé.";
            }
            return RedirectToAction("Users");
        }

        // ================= SUPPRIMER UTILISATEUR =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Utilisateurs.Find(id);
            
            if (user != null)
            {
                // Delete associated CVs first to avoid foreign key constraint
                var userCvs = _context.Cvs.Where(c => c.UtilisateurId == id).ToList();
                if (userCvs.Any())
                {
                    _context.Cvs.RemoveRange(userCvs);
                }
                
                _context.Utilisateurs.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "Utilisateur supprimé.";
            }
            return RedirectToAction("Users");
        }

        // ================= LISTE DES POSTES (avec recherche et filtre département) =================
        public IActionResult Postes(string? search, string? departement)
        {
            var query = _context.OffresEmploi.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.Titre.Contains(search) || o.Departement.Contains(search));

            if (!string.IsNullOrWhiteSpace(departement))
                query = query.Where(o => o.Departement == departement);

            ViewBag.Search = search;
            ViewBag.DepartementFilter = departement;
            ViewBag.Departements = _context.OffresEmploi
                .Select(o => o.Departement)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return View("~/Views/Admin/Postes.cshtml", query.ToList());
        }

        // Alias pour DetailPoste (sans 's') pour éviter les erreurs 404
        public IActionResult DetailPoste(int id)
        {
            MatchIntegrationHelper.EnsureMatchesForOffreAsync(_context, id).GetAwaiter().GetResult();

            var offre = _context.OffresEmploi
                .Include(o => o.Cvs)
                    .ThenInclude(c => c.DonneesCv)
                .Include(o => o.Cvs)
                    .ThenInclude(c => c.Matches)
                .FirstOrDefault(o => o.Id == id);

            if (offre == null) return NotFound();
            return View("DetailsPoste", offre);
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

        // ================= API: CRÉER UN NOUVEAU DÉPARTEMENT =================
        [HttpPost]
        public IActionResult CreateDepartement([FromBody] CreateDepartementRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nom))
            {
                return Json(new { success = false, message = "Le nom du département est requis." });
            }

            // Check if department already exists
            if (_context.Departements.Any(d => d.Nom.ToLower() == request.Nom.ToLower().Trim()))
            {
                return Json(new { success = false, message = "Ce département existe déjà." });
            }

            var departement = new Departement
            {
                Nom = request.Nom.Trim(),
                Description = request.Description?.Trim(),
                DateCreation = DateTime.Now,
                IsActive = true
            };

            _context.Departements.Add(departement);
            _context.SaveChanges();

            return Json(new { success = true, message = "Département créé avec succès.", id = departement.Id, nom = departement.Nom });
        }

        // ================= API: GET ALL DEPARTEMENTS =================
        [HttpGet]
        public IActionResult GetDepartements()
        {
            var departements = _context.Departements
                .Where(d => d.IsActive)
                .OrderBy(d => d.Nom)
                .Select(d => new { d.Id, d.Nom })
                .ToList();

            return Json(departements);
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

        // ================= PROFIL ADMIN =================
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

                // Update username and email
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