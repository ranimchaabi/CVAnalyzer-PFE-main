using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvParsing.Data;
using CvParsing.Models.Dtos;
using CvParsing.Services;
using Microsoft.AspNetCore.DataProtection;

namespace CvParsing.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly PasswordService _passwordService;
    private readonly IDataProtector _protector;

    public AuthController(
        AppDbContext context, 
        IEmailSender emailSender, 
        PasswordService passwordService,
        IDataProtectionProvider provider)
    {
        _context = context;
        _emailSender = emailSender;
        _passwordService = passwordService;
        // Création du protecteur pour les tokens de réinitialisation
        _protector = provider.CreateProtector("PasswordResetToken");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Email invalide." });

        var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user == null)
        {
            // Sécurité : ne pas révéler si l'email existe
            return Ok(new { message = "Si un compte existe avec cet email, un lien de réinitialisation a été envoyé." });
        }

        // 1. Génération du token d'expiration protégé : format "UserId|TicksExpiration"
        var ticksExp = DateTime.UtcNow.AddHours(1).Ticks;
        var tokenData = $"{user.Id}|{ticksExp}";
        var protectedToken = _protector.Protect(tokenData);

        // 2. Construire le lien de réinitialisation (frontend)
        string resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?token={Uri.EscapeDataString(protectedToken)}";
        bool emailSent = await _emailSender.SendPasswordResetAsync(user.Email, resetLink, ct);

        if (!emailSent)
        {
            // Log erreur mais ne pas exposer à l'utilisateur
            return StatusCode(500, new { message = "Erreur lors de l'envoi de l'email." });
        }

        return Ok(new { message = "Un lien de réinitialisation a été envoyé à votre adresse email." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Données invalides." });

        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new { message = "Les mots de passe ne correspondent pas." });

        int userId = 0;
        try
        {
            // 1. Déchiffrer le token
            var unprotectedData = _protector.Unprotect(request.Token);
            var parts = unprotectedData.Split('|');
            if (parts.Length != 2)
                return BadRequest(new { message = "Lien invalide." });

            userId = int.Parse(parts[0]);
            var expirationTicks = long.Parse(parts[1]);
            var expDate = new DateTime(expirationTicks, DateTimeKind.Utc);

            if (DateTime.UtcNow > expDate)
                return BadRequest(new { message = "Le lien a expiré." });
        }
        catch
        {
            return BadRequest(new { message = "Lien invalide ou corrompu." });
        }

        // On cherche le user
        var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return BadRequest(new { message = "Utilisateur introuvable." });

        // 2. Mettre à jour le mot de passe
        user.MotPasse = _passwordService.Hash(request.NewPassword);
        
        // Comme nous utilisons IDataProtector (sans BDD),
        // pour empêcher la réutilisation du token avant son expiration,
        // nous pourrions stocker la date de dernière mise à jour du mot de passe
        // Mais pour la compatibilité stricte de la Base de données actuelle, on s'appuie sur la fenêtre de 1 heure.

        await _context.SaveChangesAsync(ct);

        return Ok(new { message = "Mot de passe réinitialisé avec succès." });
    }
}