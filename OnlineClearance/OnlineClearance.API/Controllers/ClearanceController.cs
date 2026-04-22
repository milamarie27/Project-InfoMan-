using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineClearance.API.Data;
using OnlineClearance.API.DTOs;
using OnlineClearance.API.Models;
using System.Security.Claims;

namespace OnlineClearance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClearanceController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClearanceController(AppDbContext db) => _db = db;

    // ─── GENERATE clearance entries for a student + period ───────────────
    // POST /api/clearance/generate
    [HttpPost("generate")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GenerateClearance([FromBody] GenerateClearanceRequest req)
    {
        var student = await _db.Students
            .Include(s => s.Curriculum)
            .FirstOrDefaultAsync(s => s.Id == req.StudentId);
        if (student == null) return NotFound(new { message = "Student not found." });

        var period = await _db.AcademicPeriods.FindAsync(req.PeriodId);
        if (period == null) return BadRequest(new { message = "Invalid period." });

        var pendingStatus = await _db.StatusTable.FirstOrDefaultAsync(s => s.Label == "Pending");
        if (pendingStatus == null) return StatusCode(500, "Status table not seeded.");

        int created = 0;

        // Subject clearances — from offerings this period
        var offerings = await _db.SubjectOfferings
            .Where(o => o.PeriodId == req.PeriodId)
            .ToListAsync();

        foreach (var offering in offerings)
        {
            var exists = await _db.ClearanceSubjects.AnyAsync(cs =>
                cs.StudentId == student.Id &&
                cs.SubjectOfferingId == offering.Id &&
                cs.PeriodId == req.PeriodId);

            if (!exists)
            {
                _db.ClearanceSubjects.Add(new ClearanceSubject
                {
                    StudentId = student.Id,
                    SubjectOfferingId = offering.Id,
                    StatusId = pendingStatus.Id,
                    PeriodId = req.PeriodId
                });
                created++;
            }
        }

        // Org clearances — from orgs matching curriculum
        var orgs = await _db.Organizations
            .Where(o => o.CurriculumId == student.CurriculumId)
            .ToListAsync();

        foreach (var org in orgs)
        {
            var exists = await _db.ClearanceOrganizations.AnyAsync(co =>
                co.StudentId == student.Id &&
                co.OrganizationId == org.Id &&
                co.PeriodId == req.PeriodId);

            if (!exists)
            {
                _db.ClearanceOrganizations.Add(new ClearanceOrganization
                {
                    StudentId = student.Id,
                    OrganizationId = org.Id,
                    PeriodId = req.PeriodId,
                    StatusId = pendingStatus.Id
                });
                created++;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Generated {created} clearance entries." });
    }

    // ─── GET student's clearance summary ────────────────────────────────
    // GET /api/clearance/summary/{studentId}/{periodId}
    [HttpGet("summary/{studentId:int}/{periodId:int}")]
    public async Task<IActionResult> GetSummary(int studentId, int periodId)
    {
        var student = await _db.Students
            .Include(s => s.User)
            .Include(s => s.Curriculum).ThenInclude(c => c!.Course)
            .FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null) return NotFound();

        var period = await _db.AcademicPeriods.FindAsync(periodId);
        if (period == null) return NotFound();

        var clearedId = (await _db.StatusTable.FirstOrDefaultAsync(s => s.Label == "Cleared"))?.Id ?? 2;

        var subjectTotal = await _db.ClearanceSubjects.CountAsync(cs => cs.StudentId == studentId && cs.PeriodId == periodId);
        var subjectCleared = await _db.ClearanceSubjects.CountAsync(cs => cs.StudentId == studentId && cs.PeriodId == periodId && cs.StatusId == clearedId);
        var orgTotal = await _db.ClearanceOrganizations.CountAsync(co => co.StudentId == studentId && co.PeriodId == periodId);
        var orgCleared = await _db.ClearanceOrganizations.CountAsync(co => co.StudentId == studentId && co.PeriodId == periodId && co.StatusId == clearedId);

        return Ok(new StudentClearanceSummaryDto(
            student.StudentNumber,
            $"{student.User!.FirstName} {student.User.LastName}",
            student.Curriculum!.Course!.CourseCode,
            student.Curriculum.YearLevel,
            student.Curriculum.Section,
            subjectTotal, subjectCleared,
            orgTotal, orgCleared,
            subjectTotal > 0 && subjectCleared == subjectTotal && orgCleared == orgTotal,
            period.AcademicYear, period.Semester
        ));
    }

    // ─── GET all subject clearances for instructor or student ────────────
    // GET /api/clearance/subjects?periodId=&studentId=&instructorId=
    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjectClearances(
        [FromQuery] int? periodId,
        [FromQuery] int? studentId,
        [FromQuery] int? instructorId)
    {
        var query = _db.ClearanceSubjects
            .Include(cs => cs.Student).ThenInclude(s => s!.User)
            .Include(cs => cs.SubjectOffering).ThenInclude(so => so!.Instructor).ThenInclude(i => i!.User)
            .Include(cs => cs.Status)
            .Include(cs => cs.Period)
            .AsQueryable();

        if (periodId.HasValue) query = query.Where(cs => cs.PeriodId == periodId);
        if (studentId.HasValue) query = query.Where(cs => cs.StudentId == studentId);
        if (instructorId.HasValue) query = query.Where(cs => cs.SubjectOffering!.InstructorId == instructorId);

        var result = await query.Select(cs => new ClearanceSubjectDto(
            cs.Id,
            cs.Student!.StudentNumber,
            $"{cs.Student.User!.FirstName} {cs.Student.User.LastName}",
            cs.SubjectOffering!.MisCode,
            cs.SubjectOffering.SubjectCode,
            "",
            $"{cs.SubjectOffering.Instructor!.User!.FirstName} {cs.SubjectOffering.Instructor.User.LastName}",
            cs.Status!.Label,
            cs.StatusId,
            cs.Remarks,
            cs.SignedAt,
            cs.CreatedAt
        )).ToListAsync();

        return Ok(result);
    }

    // ─── APPROVE/REJECT subject clearance ───────────────────────────────
    // PUT /api/clearance/subjects/{id}/approve
    [HttpPut("subjects/{id:int}/approve")]
    [Authorize(Roles = "signatory,admin")]
    public async Task<IActionResult> ApproveSubject(int id, [FromBody] ApproveRequest req)
    {
        var cs = await _db.ClearanceSubjects
            .Include(c => c.SubjectOffering)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (cs == null) return NotFound();

        // Verify caller is the instructor or admin
        var callerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != "admin")
        {
            var signatory = await _db.Signatories.FirstOrDefaultAsync(s => s.UserId == callerId);
            if (signatory == null || cs.SubjectOffering!.InstructorId != signatory.Id)
                return Forbid();
        }

        cs.StatusId = req.StatusId;
        cs.Remarks = req.Remarks;
        cs.SignedAt = req.StatusId == 2 ? DateTime.UtcNow : null;
        cs.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated." });
    }

    // ─── GET all org clearances ──────────────────────────────────────────
    // GET /api/clearance/organizations?periodId=&studentId=&signatoryId=
    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrgClearances(
        [FromQuery] int? periodId,
        [FromQuery] int? studentId,
        [FromQuery] int? signatoryId)
    {
        var query = _db.ClearanceOrganizations
            .Include(co => co.Student).ThenInclude(s => s!.User)
            .Include(co => co.Organization).ThenInclude(o => o!.Signatory).ThenInclude(s => s!.User)
            .Include(co => co.Status)
            .AsQueryable();

        if (periodId.HasValue) query = query.Where(co => co.PeriodId == periodId);
        if (studentId.HasValue) query = query.Where(co => co.StudentId == studentId);
        if (signatoryId.HasValue) query = query.Where(co => co.Organization!.SignatoryId == signatoryId);

        var result = await query.Select(co => new ClearanceOrgDto(
            co.Id,
            co.Student!.StudentNumber,
            $"{co.Student.User!.FirstName} {co.Student.User.LastName}",
            co.Organization!.OrgName,
            $"{co.Organization.Signatory!.User!.FirstName} {co.Organization.Signatory.User.LastName}",
            co.Organization.PositionTitle,
            co.Status!.Label,
            co.StatusId,
            co.Remarks,
            co.SignedAt,
            co.CreatedAt
        )).ToListAsync();

        return Ok(result);
    }

    // ─── APPROVE/REJECT org clearance ───────────────────────────────────
    // PUT /api/clearance/organizations/{id}/approve
    [HttpPut("organizations/{id:int}/approve")]
    [Authorize(Roles = "signatory,admin")]
    public async Task<IActionResult> ApproveOrg(int id, [FromBody] ApproveRequest req)
    {
        var co = await _db.ClearanceOrganizations
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (co == null) return NotFound();

        var callerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != "admin")
        {
            var signatory = await _db.Signatories.FirstOrDefaultAsync(s => s.UserId == callerId);
            if (signatory == null || co.Organization!.SignatoryId != signatory.Id)
                return Forbid();
        }

        co.StatusId = req.StatusId;
        co.Remarks = req.Remarks;
        co.SignedAt = req.StatusId == 2 ? DateTime.UtcNow : null;
        co.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated." });
    }

    // ─── BULK approve all subjects for instructor ────────────────────────
    // POST /api/clearance/subjects/bulk-approve
    [HttpPost("subjects/bulk-approve")]
    [Authorize(Roles = "signatory,admin")]
    public async Task<IActionResult> BulkApproveSubjects([FromBody] BulkApproveRequest req)
    {
        var callerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var signatory = await _db.Signatories.FirstOrDefaultAsync(s => s.UserId == callerId);

        var query = _db.ClearanceSubjects
            .Include(cs => cs.SubjectOffering)
            .Where(cs => cs.PeriodId == req.PeriodId && cs.StatusId == 1);

        if (signatory != null)
            query = query.Where(cs => cs.SubjectOffering!.InstructorId == signatory.Id);

        var items = await query.ToListAsync();
        foreach (var item in items)
        {
            item.StatusId = 2;
            item.SignedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Bulk approved {items.Count} items." });
    }
}

public record BulkApproveRequest(int PeriodId);
