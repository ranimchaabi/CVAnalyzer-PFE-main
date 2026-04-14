using Administration.Data;
using Administration.Filters;
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

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
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

        // ================= LISTE UTILISATEURS (avec recherche) =================
        public IActionResult Users(string? search)
        {
            var query = _context.Utilisateurs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.NomUtilisateur.Contains(search) ||
                                         u.Email.Contains(search) ||
                                         u.Role.Contains(search));
            }
            ViewBag.Search = search;
            return View(query.OrderBy(u => u.Id).ToList());
        }

        // ================= DÉTAIL UTILISATEUR =================
        public IActionResult UserDetails(int id)
        {
            var user = _context.Utilisateurs.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // ================= AFFICHER FORMULAIRE DE CRÉATION =================
        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewBag.Roles = new List<string> { "Admin", "RH", "Directeur" };
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
                return View(model);
            }

            if (_context.Utilisateurs.Any(u => u.NomUtilisateur == model.NomUtilisateur))
            {
                ModelState.AddModelError("", "Ce nom d'utilisateur existe déjà.");
                ViewBag.Roles = new List<string> { "Admin", "RH", "Directeur" };
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
                return View(model);
            }

            var user = _context.Utilisateurs.Find(model.Id);
            if (user == null) return NotFound();

            user.Email        = model.Email;
            user.Role         = model.Role;
            user.IsActive     = model.IsActive;
            user.Departements = model.Role == "Directeur" ? model.Departements : null;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
                user.MotPasse = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

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

        // ================= DÉTAIL D'UN POSTE + CANDIDATS =================
        public IActionResult DetailsPoste(int id)
        {
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
                Email = user.Email
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Utilisateurs.Find(model.Id);
                if (user == null) return NotFound();

                user.Email = model.Email;
                if (!string.IsNullOrEmpty(model.NewPassword))
                    user.MotPasse = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

                _context.SaveChanges();
                TempData["Success"] = "Profil mis à jour.";
                return RedirectToAction("Profile");
            }
            return View(model);
        }
    }
}