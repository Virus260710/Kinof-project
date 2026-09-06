using System.Security.Claims;

namespace Kinof.Api.Services;

public static class BangkokTime
{
    public static readonly TimeZoneInfo Zone = Resolve();

    public static DateTime ToLocal(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
    }

    public static DateTime ToUtc(DateTime localUnspecified)
    {
        var unspecified = DateTime.SpecifyKind(localUnspecified, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, Zone);
    }

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Asia/Bangkok", "SE Asia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Asia/Bangkok", TimeSpan.FromHours(7), "Asia/Bangkok", "Asia/Bangkok");
    }
}

public static class StaffAuth
{
    public static bool IsStaff(ClaimsPrincipal user) =>
        user.IsInRole("admin") || user.IsInRole("superadmin");

    public static bool IsSuperAdmin(ClaimsPrincipal user) =>
        user.IsInRole("superadmin");
}

public static class StudentIds
{
    public static bool IsValid(string? studentId) =>
        !string.IsNullOrWhiteSpace(studentId) &&
        studentId.Length == 10 &&
        studentId.All(char.IsDigit);

    public static string? Normalize(string? studentId)
    {
        var value = studentId?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

public static class ScheduleTimes
{
    public static bool TryParse(string? value, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().Replace('.', ':');
        if (TimeOnly.TryParse(normalized, out time))
            return true;
        if (TimeSpan.TryParse(normalized, out var span) && span >= TimeSpan.Zero && span < TimeSpan.FromDays(1))
        {
            time = TimeOnly.FromTimeSpan(span);
            return true;
        }

        return false;
    }

    public static bool Overlaps(TimeOnly startA, TimeOnly endA, TimeOnly startB, TimeOnly endB) =>
        startA < endB && endA > startB;
}
