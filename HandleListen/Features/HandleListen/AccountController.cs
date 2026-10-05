using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("me")]
    public async Task<ActionResult<MeDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return new MeDto(user.Email ?? string.Empty, roles.ToList());
    }
}
