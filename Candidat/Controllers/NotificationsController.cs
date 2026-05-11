using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvParsing.Data;

namespace CvParsing.Controllers;

public class NotificationsController : Controller
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        if (!int.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
            return RedirectToAction("Login", "Account", new { returnUrl = "/Notifications" });

        ViewData["Title"] = "Notifications";
        var notifications = _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int id, CancellationToken ct)
    {
        if (!int.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
            return RedirectToAction(nameof(Index));

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId, ct);
        if (notification == null)
            return RedirectToAction(nameof(Index));

        notification.IsRead = true;
        await _context.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(notification.LinkUrl))
            return Redirect(notification.LinkUrl);

        return RedirectToAction(nameof(Index));
    }
}

