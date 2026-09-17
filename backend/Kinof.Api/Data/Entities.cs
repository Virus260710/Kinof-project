namespace Kinof.Api.Data;

public enum UserType { Student, Staff, External, Admin, SuperAdmin }
public enum UserStatus { Active, Disabled }
public enum RoomStatus { Open, Closed, Maintenance }
public enum SeatStatus { Available, Occupied, Offline }
public enum BookingStatus { Confirmed, Cancelled, Completed, Expired, Pending }
public enum InvitationStatus { Pending, Accepted, Declined }
public enum ProblemReportStatus { Pending, InProgress, Resolved }
public enum AuthMethod { Face, OtpFallback }
public enum AuthResult { Granted, Denied }
public enum BehaviorReviewStatus { Pending, Cleared, Penalized }

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? StudentId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public UserType UserType { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool FaceEnrolled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FaceEmbedding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Embedding { get; set; }
    public bool IsPrimary { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class EmailOtp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string CodeHash { get; set; }
    public string Purpose { get; set; } = "login";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class EntryOtp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? RoomId { get; set; }
    public required string CodeHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Building { get; set; }
    public int Capacity { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Seat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public int SeatNumber { get; set; }
    public string? ComputerName { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;
}

public sealed class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SeatId { get; set; }
    public required string ApiKey { get; set; }
    public string? Hostname { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class KioskDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public required string ApiKey { get; set; }
    public string? Label { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Schedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public required string CourseCode { get; set; }
    public string? CourseName { get; set; }
    public string? Section { get; set; }
    public string? InstructorName { get; set; }
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? AcademicYear { get; set; }
    public string? Semester { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ScheduleEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScheduleId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class ScheduleEnrollmentPending
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScheduleId { get; set; }
    public required string StudentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AdminAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid RoomId { get; set; }
    public Guid? SeatId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class BookingGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class GroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Invitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid InviterUserId { get; set; }
    public Guid InviteeUserId { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Message { get; set; }
    public Guid? InvitationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

public sealed class ProblemReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Category { get; set; }
    public required string Description { get; set; }
    public ProblemReportStatus Status { get; set; } = ProblemReportStatus.Pending;
    public string? StaffNote { get; set; }
    /// <summary>Optional admin response shown to the user when a status change is made.</summary>
    public string? AdminComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ProblemReportImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProblemReportId { get; set; }
    public required string StoredFileName { get; set; }
    public required string OriginalFileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AccessLog
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoomId { get; set; }
    public Guid? SeatId { get; set; }
    public AuthMethod AuthMethod { get; set; }
    public AuthResult AuthResult { get; set; }
    public string? DenyReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AgentLog
{
    public long Id { get; set; }
    public Guid AgentId { get; set; }
    public required string EventType { get; set; }
    public string? DataJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class WebsiteBlacklist
{
    public int Id { get; set; }
    public required string UrlPattern { get; set; }
    public required string Category { get; set; }
    public string Source { get; set; } = "manual";
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ProgramBlacklist
{
    public int Id { get; set; }
    public required string ProcessName { get; set; }
    public required string Category { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ProgramAllowlist
{
    public int Id { get; set; }
    public required string ProcessName { get; set; }
    public string? DisplayName { get; set; }
    public required string Category { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class BehaviorPenalty
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int Points { get; set; }
    public required string Reason { get; set; }
    public required string Source { get; set; }
    public required string SourceKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class BehaviorReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Username { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? SeatId { get; set; }
    public string? RoomName { get; set; }
    public string? SeatLabel { get; set; }
    public required string Kind { get; set; }
    public required string Target { get; set; }
    public required string Activity { get; set; }
    public required string QueueKey { get; set; }
    public int OccurrenceCount { get; set; } = 1;
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public BehaviorReviewStatus Status { get; set; } = BehaviorReviewStatus.Pending;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
