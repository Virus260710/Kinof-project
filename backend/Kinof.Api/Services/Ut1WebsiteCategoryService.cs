using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Sockets;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record ImportUt1CategoryRequest(string? Category, int? Limit);

public sealed class Ut1WebsiteCategoryService(
    HttpClient http,
    AppDbContext db,
    AuditLogService auditLog,
    IWebHostEnvironment environment,
    ILogger<Ut1WebsiteCategoryService> logger)
{
    public const int DefaultLimit = 250;
    public const int MaxLimit = 400;
    public const string SourceUt1 = "ut1";
    public const string SourceManual = "manual";
    public const string CatalogUrl = "https://dsi.ut-capitole.fr/blacklists/index_en.php";

    private const string DownloadBase = "https://dsi.ut-capitole.fr/blacklists/download/";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public static readonly Ut1Category[] Catalog =
    [
        new("social_networks", "โซเชียลเน็ตเวิร์ก", "Facebook, TikTok, Instagram, X"),
        new("streaming", "สตรีมวิดีโอ/เพลง", "YouTube, Netflix, Twitch, Spotify"),
        new("games", "เกม", "Steam, Roblox, Epic, Riot"),
        new("adult", "เนื้อหาผู้ใหญ่", "เว็บโป๊และเนื้อหาผู้ใหญ่"),
        new("gambling", "พนัน", "คาสิโนและพนันออนไลน์"),
        new("malware", "มัลแวร์", "โดเมนกระจายมัลแวร์"),
        new("phishing", "ฟิชชิง", "เว็บหลอกลวง"),
        new("chat", "แชท", "เว็บแชทสาธารณะ"),
        new("dating", "หาคู่", "เว็บเดท")
    ];

    public sealed record Ut1Category(string Id, string Label, string Description);

    public async Task<IResult> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        var imported = await db.WebsiteBlacklist.AsNoTracking()
            .Where(entry => entry.Source == SourceUt1)
            .GroupBy(entry => entry.Category)
            .Select(group => new
            {
                category = group.Key,
                count = group.Count(),
                lastImportedAt = group.Max(item => item.CreatedAt)
            })
            .ToListAsync(cancellationToken);
        var byId = imported.ToDictionary(item => item.category, StringComparer.OrdinalIgnoreCase);
        var sampleByCategory = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Catalog)
        {
            var samples = await db.WebsiteBlacklist.AsNoTracking()
                .Where(entry => entry.Source == SourceUt1 && entry.Category == item.Id)
                .OrderBy(entry => entry.UrlPattern)
                .Select(entry => entry.UrlPattern)
                .Take(8)
                .ToListAsync(cancellationToken);
            sampleByCategory[item.Id] = samples;
        }

        return Results.Ok(new
        {
            source = CatalogUrl,
            defaultLimit = DefaultLimit,
            maxLimit = MaxLimit,
            items = Catalog.Select(item =>
            {
                byId.TryGetValue(item.Id, out var row);
                sampleByCategory.TryGetValue(item.Id, out var samples);
                return new
                {
                    id = item.Id,
                    label = item.Label,
                    description = item.Description,
                    importedCount = row?.count ?? 0,
                    lastImportedAt = row?.lastImportedAt,
                    sampleDomains = samples ?? []
                };
            })
        });
    }

    public async Task<IResult> ImportAsync(
        Guid actorUserId,
        ImportUt1CategoryRequest? request,
        CancellationToken cancellationToken)
    {
        var category = Catalog.FirstOrDefault(item =>
            string.Equals(item.Id, request?.Category?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (category is null)
            return Results.BadRequest(new { message = "เลือกหมวดจากรายการ UT1 ที่ระบบรองรับ" });

        var limit = request?.Limit is int value
            ? Math.Clamp(value, 1, MaxLimit)
            : DefaultLimit;

        var fetched = await LoadDomainsAsync(category.Id, cancellationToken);
        if (fetched.Domains.Count == 0)
        {
            return Results.Json(new
            {
                message = "ดึงรายการโดเมนจาก UT1 ไม่ได้ และไม่มีรายการสำรองของหมวดนี้"
            }, statusCode: StatusCodes.Status502BadGateway);
        }

        var selected = fetched.Domains.Take(limit).ToList();
        var selectedSet = selected.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await db.WebsiteBlacklist.ToListAsync(cancellationToken);
        var byDomain = existing.ToDictionary(entry => entry.UrlPattern, StringComparer.OrdinalIgnoreCase);
        var ut1SameCategory = existing
            .Where(entry => entry.Source == SourceUt1 &&
                            string.Equals(entry.Category, category.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var removed = 0;
        foreach (var entry in ut1SameCategory.Where(entry => !selectedSet.Contains(entry.UrlPattern)))
        {
            db.WebsiteBlacklist.Remove(entry);
            removed++;
        }

        var added = 0;
        var skipped = 0;
        foreach (var domain in selected)
        {
            if (byDomain.TryGetValue(domain, out var current))
            {
                skipped++;
                if (current.Source == SourceUt1)
                    current.Category = category.Id;
                continue;
            }

            db.WebsiteBlacklist.Add(new WebsiteBlacklist
            {
                UrlPattern = domain,
                Category = category.Id,
                Source = SourceUt1,
                CreatedBy = actorUserId
            });
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        var importedCount = await db.WebsiteBlacklist.CountAsync(
            entry => entry.Source == SourceUt1 && entry.Category == category.Id,
            cancellationToken);

        await auditLog.WriteAsync(
            actorUserId,
            "tracking.ut1_import",
            "website_blacklist",
            category.Id,
            $"{category.Id} +{added} -{removed} เหลือ {importedCount} จากต้นทาง {fetched.SourceCount}",
            cancellationToken);

        return Results.Ok(new
        {
            category = category.Id,
            label = category.Label,
            importedCount,
            added,
            removed,
            skippedExisting = skipped,
            sourceCount = fetched.SourceCount,
            truncated = fetched.SourceCount > selected.Count,
            fallback = fetched.Fallback,
            limit
        });
    }

    public async Task<IResult> RemoveCategoryAsync(
        Guid actorUserId,
        string categoryId,
        CancellationToken cancellationToken)
    {
        var category = Catalog.FirstOrDefault(item =>
            string.Equals(item.Id, categoryId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (category is null)
            return Results.BadRequest(new { message = "ไม่พบหมวด UT1 นี้" });

        var entries = await db.WebsiteBlacklist
            .Where(entry => entry.Source == SourceUt1 && entry.Category == category.Id)
            .ToListAsync(cancellationToken);
        if (entries.Count == 0)
            return Results.NotFound(new { message = "ยังไม่ได้นำเข้าหมวดนี้" });

        db.WebsiteBlacklist.RemoveRange(entries);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.ut1_remove",
            "website_blacklist",
            category.Id,
            $"นำออก {entries.Count} โดเมน",
            cancellationToken);

        return Results.Ok(new { category = category.Id, removed = entries.Count });
    }

    private async Task<(List<string> Domains, int SourceCount, bool Fallback)> LoadDomainsAsync(
        string categoryId,
        CancellationToken cancellationToken)
    {
        var cached = TryReadCache(categoryId);
        if (cached is { Count: > 0 })
            return (Rank(cached), cached.Count, false);

        try
        {
            var downloaded = await DownloadDomainsAsync(categoryId, cancellationToken);
            if (downloaded.Count > 0)
            {
                TryWriteCache(categoryId, downloaded);
                return (Rank(downloaded), downloaded.Count, false);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or SocketException or InvalidDataException)
        {
            logger.LogWarning(ex, "ดาวน์โหลดหมวด UT1 {Category} ไม่สำเร็จ", categoryId);
        }

        var fallback = FallbackDomains(categoryId);
        return (Rank(fallback), fallback.Count, fallback.Count > 0);
    }

    private async Task<List<string>> DownloadDomainsAsync(string categoryId, CancellationToken cancellationToken)
    {
        var url = $"{DownloadBase}{categoryId}.tar.gz";
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var gzip = new GZipStream(await response.Content.ReadAsStreamAsync(cancellationToken), CompressionMode.Decompress);
        using var tar = new TarReader(gzip);

        while (await tar.GetNextEntryAsync(copyData: true, cancellationToken) is { } entry)
        {
            if (entry.DataStream is null) continue;
            var name = entry.Name.Replace('\\', '/');
            var fileName = Path.GetFileName(name.TrimEnd('/'));
            if (!string.Equals(fileName, "domains", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var reader = new StreamReader(entry.DataStream);
            var text = await reader.ReadToEndAsync(cancellationToken);
            return ParseDomains(text);
        }

        return [];
    }

    internal static List<string> ParseDomains(string text)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            var host = WebsiteBlacklistService.Normalize(line);
            if (host is null) continue;
            if (host.StartsWith('#') || host.Contains('*') || host.Contains(':')) continue;
            if (System.Net.IPAddress.TryParse(host, out _)) continue;
            if (host.Length is < 3 or > 255) continue;
            names.Add(host);
        }

        return [.. names];
    }

    internal static List<string> Rank(IReadOnlyCollection<string> domains) =>
        domains
            .OrderBy(domain => domain.Count(character => character == '.'))
            .ThenBy(domain => domain.Length)
            .ThenBy(domain => domain, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private string CachePath(string categoryId) =>
        Path.Combine(environment.ContentRootPath, "App_Data", "ut1-cache", $"{categoryId}.txt");

    private List<string>? TryReadCache(string categoryId)
    {
        try
        {
            var path = CachePath(categoryId);
            if (!File.Exists(path)) return null;
            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > CacheTtl) return null;
            return ParseDomains(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "อ่านแคช UT1 {Category} ไม่ได้", categoryId);
            return null;
        }
    }

    private void TryWriteCache(string categoryId, IReadOnlyCollection<string> domains)
    {
        try
        {
            var path = CachePath(categoryId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, domains);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "เขียนแคช UT1 {Category} ไม่ได้", categoryId);
        }
    }

    private static List<string> FallbackDomains(string categoryId) => categoryId switch
    {
        "social_networks" =>
        [
            "facebook.com", "instagram.com", "tiktok.com", "x.com", "twitter.com",
            "reddit.com", "linkedin.com", "snapchat.com", "pinterest.com", "threads.net",
            "vk.com", "tumblr.com"
        ],
        "streaming" =>
        [
            "youtube.com", "netflix.com", "twitch.tv", "spotify.com", "disneyplus.com",
            "vimeo.com", "dailymotion.com", "soundcloud.com"
        ],
        "games" =>
        [
            "steampowered.com", "steamcommunity.com", "epicgames.com", "roblox.com",
            "riotgames.com", "minecraft.net", "ea.com", "blizzard.com", "xbox.com"
        ],
        _ => []
    };
}
