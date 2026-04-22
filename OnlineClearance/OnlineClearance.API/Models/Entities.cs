using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineClearance.API.Models;

// ─────────────────────────────────────────────
//  USERS
// ─────────────────────────────────────────────
[Table("users")]
public class User
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("username")][Required][MaxLength(100)] public string Username { get; set; } = "";
    [Column("password")][Required] public string Password { get; set; } = "";
    [Column("first_name")][Required][MaxLength(100)] public string FirstName { get; set; } = "";
    [Column("last_name")][Required][MaxLength(100)] public string LastName { get; set; } = "";
    [Column("middle_initial")][MaxLength(5)] public string? MiddleInitial { get; set; }
    [Column("suffix_name")][MaxLength(20)] public string? SuffixName { get; set; }
    [Column("e_signature_path")] public string? ESignaturePath { get; set; }
    [Column("role")][MaxLength(20)] public string Role { get; set; } = "student"; // student | signatory | admin
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Nav
    public Student? Student { get; set; }
    public Signatory? Signatory { get; set; }
    public ICollection<Announcement> Announcements { get; set; } = [];
}

// ─────────────────────────────────────────────
//  COURSES
// ─────────────────────────────────────────────
[Table("courses")]
public class Course
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("course_code")][Required][MaxLength(20)] public string CourseCode { get; set; } = "";
    [Column("description")][MaxLength(200)] public string? Description { get; set; }

    public ICollection<Curriculum> Curricula { get; set; } = [];
}

// ─────────────────────────────────────────────
//  CURRICULUM
// ─────────────────────────────────────────────
[Table("curriculum")]
public class Curriculum
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("course_id")] public int CourseId { get; set; }
    [Column("year_level")] public int YearLevel { get; set; }
    [Column("section")][MaxLength(20)] public string Section { get; set; } = "";

    [ForeignKey("CourseId")] public Course? Course { get; set; }
    public ICollection<Student> Students { get; set; } = [];
    public ICollection<Organization> Organizations { get; set; } = [];
}

// ─────────────────────────────────────────────
//  STUDENTS
// ─────────────────────────────────────────────
[Table("students")]
public class Student
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("student_number")][Required][MaxLength(20)] public string StudentNumber { get; set; } = "";
    [Column("curriculum_id")] public int CurriculumId { get; set; }
    [Column("status")][MaxLength(20)] public string Status { get; set; } = "Regular"; // Regular | Irregular

    [ForeignKey("UserId")] public User? User { get; set; }
    [ForeignKey("CurriculumId")] public Curriculum? Curriculum { get; set; }
    public ICollection<ClearanceSubject> ClearanceSubjects { get; set; } = [];
    public ICollection<ClearanceOrganization> ClearanceOrganizations { get; set; } = [];
}

// ─────────────────────────────────────────────
//  SIGNATORIES
// ─────────────────────────────────────────────
[Table("signatories")]
public class Signatory
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("employee_id")][Required][MaxLength(30)] public string EmployeeId { get; set; } = "";
    [Column("uploaded_signature_path")] public string? UploadedSignaturePath { get; set; }

    [ForeignKey("UserId")] public User? User { get; set; }
    public ICollection<SubjectOffering> SubjectOfferings { get; set; } = [];
    public ICollection<Organization> Organizations { get; set; } = [];
}

// ─────────────────────────────────────────────
//  ACADEMIC PERIODS
// ─────────────────────────────────────────────
[Table("academic_periods")]
public class AcademicPeriod
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("academic_year")][MaxLength(20)] public string AcademicYear { get; set; } = "";
    [Column("semester")][MaxLength(30)] public string Semester { get; set; } = "";
    [Column("is_active")] public bool IsActive { get; set; } = false;

    public ICollection<SubjectOffering> SubjectOfferings { get; set; } = [];
    public ICollection<ClearanceSubject> ClearanceSubjects { get; set; } = [];
    public ICollection<ClearanceOrganization> ClearanceOrganizations { get; set; } = [];
}

// ─────────────────────────────────────────────
//  SUBJECTS
// ─────────────────────────────────────────────
[Table("subjects")]
public class Subject
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("subject_code")][Required][MaxLength(30)] public string SubjectCode { get; set; } = "";
    [Column("title")][MaxLength(200)] public string Title { get; set; } = "";
    [Column("lec_units")] public int LecUnits { get; set; } = 0;
    [Column("lab_units")] public int LabUnits { get; set; } = 0;
}

// ─────────────────────────────────────────────
//  SUBJECT OFFERINGS
// ─────────────────────────────────────────────
[Table("subject_offerings")]
public class SubjectOffering
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("mis_code")][Required][MaxLength(30)] public string MisCode { get; set; } = "";
    [Column("subject_code")][MaxLength(30)] public string SubjectCode { get; set; } = "";
    [Column("instructor_id")] public int InstructorId { get; set; } // FK to signatories.id
    [Column("period_id")] public int PeriodId { get; set; }

    [ForeignKey("InstructorId")] public Signatory? Instructor { get; set; }
    [ForeignKey("PeriodId")] public AcademicPeriod? Period { get; set; }
    public ICollection<ClearanceSubject> ClearanceSubjects { get; set; } = [];
}

// ─────────────────────────────────────────────
//  ORGANIZATIONS
// ─────────────────────────────────────────────
[Table("organizations")]
public class Organization
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("org_name")][MaxLength(200)] public string OrgName { get; set; } = "";
    [Column("signatory_id")] public int SignatoryId { get; set; }
    [Column("position_title")][MaxLength(100)] public string PositionTitle { get; set; } = "";
    [Column("curriculum_id")] public int CurriculumId { get; set; }

    [ForeignKey("SignatoryId")] public Signatory? Signatory { get; set; }
    [ForeignKey("CurriculumId")] public Curriculum? Curriculum { get; set; }
    public ICollection<ClearanceOrganization> ClearanceOrganizations { get; set; } = [];
}

// ─────────────────────────────────────────────
//  STATUS TABLE
// ─────────────────────────────────────────────
[Table("status_table")]
public class StatusTable
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("label")][MaxLength(50)] public string Label { get; set; } = "";
}

// ─────────────────────────────────────────────
//  CLEARANCE SUBJECTS
// ─────────────────────────────────────────────
[Table("clearance_subjects")]
public class ClearanceSubject
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("student_id")] public int StudentId { get; set; }
    [Column("subject_offering_id")] public int SubjectOfferingId { get; set; }
    [Column("status_id")] public int StatusId { get; set; }
    [Column("remarks")] public string? Remarks { get; set; }
    [Column("period_id")] public int PeriodId { get; set; }
    [Column("signed_at")] public DateTime? SignedAt { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("StudentId")] public Student? Student { get; set; }
    [ForeignKey("SubjectOfferingId")] public SubjectOffering? SubjectOffering { get; set; }
    [ForeignKey("StatusId")] public StatusTable? Status { get; set; }
    [ForeignKey("PeriodId")] public AcademicPeriod? Period { get; set; }
}

// ─────────────────────────────────────────────
//  CLEARANCE ORGANIZATIONS
// ─────────────────────────────────────────────
[Table("clearance_organization")]
public class ClearanceOrganization
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("student_id")] public int StudentId { get; set; }
    [Column("organization_id")] public int OrganizationId { get; set; }
    [Column("period_id")] public int PeriodId { get; set; }
    [Column("status_id")] public int StatusId { get; set; }
    [Column("remarks")] public string? Remarks { get; set; }
    [Column("signed_at")] public DateTime? SignedAt { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("StudentId")] public Student? Student { get; set; }
    [ForeignKey("OrganizationId")] public Organization? Organization { get; set; }
    [ForeignKey("StatusId")] public StatusTable? Status { get; set; }
    [ForeignKey("PeriodId")] public AcademicPeriod? Period { get; set; }
}

// ─────────────────────────────────────────────
//  ANNOUNCEMENTS
// ─────────────────────────────────────────────
[Table("announcements")]
public class Announcement
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("title")][Required][MaxLength(200)] public string Title { get; set; } = "";
    [Column("content")][Required] public string Content { get; set; } = "";
    [Column("author_id")] public int AuthorId { get; set; }
    [Column("type")][MaxLength(20)] public string Type { get; set; } = "General";
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("AuthorId")] public User? Author { get; set; }
}
