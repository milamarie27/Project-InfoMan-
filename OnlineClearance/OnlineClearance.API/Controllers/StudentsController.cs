using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineClearance.API.Data;
using OnlineClearance.API.DTOs;

namespace OnlineClearance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudentsController(AppDbContext db) => _db = db;

    // GET /api/students
    [HttpGet]
    [Authorize(Roles = "admin,signatory")]
    public async Task<IActionResult> GetAll([FromQuery] int? curriculumId, [FromQuery] string? search)
    {
        var query = _db.Students
            .Include(s => s.User)
            .Include(s => s.Curriculum)
                .ThenInclude(c => c!.Course)
            .AsQueryable();

        if (curriculumId.HasValue)
            query = query.Where(s => s.CurriculumId == curriculumId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.StudentNumber.Contains(search) ||
                s.User!.FirstName.Contains(search) ||
                s.User!.LastName.Contains(search));

        var students = await query
            .Select(s => new StudentDto(
                s.Id,
                s.StudentNumber,
                $"{s.User!.FirstName} {s.User.MiddleInitial ?? ""} {s.User.LastName}".Trim(),
                s.Curriculum!.Course!.CourseCode,
                s.Curriculum.Course.Description ?? "",
                s.Curriculum.YearLevel,
                s.Curriculum.Section,
                s.Status,
                s.User.Username
            ))
            .ToListAsync();

        return Ok(students);
    }

    // GET /api/students/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _db.Students
            .Include(s => s.User)
            .Include(s => s.Curriculum).ThenInclude(c => c!.Course)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (s == null) return NotFound();

        return Ok(new StudentDto(
            s.Id,
            s.StudentNumber,
            $"{s.User!.FirstName} {s.User.MiddleInitial ?? ""} {s.User.LastName}".Trim(),
            s.Curriculum!.Course!.CourseCode,
            s.Curriculum.Course.Description ?? "",
            s.Curriculum.YearLevel,
            s.Curriculum.Section,
            s.Status,
            s.User.Username
        ));
    }

    // GET /api/students/by-number/{studentNumber}
    [HttpGet("by-number/{studentNumber}")]
    public async Task<IActionResult> GetByNumber(string studentNumber)
    {
        var s = await _db.Students
            .Include(s => s.User)
            .Include(s => s.Curriculum).ThenInclude(c => c!.Course)
            .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

        if (s == null) return NotFound();

        return Ok(new StudentDto(
            s.Id,
            s.StudentNumber,
            $"{s.User!.FirstName} {s.User.MiddleInitial ?? ""} {s.User.LastName}".Trim(),
            s.Curriculum!.Course!.CourseCode,
            s.Curriculum.Course.Description ?? "",
            s.Curriculum.YearLevel,
            s.Curriculum.Section,
            s.Status,
            s.User.Username
        ));
    }

    // PUT /api/students/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentRequest req)
    {
        var student = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound();

        student.Status = req.Status;
        student.CurriculumId = req.CurriculumId;
        student.User!.FirstName = req.FirstName;
        student.User.LastName = req.LastName;
        student.User.MiddleInitial = req.MiddleInitial;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated." });
    }

    // DELETE /api/students/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return NotFound();

        _db.Students.Remove(student);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}

public record UpdateStudentRequest(
    string FirstName, string LastName, string? MiddleInitial,
    int CurriculumId, string Status);
