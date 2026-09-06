using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Kinof.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<FaceEmbedding> FaceEmbeddings => Set<FaceEmbedding>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();
    public DbSet<EntryOtp> EntryOtps => Set<EntryOtp>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ScheduleEnrollment> ScheduleEnrollments => Set<ScheduleEnrollment>();
    public DbSet<ScheduleEnrollmentPending> ScheduleEnrollmentPendings => Set<ScheduleEnrollmentPending>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingGroup> BookingGroups => Set<BookingGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ProblemReport> ProblemReports => Set<ProblemReport>();
    public DbSet<ProblemReportImage> ProblemReportImages => Set<ProblemReportImage>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();
    public DbSet<AgentLog> AgentLogs => Set<AgentLog>();
    public DbSet<WebsiteBlacklist> WebsiteBlacklist => Set<WebsiteBlacklist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.StudentId);
            entity.HasIndex(x => x.UserType);
            entity.Property(x => x.Username).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.StudentId).HasMaxLength(20);
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.Phone).HasMaxLength(20);
            entity.Property(x => x.JobTitle).HasMaxLength(100);
            entity.Property(x => x.UserType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<FaceEmbedding>(entity =>
        {
            entity.ToTable("face_embeddings");
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasOne<User>().WithOne().HasForeignKey<FaceEmbedding>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.ToTable("email_otps");
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EntryOtp>(entity =>
        {
            entity.ToTable("entry_otps");
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("seats");
            entity.HasIndex(x => new { x.RoomId, x.SeatNumber }).IsUnique();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.ToTable("agents");
            entity.HasIndex(x => x.SeatId).IsUnique();
            entity.HasOne<Seat>().WithOne().HasForeignKey<Agent>(x => x.SeatId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.ToTable("schedules");
            entity.Property(x => x.CourseCode).HasMaxLength(50);
            entity.Property(x => x.CourseName).HasMaxLength(255);
            entity.Property(x => x.Section).HasMaxLength(50);
            entity.Property(x => x.InstructorName).HasMaxLength(255);
            entity.Property(x => x.AcademicYear).HasMaxLength(20);
            entity.Property(x => x.Semester).HasMaxLength(20);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasIndex(x => new { x.RoomId, x.DayOfWeek, x.IsActive });
            entity.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScheduleEnrollment>(entity =>
        {
            entity.ToTable("schedule_enrollments");
            entity.HasIndex(x => new { x.ScheduleId, x.UserId }).IsUnique();
            entity.HasOne<Schedule>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScheduleEnrollmentPending>(entity =>
        {
            entity.ToTable("schedule_enrollment_pending");
            entity.Property(x => x.StudentId).HasMaxLength(20);
            entity.HasIndex(x => new { x.ScheduleId, x.StudentId }).IsUnique();
            entity.HasIndex(x => x.StudentId);
            entity.HasOne<Schedule>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.ToTable("admin_audit_logs");
            entity.Property(x => x.Action).HasMaxLength(80);
            entity.Property(x => x.TargetType).HasMaxLength(50);
            entity.Property(x => x.TargetId).HasMaxLength(80);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.Action);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Seat>().WithMany().HasForeignKey(x => x.SeatId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BookingGroup>(entity =>
        {
            entity.ToTable("booking_groups");
            entity.HasIndex(x => x.BookingId).IsUnique();
            entity.HasOne<Booking>().WithOne().HasForeignKey<BookingGroup>(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.ToTable("group_members");
            entity.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
            entity.HasOne<BookingGroup>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.ToTable("invitations");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.GroupId, x.InviteeUserId }).IsUnique();
            entity.HasOne<BookingGroup>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.InviterUserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.InviteeUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.Property(x => x.Message).HasMaxLength(500);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Invitation>().WithMany().HasForeignKey(x => x.InvitationId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProblemReport>(entity =>
        {
            entity.ToTable("problem_reports");
            entity.Property(x => x.Category).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(5000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProblemReportImage>(entity =>
        {
            entity.ToTable("problem_report_images");
            entity.Property(x => x.OriginalFileName).HasMaxLength(255);
            entity.Property(x => x.StoredFileName).HasMaxLength(255);
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.HasIndex(x => x.ProblemReportId);
            entity.HasOne<ProblemReport>().WithMany().HasForeignKey(x => x.ProblemReportId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccessLog>(entity =>
        {
            entity.ToTable("access_logs");
            entity.Property(x => x.AuthMethod).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.AuthResult).HasConversion<string>().HasMaxLength(20);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Seat>().WithMany().HasForeignKey(x => x.SeatId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentLog>(entity =>
        {
            entity.ToTable("agent_logs");
            entity.HasOne<Agent>().WithMany().HasForeignKey(x => x.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WebsiteBlacklist>(entity =>
        {
            entity.ToTable("website_blacklist");
            entity.HasIndex(x => x.UrlPattern).IsUnique();
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
        }
    }

    private static string ToSnakeCase(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0)
                result.Append('_');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}
