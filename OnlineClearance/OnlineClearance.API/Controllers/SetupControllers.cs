using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineClearance.API.Data;
using OnlineClearance.API.DTOs;
using OnlineClearance.API.Models;

namespace OnlineClearance.API.Controllers;

// ─── COURSES ─────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CoursesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Courses.Select(c => new CourseDto(c.Id, c.CourseCode, c.Description)).ToListAsync());

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest req)
    {
        if (await _db.Courses.AnyAsync(c => c.CourseCode == req.CourseCode))
            return Conflict(new { message = "Course code exists." });
        var c = new Course { CourseCode = req.CourseCode, Description = req.Description };
        _db.Courses.Add(c);
        await _db.SaveChangesAsync();
        return Ok(new CourseDto(c.Id, c.CourseCode, c.Description));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Courses.FindAsync(id);
        if (c == null) return NotFound();
        _db.Courses.Remove(c);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}

// ─── CURRICULUM ──────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class CurriculumController : ControllerBase
{
    private readonly AppDbContext _db;
    public CurriculumController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Curricula
            .Include(c => c.Course)
            .Select(c => new CurriculumDto(c.Id, c.CourseId, c.Course!.CourseCode, c.Course.Description, c.YearLevel, c.Section))
            .ToListAsync());

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateCurriculumRequest req)
    {
        var c = new Curriculum { CourseId = req.CourseId, YearLevel = req.YearLevel, Section = req.Section };
        _db.Curricula.Add(c);
        await _db.SaveChangesAsync();
        var full = await _db.Curricula.Include(x => x.Course).FirstAsync(x => x.Id == c.Id);
        return Ok(new CurriculumDto(full.Id, full.CourseId, full.Course!.CourseCode, full.Course.Description, full.YearLevel, full.Section));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Curricula.FindAsync(id);
        if (c == null) return NotFound();
        _db.Curricula.Remove(c);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}

// ─── ACADEMIC PERIODS ─────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeriodsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PeriodsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.AcademicPeriods
            .Select(p => new AcademicPeriodDto(p.Id, p.AcademicYear, p.Semester, p.IsActive))
            .ToListAsync());

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var p = await _db.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);
        return p == null ? NotFound() : Ok(new AcademicPeriodDto(p.Id, p.AcademicYear, p.Semester, p.IsActive));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreatePeriodRequest req)
    {
        var p = new AcademicPeriod { AcademicYear = req.AcademicYear, Semester = req.Semester };
        _db.AcademicPeriods.Add(p);
        await _db.SaveChangesAsync();
        return Ok(new AcademicPeriodDto(p.Id, p.AcademicYear, p.Semester, p.IsActive));
    }

    [HttpPut("{id:int}/activate")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Activate(int id)
    {
        // Deactivate all then activate selected
        await _db.AcademicPeriods.ExecuteUpdateAsync(p => p.SetProperty(x => x.IsActive, false));
        var p = await _db.AcademicPeriods.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = true;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Period activated." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.AcademicPeriods.FindAsync(id);
        if (p == null) return NotFound();
        _db.AcademicPeriods.Remove(p);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}

// ─── SUBJECTS ─────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SubjectsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Subjects
            .Select(s => new SubjectDto(s.Id, s.SubjectCode, s.Title, s.LecUnits, s.LabUnits))
            .ToListAsync());

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateSubjectRequest req)
    {
        var s = new Subject { SubjectCode = req.SubjectCode, Title = req.Title, LecUnits = req.LecUnits, LabUnits = req.LabUnits };
        _db.Subjects.Add(s);
        await _db.SaveChangesAsync();
        return Ok(new SubjectDto(s.Id, s.SubjectCode, s.Title, s.LecUnits, s.LabUnits));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Subjects.FindAsync(id);
        if (s == null) return NotFound();
        _db.Subjects.Remove(s);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}

// ─── SUBJECT OFFERINGS ────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OfferingsController : ControllerBase
{
    private readonly AppDbContext _db;
    public OfferingsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? periodId, [FromQuery] int? instructorId)
    {
        var q = _db.SubjectOfferings
            .Include(o => o.Instructor).ThenInclude(i => i!.User)
            .Include(o => o.Period)
            .AsQueryable();

        if (periodId.HasValue) q = q.Where(o => o.PeriodId == periodId);
        if (instructorId.HasValue) q = q.Where(o => o.InstructorId == instructorId);

        return Ok(await q.Select(o => new SubjectOfferingDto(
            o.Id, o.MisCode, o.SubjectCode,
            "", // title fetched from subjects table separately
            o.InstructorId,
            $"{o.Instructor!.User!.FirstName} {o.Instructor.User.LastName}",
            o.Instructor.EmployeeId,
            o.PeriodId, o.Period!.AcademicYear, o.Period.Semester
        )).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateSubjectOfferingRequest req)
    {
        if (await _db.SubjectOfferings.AnyAsync(o => o.MisCode == req.MisCode))
            return Conflict(new { message = "MIS code already exists." });

        var offering = new SubjectOffering
        {
            MisCode = req.MisCode,
            SubjectCode = req.SubjectCode,
            InstructorId = req.InstructorId,
            PeriodId = req.PeriodId
        };
        _db.SubjectOfferings.Add(offering);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Created.", id = offering.Id });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var o = await _db.SubjectOfferings.FindAsync(id);
        if (o == null) return NotFound();
        _db.SubjectOfferings.Remove(o);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}

// ─── ORGANIZATIONS ────────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrganizationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? curriculumId)
    {
        var q = _db.Organizations
            .Include(o => o.Signatory).ThenInclude(s => s!.User)
            .Include(o => o.Curriculum).ThenInclude(c => c!.Course)
            .AsQueryable();

        if (curriculumId.HasValue) q = q.Where(o => o.CurriculumId == curriculumId);

        return Ok(await q.Select(o => new OrganizationDto(
            o.Id, o.OrgName,
            o.SignatoryId,
            $"{o.Signatory!.User!.FirstName} {o.Signatory.User.LastName}",
            o.PositionTitle,
            o.CurriculumId,
            o.Curriculum!.Course!.CourseCode,
            o.Curriculum.YearLevel,
            o.Curriculum.Section
        )).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest req)
    {
        var org = new Organization
        {
            OrgName = req.OrgName,
            SignatoryId = req.SignatoryId,
            PositionTitle = req.PositionTitle,
            CurriculumId = req.CurriculumId
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Created.", id = org.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateOrganizationRequest req)
    {
        var org = await _db.Organizations.FindAsync(id);
        if (org == null) return NotFound();
        org.OrgName = req.OrgName;
        org.SignatoryId = req.SignatoryId;
        org.PositionTitle = req.PositionTitle;
        org.CurriculumId = req.CurriculumId;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var org = await _db.Organizations.FindAsync(id);
        if (org == null) return NotFound();
        _db.Organizations.Remove(org);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}
