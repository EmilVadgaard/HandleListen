using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/personal-notes")]
public class PersonalNoteController : ControllerBase
{
    private readonly AppDbContext _context;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public PersonalNoteController(AppDbContext context)
    {
        _context = context;
    }

    private static PersonalNoteDto ToDto(PersonalNote note) => new(note.Id, note.Text, note.Date);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonalNoteDto>>> GetAll()
    {
        var notes = await _context.PersonalNotes.Where(n => n.UserId == UserId).ToListAsync();
        return notes.Select(ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<PersonalNoteDto>> Create(CreatePersonalNoteRequest request)
    {
        var text = request.Text?.Trim() ?? string.Empty;
        if (text.Length == 0) return BadRequest("Tekst er påkrævet.");

        var note = new PersonalNote { UserId = UserId, Text = text, Date = request.Date };
        _context.PersonalNotes.Add(note);
        await _context.SaveChangesAsync();
        return ToDto(note);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, UpdatePersonalNoteRequest request)
    {
        var note = await _context.PersonalNotes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (note is null) return NotFound();

        var text = request.Text?.Trim() ?? string.Empty;
        if (text.Length == 0) return BadRequest("Tekst er påkrævet.");

        note.Text = text;
        note.Date = request.Date;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var note = await _context.PersonalNotes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (note is null) return NotFound();

        _context.PersonalNotes.Remove(note);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
