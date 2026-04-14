using Administration.Data;
using Administration.Filters;
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
            var stats = new DashboardStatsViewModel
            {
                TotalOffres  = _context.OffresEmploi.Count(),
                TotalCvs     = _context.Cvs.Count(),
                TotalMatches = _context.Matches.Count()
            };
            return View(stats);
        }

        // ================= LISTE DES POSTES (avec recherche) =================
        public IActionResult Postes(string? search)
        {
            var query = _context.OffresEmploi.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.Titre.Contains(search) || o.Departement.Contains(search));
            ViewBag.Search = search;
            return View(query.ToList());
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
            var offre = _context.OffresEmploi.Find(id);
            if (offre != null)
            {
                _context.OffresEmploi.Remove(offre);
                _context.SaveChanges();
                TempData["Success"] = "Poste supprimé.";
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
            var user   = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

            var vm = new ProfileEditViewModel
            {
                Id             = user.Id,
                NomUtilisateur = user.NomUtilisateur,
                Email          = user.Email
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