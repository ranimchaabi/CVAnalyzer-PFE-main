using Administration.Data;
using Administration.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Administration.Controllers;

[SessionAuthorize("Admin", "RH", "Directeur")]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!int.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var notifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int id, CancellationToken ct)
    {
        if (!int.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId, ct);

        if (notification == null)
        {
            return RedirectToAction(nameof(Index));
        }

<<<<<<< HEAD
        _context.Notifications.Remove(notification);
=======
        notification.IsRead = true;
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        await _context.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(notification.LinkUrl))
        {
            return Redirect(notification.LinkUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
