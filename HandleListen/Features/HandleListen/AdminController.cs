using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Owner-only (not Moderator) - this is a single-person curation tool for the known-item
// catalog, not a shared family feature like the Owner/Moderator-gated known-item endpoints.
[Authorize(Roles = "Owner")]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("known-item-requests")]
    public async Task<ActionResult<IEnumerable<KnownItemRequestDto>>> GetKnownItemRequests()
    {
        var requests = await _context.KnownItemRequests
            .Where(r => !r.Resolved)
            .OrderByDescending(r => r.SeenCount)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();

        var userIds = requests.Select(r => r.RequestedByUserId).Distinct().ToList();
        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

        return requests
            .Select(r => new KnownItemRequestDto(
                r.Id, r.Name, r.SourceDescription,
                users.FirstOrDefault(u => u.Id == r.RequestedByUserId)?.Email ?? "",
                r.CreatedAt, r.LastSeenAt, r.SeenCount))
            .ToList();
    }

    [HttpPost("known-item-requests/{id}/resolve")]
    public async Task<IActionResult> ResolveKnownItemRequest(int id)
    {
        var request = await _context.KnownItemRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request is null) return NotFound();

        request.Resolved = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
