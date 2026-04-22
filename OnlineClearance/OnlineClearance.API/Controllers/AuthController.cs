using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineClearance.API.Data;
using OnlineClearance.API.DTOs;
using OnlineClearance.API.Helpers;
using OnlineClearance.API.Models;

namespace OnlineClearance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;

    public AuthController(AppDbContext db, JwtHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users
            .Include(u => u.Student)
            .Include(u => u.Signatory)
            .FirstOrDefaultAsync(u => u.Username == req.Username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password))
            return Unauthorized(new { message = "Invalid username or password." });

        var studentNumber = user.Student?.StudentNumber;
        var employeeId = user.Signatory?.EmployeeId;
        var token = _jwt.GenerateToken(user, studentNumber, employeeId);

        return Ok(new LoginResponse(
            Token: token,
            Role: user.Role,
            FullName: $"{user.FirstName} {user.LastName}",
            UserId: user.Id,
            StudentNumber: studentNumber,
            EmployeeId: employeeId
        ));
    }

    // POST /api/auth/register/student
    [HttpPost("register/student")]
    public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return Conflict(new { message = "Username already taken." });

        if (await _db.Students.AnyAsync(s => s.StudentNumber == req.StudentNumber))
            return Conflict(new { message = "Student number already registered." });

        if (!await _db.Curricula.AnyAsync(c => c.Id == req.CurriculumId))
            return BadRequest(new { message = "Invalid curriculum." });

        var user = new User
        {
            Username = req.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName = req.FirstName,
            LastName = req.LastName,
            MiddleInitial = req.MiddleInitial,
            SuffixName = req.SuffixName,
            Role = "student"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var student = new Student
        {
            UserId = user.Id,
            StudentNumber = req.StudentNumber,
            CurriculumId = req.CurriculumId,
            Status = req.Status
        };

        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Student registered successfully.", userId = user.Id });
    }

    // POST /api/auth/register/signatory
    [HttpPost("register/signatory")]
    public async Task<IActionResult> RegisterSignatory([FromBody] RegisterSignatoryRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return Conflict(new { message = "Username already taken." });

        if (await _db.Signatories.AnyAsync(s => s.EmployeeId == req.EmployeeId))
            return Conflict(new { message = "Employee ID already registered." });

        var user = new User
        {
            Username = req.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName = req.FirstName,
            LastName = req.LastName,
            MiddleInitial = req.MiddleInitial,
            Role = "signatory"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var signatory = new Signatory
        {
            UserId = user.Id,
            EmployeeId = req.EmployeeId
        };

        _db.Signatories.Add(signatory);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Signatory registered successfully.", userId = user.Id });
    }
}
