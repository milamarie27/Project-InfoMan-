namespace OnlineClearance.API.DTOs;

// ─── AUTH ────────────────────────────────────
public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string Token,
    string Role,
    string FullName,
    int UserId,
    string? StudentNumber,
    string? EmployeeId
);

// ─── USER ────────────────────────────────────
public record RegisterStudentRequest(
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string? MiddleInitial,
    string? SuffixName,
    string StudentNumber,
    int CurriculumId,
    string Status
);

public record RegisterSignatoryRequest(
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string? MiddleInitial,
    string EmployeeId
);

public record UserDto(
    int Id,
    string Username,
    string FirstName,
    string LastName,
    string? MiddleInitial,
    string? SuffixName,
    string Role,
    bool IsActive,
    string? ESignaturePath
);

// ─── STUDENT ─────────────────────────────────
public record StudentDto(
    int Id,
    string StudentNumber,
    string FullName,
    string CourseCode,
    string CourseDescription,
    int YearLevel,
    string Section,
    string Status,
    string Username
);

// ─── COURSE / CURRICULUM ─────────────────────
public record CourseDto(int Id, string CourseCode, string? Description);

public record CurriculumDto(
    int Id,
    int CourseId,
    string CourseCode,
    string? CourseDescription,
    int YearLevel,
    string Section
);

public record CreateCurriculumRequest(int CourseId, int YearLevel, string Section);
public record CreateCourseRequest(string CourseCode, string? Description);

// ─── ACADEMIC PERIOD ─────────────────────────
public record AcademicPeriodDto(int Id, string AcademicYear, string Semester, bool IsActive);
public record CreatePeriodRequest(string AcademicYear, string Semester);

// ─── SUBJECT ─────────────────────────────────
public record SubjectDto(int Id, string SubjectCode, string Title, int LecUnits, int LabUnits);
public record CreateSubjectRequest(string SubjectCode, string Title, int LecUnits, int LabUnits);

// ─── SUBJECT OFFERING ────────────────────────
public record SubjectOfferingDto(
    int Id,
    string MisCode,
    string SubjectCode,
    string SubjectTitle,
    int InstructorId,
    string InstructorName,
    string EmployeeId,
    int PeriodId,
    string AcademicYear,
    string Semester
);

public record CreateSubjectOfferingRequest(
    string MisCode,
    string SubjectCode,
    int InstructorId,
    int PeriodId
);

// ─── ORGANIZATION ────────────────────────────
public record OrganizationDto(
    int Id,
    string OrgName,
    int SignatoryId,
    string SignatoryName,
    string PositionTitle,
    int CurriculumId,
    string CourseCode,
    int YearLevel,
    string Section
);

public record CreateOrganizationRequest(
    string OrgName,
    int SignatoryId,
    string PositionTitle,
    int CurriculumId
);

// ─── CLEARANCE ───────────────────────────────
public record ClearanceSubjectDto(
    int Id,
    string StudentNumber,
    string StudentName,
    string MisCode,
    string SubjectCode,
    string SubjectTitle,
    string InstructorName,
    string StatusLabel,
    int StatusId,
    string? Remarks,
    DateTime? SignedAt,
    DateTime CreatedAt
);

public record ClearanceOrgDto(
    int Id,
    string StudentNumber,
    string StudentName,
    string OrgName,
    string SignatoryName,
    string PositionTitle,
    string StatusLabel,
    int StatusId,
    string? Remarks,
    DateTime? SignedAt,
    DateTime CreatedAt
);

public record ApproveRequest(int StatusId, string? Remarks);

public record StudentClearanceSummaryDto(
    string StudentNumber,
    string StudentName,
    string CourseCode,
    int YearLevel,
    string Section,
    int TotalSubjectItems,
    int ClearedSubjectItems,
    int TotalOrgItems,
    int ClearedOrgItems,
    bool IsFullyCleared,
    string AcademicYear,
    string Semester
);

public record GenerateClearanceRequest(int StudentId, int PeriodId);

// ─── ANNOUNCEMENTS ───────────────────────────
public record AnnouncementDto(
    int Id,
    string Title,
    string Content,
    string AuthorName,
    string Type,
    DateTime CreatedAt
);

public record CreateAnnouncementRequest(string Title, string Content, string Type);

// ─── SIGNATORY ───────────────────────────────
public record SignatoryDto(
    int Id,
    string EmployeeId,
    string FullName,
    string Username,
    string? SignaturePath,
    bool IsActive
);

// ─── REPORT ──────────────────────────────────
public record ClearanceReportRow(
    string StudentNumber,
    string FullName,
    string Course,
    int YearLevel,
    string Section,
    string AcademicYear,
    string Semester,
    int ClearedSubjects,
    int TotalSubjects,
    int ClearedOrgs,
    int TotalOrgs,
    string OverallStatus
);
