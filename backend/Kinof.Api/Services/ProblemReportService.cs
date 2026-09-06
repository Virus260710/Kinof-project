using System.Security.Claims;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed class ProblemReportService(AppDbContext db, IWebHostEnvironment environment)
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const int MaxImages = 3;

    public async Task<IResult> CreateAsync(
        Guid userId,
        string? category,
        string? description,
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken)
    {
        category = category?.Trim();
        description = description?.Trim();
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(description))
            return Results.BadRequest(new { message = "กรุณาเลือกหัวข้อและระบุรายละเอียดปัญหา" });
        if (category.Length > 100 || description.Length > 5000)
            return Results.BadRequest(new { message = "ข้อมูลปัญหายาวเกินกำหนด" });
        if (files.Count > MaxImages)
            return Results.BadRequest(new { message = "แนบรูปภาพได้สูงสุด 3 รูป" });

        foreach (var file in files)
        {
            if (file.Length <= 0 || file.Length > MaxImageBytes || !AllowedContentTypes.Contains(file.ContentType))
                return Results.BadRequest(new { message = "รองรับเฉพาะไฟล์ JPG, PNG, WEBP หรือ GIF ขนาดไม่เกิน 5 MB ต่อไฟล์" });
        }

        var report = new ProblemReport
        {
            UserId = userId,
            Category = category,
            Description = description
        };
        db.ProblemReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);

        var uploadDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "problem-reports", report.Id.ToString("N"));
        Directory.CreateDirectory(uploadDirectory);
        try
        {
            foreach (var file in files)
            {
                var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
                var path = Path.Combine(uploadDirectory, storedName);
                await using var stream = File.Create(path);
                await file.CopyToAsync(stream, cancellationToken);
                db.ProblemReportImages.Add(new ProblemReportImage
                {
                    ProblemReportId = report.Id,
                    StoredFileName = storedName,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    ContentType = file.ContentType,
                    SizeBytes = file.Length
                });
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            db.ProblemReports.Remove(report);
            await db.SaveChangesAsync(CancellationToken.None);
            if (Directory.Exists(uploadDirectory)) Directory.Delete(uploadDirectory, true);
            throw;
        }

        return Results.Ok(await ToResponseAsync(report.Id, userId, false, cancellationToken));
    }

    public Task<IResult> GetMineAsync(Guid userId, CancellationToken cancellationToken) =>
        GetReportsAsync(userId, false, cancellationToken);

    public Task<IResult> GetAllAsync(CancellationToken cancellationToken) =>
        GetReportsAsync(null, true, cancellationToken);

    public async Task<IResult> GetDetailsAsync(Guid reportId, Guid? userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var report = await db.ProblemReports.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == reportId && (isAdmin || x.UserId == userId), cancellationToken);
        if (report is null) return Results.NotFound(new { message = "ไม่พบคำร้องนี้" });
        return Results.Ok(await ToResponseAsync(reportId, userId, isAdmin, cancellationToken));
    }

    public async Task<IResult> UpdateStatusAsync(Guid reportId, string? status, CancellationToken cancellationToken)
    {
        var normalized = status?.Trim().ToLowerInvariant() switch
        {
            "pending" or "รอดำเนินการ" => ProblemReportStatus.Pending,
            "inprogress" or "กำลังดำเนินการ" => ProblemReportStatus.InProgress,
            "resolved" or "เสร็จสิ้น" => ProblemReportStatus.Resolved,
            _ => (ProblemReportStatus?)null
        };
        if (normalized is null) return Results.BadRequest(new { message = "สถานะไม่ถูกต้อง" });

        var report = await db.ProblemReports.SingleOrDefaultAsync(x => x.Id == reportId, cancellationToken);
        if (report is null) return Results.NotFound(new { message = "ไม่พบคำร้องนี้" });
        report.Status = normalized.Value;
        report.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(await ToResponseAsync(reportId, null, true, cancellationToken));
    }

    public async Task<IResult> GetImageAsync(Guid reportId, Guid imageId, Guid? userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var image = await db.ProblemReportImages.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == imageId && x.ProblemReportId == reportId &&
                 (isAdmin || db.ProblemReports.Any(report => report.Id == reportId && report.UserId == userId)), cancellationToken);
        if (image is null) return Results.NotFound();

        var path = Path.Combine(environment.ContentRootPath, "App_Data", "problem-reports", reportId.ToString("N"), image.StoredFileName);
        if (!File.Exists(path)) return Results.NotFound();
        return Results.File(path, image.ContentType, image.OriginalFileName, enableRangeProcessing: true);
    }

    private async Task<IResult> GetReportsAsync(Guid? userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var reports = await db.ProblemReports.AsNoTracking()
            .Where(x => isAdmin || x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var users = await db.Users.AsNoTracking()
            .Select(x => new { x.Id, x.Username, x.Email, name = x.FirstName + " " + x.LastName })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var images = await db.ProblemReportImages.AsNoTracking()
            .ToListAsync(cancellationToken);

        return Results.Ok(reports.Select(report => MapResponse(new
        {
            report.Id,
            report.UserId,
            user = users[report.UserId],
            report.Category,
            report.Description,
            report.Status,
            report.CreatedAt,
            report.UpdatedAt,
            images = images.Where(image => image.ProblemReportId == report.Id)
                .Select(image => new
                {
                    id = image.Id,
                    image.OriginalFileName,
                    url = $"/api/problem-reports/{report.Id}/images/{image.Id}"
                })
                .ToList()
        })));
    }

    private async Task<object?> ToResponseAsync(Guid reportId, Guid? userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var report = await db.ProblemReports.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == reportId && (isAdmin || x.UserId == userId), cancellationToken);
        if (report is null) return null;

        var user = await db.Users.AsNoTracking()
            .Where(x => x.Id == report.UserId)
            .Select(x => new { x.Username, x.Email, name = x.FirstName + " " + x.LastName })
            .FirstAsync(cancellationToken);
        var images = await db.ProblemReportImages.AsNoTracking()
            .Where(x => x.ProblemReportId == report.Id)
            .Select(x => new { id = x.Id, x.OriginalFileName, url = $"/api/problem-reports/{report.Id}/images/{x.Id}" })
            .ToListAsync(cancellationToken);

        return MapResponse(new
        {
            report.Id,
            report.UserId,
            user,
            report.Category,
            report.Description,
            report.Status,
            report.CreatedAt,
            report.UpdatedAt,
            images
        });
    }

    private static object MapResponse(dynamic report) => new
    {
        id = report.Id,
        userId = report.UserId,
        user = report.user,
        category = report.Category,
        description = report.Description,
        status = ToThaiStatus(report.Status),
        createdAt = report.CreatedAt,
        updatedAt = report.UpdatedAt,
        images = report.images
    };

    private static string ToThaiStatus(ProblemReportStatus status) => status switch
    {
        ProblemReportStatus.InProgress => "กำลังดำเนินการ",
        ProblemReportStatus.Resolved => "เสร็จสิ้น",
        _ => "รอดำเนินการ"
    };
}
