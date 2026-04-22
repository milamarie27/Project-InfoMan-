using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineClearance.API.Data;
using OnlineClearance.API.DTOs;

namespace OnlineClearance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    // GET /api/reports/clearance?periodId=
    [HttpGet("clearance")]
    [Authorize(Roles = "admin,signatory")]
    public async Task<IActionResult> GetClearanceReport([FromQuery] int? periodId)
    {
        var clearedId = (await _db.StatusTable.FirstOrDefaultAsync(s => s.Label == "Cleared"))?.Id ?? 2;

        var students = await _db.Students
            .Include(s => s.User)
            .Include(s => s.Curriculum).ThenInclude(c => c!.Course)
            .ToListAsync();

        var period = periodId.HasValue
            ? await _db.AcademicPeriods.FindAsync(periodId)
            : await _db.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);

        if (period == null) return BadRequest(new { message = "No active period found." });

        var rows = new List<ClearanceReportRow>();

        foreach (var s in students)
        {
            var subTotal = await _db.ClearanceSubjects.CountAsync(cs => cs.StudentId == s.Id && cs.PeriodId == period.Id);
            var subCleared = await _db.ClearanceSubjects.CountAsync(cs => cs.StudentId == s.Id && cs.PeriodId == period.Id && cs.StatusId == clearedId);
            var orgTotal = await _db.ClearanceOrganizations.CountAsync(co => co.StudentId == s.Id && co.PeriodId == period.Id);
            var orgCleared = await _db.ClearanceOrganizations.CountAsync(co => co.StudentId == s.Id && co.PeriodId == period.Id && co.StatusId == clearedId);

            if (subTotal == 0 && orgTotal == 0) continue;

            rows.Add(new ClearanceReportRow(
                s.StudentNumber,
                $"{s.User!.FirstName} {s.User.LastName}",
                s.Curriculum!.Course!.CourseCode,
                s.Curriculum.YearLevel,
                s.Curriculum.Section,
                period.AcademicYear,
                period.Semester,
                subCleared, subTotal,
                orgCleared, orgTotal,
                subTotal > 0 && subCleared == subTotal && orgCleared == orgTotal ? "Cleared" : "Pending"
            ));
        }

        return Ok(rows);
    }

    // GET /api/reports/cleared?periodId=
    [HttpGet("cleared")]
    public async Task<IActionResult> GetClearedStudents([FromQuery] int? periodId)
    {
        var period = periodId.HasValue
            ? await _db.AcademicPeriods.FindAsync(periodId)
            : await _db.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);
        if (period == null) return BadRequest(new { message = "No period found." });

        var clearedId = (await _db.StatusTable.FirstOrDefaultAsync(s => s.Label == "Cleared"))?.Id ?? 2;

        // Students where ALL their clearance items in the period are cleared
        var allStudentIds = await _db.Students.Select(s => s.Id).ToListAsync();
        var fullyCleared = new List<int>();

        foreach (var id in allStudentIds)
        {
            var subTotal = await _db.ClearanceSubjects.CountAsync(cs => cs.StudentId == id && cs.PeriodId == period.Id);
            var subCleared = await _db.ClearanceSubjects.CountAsync(cs => cs.StudentId == id && cs.PeriodId == period.Id && cs.StatusId == clearedId);
            var orgTotal = await _db.ClearanceOrganizations.CountAsync(co => co.StudentId == id && co.PeriodId == period.Id);
            var orgCleared = await _db.ClearanceOrganizations.CountAsync(co => co.StudentId == id && co.PeriodId == period.Id && co.StatusId == clearedId);

            if (subTotal > 0 && subCleared == subTotal && orgCleared == orgTotal)
                fullyCleared.Add(id);
        }

        var result = await _db.Students
            .Include(s => s.User)
            .Include(s => s.Curriculum).ThenInclude(c => c!.Course)
            .Where(s => fullyCleared.Contains(s.Id))
            .Select(s => new StudentDto(
                s.Id,
                s.StudentNumber,
                $"{s.User!.FirstName} {s.User.LastName}",
                s.Curriculum!.Course!.CourseCode,
                s.Curriculum.Course.Description ?? "",
                s.Curriculum.YearLevel,
                s.Curriculum.Section,
                s.Status,
                s.User.Username
            )).ToListAsync();

        return Ok(result);
    }

    // GET /api/reports/pending?periodId=
    [HttpGet("pending")]
    [Authorize(Roles = "admin,signatory")]
    public async Task<IActionResult> GetPendingStudents([FromQuery] int? periodId)
    {
        var period = periodId.HasValue
            ? await _db.AcademicPeriods.FindAsync(periodId)
            : await _db.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);
        if (period == null) return BadRequest(new { message = "No period found." });

        var clearedId = (await _db.StatusTable.FirstOrDefaultAsync(s => s.Label == "Cleared"))?.Id ?? 2;

        var pendingIds = await _db.ClearanceSubjects
            .Where(cs => cs.PeriodId == period.Id && cs.StatusId != clearedId)
            .Select(cs => cs.StudentId)
            .Union(_db.ClearanceOrganizations
                .Where(co => co.PeriodId == period.Id && co.StatusId != clearedId)
                .Select(co => co.StudentId))
            .Distinct()
            .ToListAsync();

        var result = await _db.Students
            .Include(s => s.User)
            .Include(s => s.Curriculum).ThenInclude(c => c!.Course)
            .Where(s => pendingIds.Contains(s.Id))
            .Select(s => new StudentDto(
                s.Id,
                s.StudentNumber,
                $"{s.User!.FirstName} {s.User.LastName}",
                s.Curriculum!.Course!.CourseCode,
                s.Curriculum.Course.Description ?? "",
                s.Curriculum.YearLevel,
                s.Curriculum.Section,
                s.Status,
                s.User.Username
            )).ToListAsync();

        return Ok(result);
    }

    // GET /api/reports/export?periodId=
    [HttpGet("export")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ExportExcel([FromQuery] int? periodId)
    {
        var period = periodId.HasValue
            ? await _db.AcademicPeriods.FindAsync(periodId)
            : await _db.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);
        if (period == null) return BadRequest();

        var clearedId = (await _db.StatusTable.FirstOrDefaultAsync(s => s.Label == "Cleared"))?.Id ?? 2;
        var students = await _db.Students
            .Include(s => s.User)
            .Include(s => s.Curriculum).ThenInclude(c => c!.Course)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Clearance Report");

        // Header
        ws.Cell(1, 1).Value = $"Clearance Report - {period.AcademicYear} {period.Semester}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 10).Merge();

        string[] headers = ["Student No.", "Full Name", "Course", "Year", "Section",
            "Subj Cleared", "Subj Total", "Org Cleared", "Org Total", "Status"];
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
            ws.Cell(2, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
            ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 3;
        foreach (var s in students)
        {
            var sc = await _db.ClearanceSubjects.CountAsync(x => x.StudentId == s.Id && x.PeriodId == period.Id);
            var scc = await _db.ClearanceSubjects.CountAsync(x => x.StudentId == s.Id && x.PeriodId == period.Id && x.StatusId == clearedId);
            var oc = await _db.ClearanceOrganizations.CountAsync(x => x.StudentId == s.Id && x.PeriodId == period.Id);
            var occ = await _db.ClearanceOrganizations.CountAsync(x => x.StudentId == s.Id && x.PeriodId == period.Id && x.StatusId == clearedId);

            if (sc == 0 && oc == 0) continue;

            var isCleared = sc > 0 && scc == sc && occ == oc;
            ws.Cell(row, 1).Value = s.StudentNumber;
            ws.Cell(row, 2).Value = $"{s.User!.FirstName} {s.User.LastName}";
            ws.Cell(row, 3).Value = s.Curriculum!.Course!.CourseCode;
            ws.Cell(row, 4).Value = s.Curriculum.YearLevel;
            ws.Cell(row, 5).Value = s.Curriculum.Section;
            ws.Cell(row, 6).Value = scc;
            ws.Cell(row, 7).Value = sc;
            ws.Cell(row, 8).Value = occ;
            ws.Cell(row, 9).Value = oc;
            ws.Cell(row, 10).Value = isCleared ? "CLEARED" : "PENDING";

            var statusCell = ws.Cell(row, 10);
            statusCell.Style.Fill.BackgroundColor = isCleared ? XLColor.FromHtml("#dcfce7") : XLColor.FromHtml("#fef9c3");
            statusCell.Style.Font.FontColor = isCleared ? XLColor.FromHtml("#166534") : XLColor.FromHtml("#854d0e");

            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"clearance_{period.AcademicYear}_{period.Semester}.xlsx");
    }
}
