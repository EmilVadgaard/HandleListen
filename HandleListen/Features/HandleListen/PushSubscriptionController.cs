using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/push-subscriptions")]
public class PushSubscriptionController : ControllerBase
{
    private readonly AppDbContext _context;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public PushSubscriptionController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterPushTokenRequest request)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token)) return BadRequest();

        var existing = await _context.PushSubscriptions.FirstOrDefaultAsync(s => s.Token == token);
        if (existing is not null)
        {
            // The same device token re-registering (app reinstall, token refresh) should move
            // to whichever user is currently logged in rather than create a duplicate row.
            existing.UserId = UserId;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        _context.PushSubscriptions.Add(new PushSubscription { UserId = UserId, Token = token });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Unregister(RegisterPushTokenRequest request)
    {
        var token = request.Token?.Trim();
        var existing = await _context.PushSubscriptions.FirstOrDefaultAsync(s => s.Token == token && s.UserId == UserId);
        if (existing is not null)
        {
            _context.PushSubscriptions.Remove(existing);
            await _context.SaveChangesAsync();
        }
        return NoContent();
    }
}
