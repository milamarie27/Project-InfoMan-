using Microsoft.EntityFrameworkCore;
using OnlineClearance.API.Models;

namespace OnlineClearance.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User>                 Users                 => Set<User>();
    public DbSet<Student>              Students              => Set<Student>();
    public DbSet<Signatory>            Signatories           => Set<Signatory>();
    public DbSet<Course>               Courses               => Set<Course>();
    public DbSet<Curriculum>           Curricula             => Set<Curriculum>();
    public DbSet<AcademicPeriod>       AcademicPeriods       => Set<AcademicPeriod>();
    public DbSet<Subject>              Subjects              => Set<Subject>();
    public DbSet<SubjectOffering>      SubjectOfferings      => Set<SubjectOffering>();
    public DbSet<Organization>         Organizations         => Set<Organization>();
    public DbSet<StatusTable>          StatusTable           => Set<StatusTable>();
    public DbSet<ClearanceSubject>     ClearanceSubjects     => Set<ClearanceSubject>();
    public DbSet<ClearanceOrganization>ClearanceOrganizations=> Set<ClearanceOrganization>();
    public DbSet<Announcement>         Announcements         => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Unique indexes ─────────────────────────────────────────────────
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.StudentNumber).IsUnique();
        modelBuilder.Entity<Signatory>()
            .HasIndex(s => s.EmployeeId).IsUnique();
        modelBuilder.Entity<Course>()
            .HasIndex(c => c.CourseCode).IsUnique();
        modelBuilder.Entity<SubjectOffering>()
            .HasIndex(so => so.MisCode).IsUnique();
        modelBuilder.Entity<Organization>()
            .HasIndex(o => new { o.OrgName, o.PositionTitle }).IsUnique();

        // ── Composite unique on clearance tables ───────────────────────────
        modelBuilder.Entity<ClearanceSubject>()
            .HasIndex(cs => new { cs.StudentId, cs.SubjectOfferingId, cs.PeriodId })
            .IsUnique();
        modelBuilder.Entity<ClearanceOrganization>()
            .HasIndex(co => new { co.StudentId, co.OrganizationId, co.PeriodId })
            .IsUnique();

        // ── Relationships — no cascade loops ──────────────────────────────
        modelBuilder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Signatory>()
            .HasOne(s => s.User)
            .WithOne(u => u.Signatory)
            .HasForeignKey<Signatory>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClearanceSubject>()
            .HasOne(cs => cs.Student)
            .WithMany(s => s.ClearanceSubjects)
            .HasForeignKey(cs => cs.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClearanceSubject>()
            .HasOne(cs => cs.SubjectOffering)
            .WithMany(so => so.ClearanceSubjects)
            .HasForeignKey(cs => cs.SubjectOfferingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClearanceSubject>()
            .HasOne(cs => cs.Status)
            .WithMany()
            .HasForeignKey(cs => cs.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClearanceSubject>()
            .HasOne(cs => cs.Period)
            .WithMany(p => p.ClearanceSubjects)
            .HasForeignKey(cs => cs.PeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClearanceOrganization>()
            .HasOne(co => co.Student)
            .WithMany(s => s.ClearanceOrganizations)
            .HasForeignKey(co => co.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClearanceOrganization>()
            .HasOne(co => co.Organization)
            .WithMany(o => o.ClearanceOrganizations)
            .HasForeignKey(co => co.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClearanceOrganization>()
            .HasOne(co => co.Status)
            .WithMany()
            .HasForeignKey(co => co.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClearanceOrganization>()
            .HasOne(co => co.Period)
            .WithMany(p => p.ClearanceOrganizations)
            .HasForeignKey(co => co.PeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Organization>()
            .HasOne(o => o.Signatory)
            .WithMany(s => s.Organizations)
            .HasForeignKey(o => o.SignatoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Organization>()
            .HasOne(o => o.Curriculum)
            .WithMany(c => c.Organizations)
            .HasForeignKey(o => o.CurriculumId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SubjectOffering>()
            .HasOne(so => so.Instructor)
            .WithMany(s => s.SubjectOfferings)
            .HasForeignKey(so => so.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.Author)
            .WithMany(u => u.Announcements)
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
