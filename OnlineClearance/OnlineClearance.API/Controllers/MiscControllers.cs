using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineClearance.API.Data;
using OnlineClearance.API.DTOs;
using OnlineClearance.API.Models;
using System.Security.Claims;

namespace OnlineClearance.API.Controllers;

// ─── ANNOUNCEMENTS ────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AnnouncementsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Announcements
            .Include(a => a.Author)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto(
                a.Id, a.Title, a.Content,
                $"{a.Author!.FirstName} {a.Author.LastName}",
                a.Type, a.CreatedAt))
            .ToListAsync());

    [HttpPost]
    [Authorize(Roles = "admin,signatory")]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ann = new Announcement
        {
            Title = req.Title,
            Content = req.Content,
            Type = req.Type,
            AuthorId = userId
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Created.", id = ann.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,signatory")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAnnouncementRequest req)
    {
        var ann = await _db.Announcements.FindAsync(id);
        if (ann == null) return NotFound();
        ann.Title = req.Title;
        ann.Content = req.Content;
        ann.Type = req.Type;
        ann.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ann = await _db.Announcements.FindAsync(id);
        if (ann == null) return NotFound();
        _db.Announcements.Remove(ann);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}

// ─── SIGNATORIES ─────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SignatoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public SignatoriesController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Signatories
            .Include(s => s.User)
            .Select(s => new SignatoryDto(
                s.Id, s.EmployeeId,
                $"{s.User!.FirstName} {s.User.LastName}",
                s.User.Username,
                s.UploadedSignaturePath,
                s.User.IsActive))
            .ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _db.Signatories.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        return Ok(new SignatoryDto(s.Id, s.EmployeeId, $"{s.User!.FirstName} {s.User.LastName}",
            s.User.Username, s.UploadedSignaturePath, s.User.IsActive));
    }

    // POST /api/signatories/{id}/signature  - upload signature image
    [HttpPost("{id:int}/signature")]
    [Authorize(Roles = "signatory,admin")]
    public async Task<IActionResult> UploadSignature(int id, IFormFile file)
    {
        var sig = await _db.Signatories.FindAsync(id);
        if (sig == null) return NotFound();

        if (file.Length > 2 * 1024 * 1024) return BadRequest(new { message = "File too large (max 2MB)." });
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext is not ".png" and not ".jpg" and not ".jpeg") return BadRequest(new { message = "Only PNG/JPG allowed." });

        var sigDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "signatures");
        Directory.CreateDirectory(sigDir);

        var fileName = $"sig_{sig.EmployeeId}{ext}";
        var path = Path.Combine(sigDir, fileName);
        using var fs = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(fs);

        sig.UploadedSignaturePath = $"/signatures/{fileName}";
        await _db.SaveChangesAsync();

        return Ok(new { path = sig.UploadedSignaturePath });
    }

    [HttpPut("{id:int}/deactivate")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var sig = await _db.Signatories.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
        if (sig == null) return NotFound();
        sig.User!.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deactivated." });
    }
}

// ─── STATUS TABLE ─────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatusController : ControllerBase
{
    private readonly AppDbContext _db;
    public StatusController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.StatusTable.Select(s => new { s.Id, s.Label }).ToListAsync());
}
