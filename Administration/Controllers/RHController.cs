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
<<<<<<< HEAD
                TotalOffres = _context.OffresEmploi.Count(),
                TotalCvs = _context.Cvs.Count(),
                MonthLabels = new List<string>(),
                OffersPerMonth = new List<int>(),
                PendingCvs = _context.Cvs.Count(c => c.ValidationStatus == "Pending"),
                AcceptedCvs = _context.Cvs.Count(c => c.ValidationStatus == "Accepted"),
                RejectedCvs = _context.Cvs.Count(c => c.ValidationStatus == "Rejected")
            };

            for (int i = 5; i >= 0; i--)
            {
                var targetDate = DateTime.Now.AddMonths(-i);
                stats.MonthLabels.Add(targetDate.ToString("MMM"));
                stats.OffersPerMonth.Add(_context.OffresEmploi.Count(o =>
                    o.DateCreation.Month == targetDate.Month &&
                    o.DateCreation.Year == targetDate.Year));
            }

=======
                TotalOffres  = _context.OffresEmploi.Count(),
                TotalCvs     = _context.Cvs.Count(),
                TotalMatches = _context.Matches.Count()
            };
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
            return View(stats);
        }

        // ================= LISTE DES POSTES =================
        public IActionResult Postes(string? search)
        {
            var query = _context.OffresEmploi.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.Titre.Contains(search) || o.Departement.Contains(search));

            ViewBag.Search = search;
<<<<<<< HEAD
            return View(query.OrderByDescending(o => o.DateCreation).ToList());
        }

        // ================= DÉTAIL D'UN POSTE =================
=======
            return View(query.ToList());
        }

        // ================= DÉTAIL D'UN POSTE =================
        // ✅ ACTION AJOUTÉE
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
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
<<<<<<< HEAD
                return NotFound();
=======
            {
                return NotFound();
            }
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc

            var candidats = offre.Cvs
                .Where(cv => cv.Utilisateur != null)
                .Select(cv => cv.Utilisateur)
                .DistinctBy(u => u.Id)
                .OrderByDescending(u => u.DateCreation)
                .ToList();

<<<<<<< HEAD
            var latestCvsByUser = offre.Cvs
                .GroupBy(cv => cv.UtilisateurId)
                .Select(g => g.OrderByDescending(cv => cv.UploadDate).First())
                .ToList();

            var cvByUserId = latestCvsByUser
                .ToDictionary(cv => cv.UtilisateurId, cv => cv.Id);

            var scoreByUserId = latestCvsByUser
                .ToDictionary(
                    cv => cv.UtilisateurId,
                    cv => cv.Matches.FirstOrDefault()?.GlobalScore ?? 0f
                );

            var validationStatusByUserId = latestCvsByUser
                .ToDictionary(
                    cv => cv.UtilisateurId,
                    cv => cv.ValidationStatus
=======
            var cvByUserId = offre.Cvs
                .GroupBy(cv => cv.UtilisateurId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(cv => cv.UploadDate).First().Id);

            var scoreByUserId = offre.Cvs
                .GroupBy(cv => cv.UtilisateurId)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(cv => cv.Matches).OrderByDescending(m => m.GlobalScore).FirstOrDefault()?.GlobalScore ?? 0f
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
                );

            var viewModel = new PosteDetailViewModel
            {
                Offre = offre,
                Candidats = candidats
            };

            ViewBag.CvByUserId = cvByUserId;
            ViewBag.ScoreByUserId = scoreByUserId;
<<<<<<< HEAD
            ViewBag.ValidationStatusByUserId = validationStatusByUserId;
=======
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc

            return View(viewModel);
        }

        // ================= CRÉER UN POSTE =================
        [HttpGet]
        public IActionResult CreatePoste()
        {
<<<<<<< HEAD
=======
            // Get departments from database
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
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
<<<<<<< HEAD
                var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
                model.IdResponsable = userId;
                model.DateCreation = DateTime.Now;
                model.Statut = "ACTIF";
=======
                var userId        = int.Parse(HttpContext.Session.GetString("UserId")!);
                model.IdResponsable = userId;
                model.DateCreation  = DateTime.Now;
                model.Statut        = "ACTIF";
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc

                _context.OffresEmploi.Add(model);
                _context.SaveChanges();

                var admins = _context.Utilisateurs.Where(u => u.Role == "Admin").Select(u => u.Id).ToList();
                foreach (var adminId in admins)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientUserId = adminId,
                        Title = "Nouveau poste créé",
                        Message = $"Le poste \"{model.Titre}\" a été créé.",
                        Type = "JobCreated",
                        LinkUrl = $"/Admin/DetailsPoste/{model.Id}",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }

                var directors = _context.Utilisateurs
                    .Where(u => u.Role == "Directeur" && !string.IsNullOrWhiteSpace(u.Departements))
                    .ToList()
                    .Where(u => u.Departements!
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Any(d => string.Equals(d, model.Departement, StringComparison.OrdinalIgnoreCase)))
                    .Select(u => u.Id)
                    .Distinct()
                    .ToList();

                foreach (var directorId in directors)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientUserId = directorId,
                        Title = "Nouvelle offre liée à votre département",
                        Message = $"Une nouvelle offre \"{model.Titre}\" a été créée dans le département {model.Departement}.",
                        Type = "DirectorJobOffer",
                        LinkUrl = $"/DirecteurDepartement/DetailsPoste/{model.Id}",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }
                _context.SaveChanges();

                TempData["Success"] = "Poste créé avec succès.";
                return RedirectToAction("Postes");
            }
<<<<<<< HEAD

=======
            
            // Re-populate departments on validation error
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
            ViewBag.Departements = _context.Departements
                .Where(d => d.IsActive)
                .OrderBy(d => d.Nom)
                .Select(d => d.Nom)
                .ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
<<<<<<< HEAD
        public IActionResult SendMessageToCandidates(int offreId, List<int> candidateIds, string rendezVousDate, string rendezVousTime, string locationOrLink, string message)
        {
            if (candidateIds == null || !candidateIds.Any() || string.IsNullOrWhiteSpace(rendezVousDate)
                || string.IsNullOrWhiteSpace(rendezVousTime) || string.IsNullOrWhiteSpace(locationOrLink))
            {
                TempData["Error"] = "Veuillez sélectionner au moins un candidat et renseigner la date, l'heure et le lieu/lien du rendez-vous.";
=======
        public IActionResult SendMessageToCandidates(int offreId, List<int> candidateIds, string message)
        {
            if (candidateIds == null || !candidateIds.Any() || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Veuillez sélectionner au moins un candidat et saisir un message.";
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
                return RedirectToAction(nameof(DetailPoste), new { id = offreId });
            }

            var cleanIds = candidateIds.Distinct().ToList();
            var validCandidates = _context.Utilisateurs
                .Where(u => cleanIds.Contains(u.Id) && u.Role == "Candidat")
                .Select(u => u.Id)
                .ToList();

<<<<<<< HEAD
            var offre = _context.OffresEmploi.Find(offreId);
            var offreTitre = offre?.Titre ?? "inconnue";

            foreach (var candidateId in validCandidates)
            {
                var notificationMessage = $"Un rendez-vous d'entretien a été planifié pour l'offre \"{offreTitre}\".\n" +
                    $"Date : {rendezVousDate}\n" +
                    $"Heure : {rendezVousTime}\n" +
                    $"Lieu / lien : {locationOrLink}";

                if (!string.IsNullOrWhiteSpace(message))
                {
                    notificationMessage += $"\nInstructions : {message.Trim()}";
                }

                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = candidateId,
                    Title = "Rendez-vous d'entretien",
                    Message = notificationMessage,
                    Type = "RendezVous",
=======
            foreach (var candidateId in validCandidates)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = candidateId,
                    Title = "Message RH",
                    Message = message.Trim(),
                    Type = "HRMessage",
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
                    LinkUrl = "/Notifications",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
<<<<<<< HEAD

                var cvs = _context.Cvs.Where(c => c.OffreId == offreId && c.UtilisateurId == candidateId).ToList();
                foreach (var cv in cvs)
                {
                    if (cv.ValidationStatus != "Accepted" && cv.ValidationStatus != "Rejected")
                    {
                        cv.ValidationStatus = "InterviewScheduled";
                    }
                }
            }

            _context.SaveChanges();
            TempData["Success"] = "Rendez-vous envoyé aux candidats sélectionnés.";
            return RedirectToAction(nameof(DetailPoste), new { id = offreId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCandidateRecruitmentStatus(int cvId, int offreId, string status)
        {
            if (status != "Accepted" && status != "Rejected")
            {
                TempData["Error"] = "Statut invalide.";
                return RedirectToAction(nameof(DetailPoste), new { id = offreId });
            }

            var cv = _context.Cvs.FirstOrDefault(c => c.Id == cvId && c.OffreId == offreId);
            if (cv == null)
            {
                TempData["Error"] = "CV introuvable.";
                return RedirectToAction(nameof(DetailPoste), new { id = offreId });
            }

            cv.ValidationStatus = status;
            _context.SaveChanges();

            var offre = _context.OffresEmploi.Find(offreId);
            var offreTitre = offre?.Titre ?? "inconnue";
            var candidate = _context.Utilisateurs.Find(cv.UtilisateurId);
            if (candidate != null)
            {
                var title = status == "Accepted" ? "Candidature acceptée" : "Candidature refusée";
                var message = status == "Accepted"
                    ? $"Félicitations ! Votre candidature pour l'offre \"{offreTitre}\" a été acceptée après le rendez-vous RH."
                    : $"Votre candidature pour l'offre \"{offreTitre}\" a été refusée après le rendez-vous RH.";

                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = candidate.Id,
                    Title = title,
                    Message = message,
                    Type = status == "Accepted" ? "CandidateAccepted" : "CandidateRejected",
                    LinkUrl = "/Notifications",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
                _context.SaveChanges();
            }

            TempData["Success"] = status == "Accepted"
                ? "Recrutement validé et candidat informé."
                : "Candidature refusée et candidat informé.";

=======
            }

            _context.SaveChanges();
            TempData["Success"] = "Message envoyé aux candidats sélectionnés.";
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
            return RedirectToAction(nameof(DetailPoste), new { id = offreId });
        }

        // ================= MODIFIER UN POSTE =================
        [HttpGet]
        public IActionResult EditPoste(int id)
        {
            var offre = _context.OffresEmploi.Find(id);
            if (offre == null) return NotFound();
<<<<<<< HEAD

=======
            
            // Get departments from database
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
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
<<<<<<< HEAD

=======
            
            // Re-populate departments on validation error
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
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

<<<<<<< HEAD
            var ids = selectedIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
=======
            var ids = selectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
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
<<<<<<< HEAD
                return NotFound();
=======
            {
                return NotFound();
            }
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc

            var latestCv = await _context.Cvs
                .Include(c => c.Matches)
                .Where(c => c.UtilisateurId == id)
                .OrderByDescending(c => c.UploadDate)
                .FirstOrDefaultAsync();

            var latestMatch = latestCv?.Matches
                .OrderByDescending(m => m.GlobalScore)
                .FirstOrDefault();

            if (latestMatch != null)
<<<<<<< HEAD
                return RedirectToAction(nameof(CvResult), new { offreId = latestMatch.OffreId, cvId = latestMatch.CvId });
=======
            {
                return RedirectToAction(nameof(CvResult), new { offreId = latestMatch.OffreId, cvId = latestMatch.CvId });
            }
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc

            return View(candidat);
        }

        // ================= PROFIL =================
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
<<<<<<< HEAD
            var user = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

            return View(new ProfileEditViewModel
            {
                Id = user.Id,
                NomUtilisateur = user.NomUtilisateur,
                Email = user.Email,
                CurrentPhotoUrl = user.PhotoUrl
            });
=======
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
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileEditViewModel model)
        {
<<<<<<< HEAD
            // ✅ Toujours ignorer ces champs du ModelState
            ModelState.Remove("ProfileImage");
            ModelState.Remove("Id");
            ModelState.Remove("ActiveTab");

            bool isPasswordTab = model.ActiveTab == "password";

            // ✅ Si onglet profil : ignorer les champs mot de passe
            if (!isPasswordTab)
            {
                ModelState.Remove("CurrentPassword");
                ModelState.Remove("NewPassword");
                ModelState.Remove("ConfirmNewPassword");
            }
            // ✅ Si onglet mot de passe : ignorer les champs profil
            else
            {
                ModelState.Remove("NomUtilisateur");
                ModelState.Remove("Email");
            }

            if (!ModelState.IsValid)
                return View(model);

            var sessionUserId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = _context.Utilisateurs.FirstOrDefault(u => u.Id == sessionUserId);

            if (user == null) return NotFound();

            bool passwordChanged = false;

            // ✅ Traitement onglet PROFIL
            if (!isPasswordTab)
            {
                var newUsername = model.NomUtilisateur?.Trim().ToLower();
                var newEmail = model.Email?.Trim().ToLower();

                if (!string.Equals(user.NomUtilisateur.Trim().ToLower(), newUsername))
                {
                    var usernameExists = _context.Utilisateurs
                        .Any(u => u.NomUtilisateur.ToLower().Trim() == newUsername && u.Id != sessionUserId);

                    if (usernameExists)
                    {
                        ModelState.AddModelError("NomUtilisateur", "Ce nom d'utilisateur est déjà utilisé.");
                        return View(model);
                    }
                }

                if (!string.Equals(user.Email.Trim().ToLower(), newEmail))
                {
                    var emailExists = _context.Utilisateurs
                        .Any(u => u.Email.ToLower().Trim() == newEmail && u.Id != sessionUserId);

                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                        return View(model);
                    }
                }

                user.NomUtilisateur = model.NomUtilisateur!.Trim();
                user.Email = model.Email!.Trim();

                // ================= IMAGE =================
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(ext))
                    {
                        TempData["Error"] = "Format d'image invalide.";
                        return RedirectToAction("Profile");
                    }

                    if (model.ProfileImage.Length > 2 * 1024 * 1024)
                    {
                        TempData["Error"] = "Image trop grande (max 2MB).";
                        return RedirectToAction("Profile");
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{user.Id}_{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(user.PhotoUrl))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                            user.PhotoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                        if (System.IO.File.Exists(oldPath))
                        {
                            try { System.IO.File.Delete(oldPath); } catch { }
                        }
                    }

                    user.PhotoUrl = $"/uploads/profiles/{fileName}";
                }
            }
            // ✅ Traitement onglet MOT DE PASSE
            else
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Le mot de passe actuel est requis.");
                    return View(model);
                }

                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.MotPasse))
                {
                    ModelState.AddModelError("CurrentPassword", "Mot de passe incorrect.");
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.NewPassword))
                {
                    ModelState.AddModelError("NewPassword", "Le nouveau mot de passe est requis.");
                    return View(model);
                }

                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "Les mots de passe ne correspondent pas.");
                    return View(model);
                }

                user.MotPasse = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                passwordChanged = true;
            }

            _context.SaveChanges();

            // Mettre à jour la session
            HttpContext.Session.SetString("Username", user.NomUtilisateur);
            HttpContext.Session.SetString("UserProfileImage", user.PhotoUrl ?? "");

            TempData["Success"] = passwordChanged
                ? "Mot de passe modifié avec succès."
                : "Profil mis à jour avec succès.";

            return RedirectToAction("Profile");
=======
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
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        }
    }
}