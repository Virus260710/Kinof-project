# Backend Implementation — SmartLab.Api

> Agent ตัวถัดไป: copy โค้ดด้านล่างไปสร้างไฟล์จริง แล้วรัน `dotnet run`

## SmartLab.Api.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

## Models/AgentDevice.cs

```csharp
namespace SmartLab.Api.Models;

public class AgentDevice
{
    public Guid Id { get; set; }
    public string SeatId { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public DateTime? LastHeartbeat { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

## Models/AgentLog.cs

```csharp
namespace SmartLab.Api.Models;

public class AgentLog
{
    public long Id { get; set; }
    public Guid AgentId { get; set; }
    public string EventType { get; set; } = "";
    public string DataJson { get; set; } = "{}";
    public DateTime Timestamp { get; set; }
    public AgentDevice? Agent { get; set; }
}
```

## Models/WebsiteBlacklist.cs

```csharp
namespace SmartLab.Api.Models;

public class WebsiteBlacklist
{
    public int Id { get; set; }
    public string UrlPattern { get; set; } = "";
    public string Category { get; set; } = "custom";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

## Data/AppDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using SmartLab.Api.Models;

namespace SmartLab.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AgentDevice> Agents => Set<AgentDevice>();
    public DbSet<AgentLog> AgentLogs => Set<AgentLog>();
    public DbSet<WebsiteBlacklist> Blacklist => Set<WebsiteBlacklist>();
}
```

## Data/DbInitializer.cs

```csharp
using SmartLab.Api.Models;

namespace SmartLab.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (db.Blacklist.Any()) return;

        db.Blacklist.AddRange(
            new WebsiteBlacklist { UrlPattern = "facebook.com", Category = "social" },
            new WebsiteBlacklist { UrlPattern = "twitter.com", Category = "social" },
            new WebsiteBlacklist { UrlPattern = "tiktok.com", Category = "social" },
            new WebsiteBlacklist { UrlPattern = "pornhub.com", Category = "adult" },
            new WebsiteBlacklist { UrlPattern = "thepiratebay.org", Category = "torrent" }
        );
        await db.SaveChangesAsync();
    }
}
```

## Program.cs

```csharp
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartLab.Api.Data;
using SmartLab.Api.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=smartlab.db"));
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
    await DbInitializer.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

// --- Agent: Register ---
app.MapPost("/api/agent/register", async (RegisterRequest req, AppDbContext db) =>
{
    var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var agent = new AgentDevice
    {
        Id = Guid.NewGuid(),
        SeatId = req.SeatId,
        RoomId = req.RoomId,
        Hostname = req.Hostname,
        OsVersion = req.OsVersion,
        ApiKey = key
    };
    db.Agents.Add(agent);
    await db.SaveChangesAsync();
    return Results.Ok(new { agentId = agent.Id, apiKey = key });
});

// --- Agent: Heartbeat ---
app.MapPost("/api/agent/heartbeat", async (HeartbeatRequest req, HttpRequest http, AppDbContext db) =>
{
    if (!await ValidateAgent(req.AgentId, http, db)) return Results.Unauthorized();
    var agent = await db.Agents.FindAsync(req.AgentId);
    if (agent is null) return Results.NotFound();
    agent.LastHeartbeat = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { ok = true });
});

// --- Agent: Logs ---
app.MapPost("/api/agent/logs", async (LogBatchRequest req, HttpRequest http, AppDbContext db) =>
{
    if (!await ValidateAgent(req.AgentId, http, db)) return Results.Unauthorized();
    foreach (var log in req.Logs.Take(100))
    {
        db.AgentLogs.Add(new AgentLog
        {
            AgentId = req.AgentId,
            EventType = log.EventType,
            DataJson = JsonSerializer.Serialize(log.Data),
            Timestamp = log.Timestamp
        });
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { received = req.Logs.Count });
});

// --- Agent: Config ---
app.MapGet("/api/agent/config", async (HttpRequest http, AppDbContext db) =>
{
    var agent = await GetAgentFromKey(http, db);
    if (agent is null) return Results.Unauthorized();
    var blacklist = await db.Blacklist.Select(b => new { b.UrlPattern, b.Category }).ToListAsync();
    return Results.Ok(new
    {
        blacklist,
        blockedProcessNames = new[] { "tor.exe", "bittorrent.exe" },
        configVersion = blacklist.Count
    });
});

// --- Admin: Agents ---
app.MapGet("/api/admin/agents", async (AppDbContext db) =>
{
    var cutoff = DateTime.UtcNow.AddSeconds(-90);
    var agents = await db.Agents.Select(a => new
    {
        a.Id,
        agentId = a.Id,
        a.SeatId,
        a.RoomId,
        a.Hostname,
        isOnline = a.LastHeartbeat != null && a.LastHeartbeat > cutoff,
        lastHeartbeat = a.LastHeartbeat
    }).ToListAsync();
    return Results.Ok(agents);
});

// --- Admin: Logs ---
app.MapGet("/api/admin/logs", async (AppDbContext db,
    string? seatId, string? roomId, string? eventType,
    DateTime? from, DateTime? to, int page = 1, int limit = 50) =>
{
    var q = db.AgentLogs.Include(l => l.Agent).AsQueryable();
    if (seatId != null) q = q.Where(l => l.Agent!.SeatId == seatId);
    if (roomId != null) q = q.Where(l => l.Agent!.RoomId == roomId);
    if (eventType != null) q = q.Where(l => l.EventType == eventType);
    if (from != null) q = q.Where(l => l.Timestamp >= from);
    if (to != null) q = q.Where(l => l.Timestamp <= to);

    var total = await q.CountAsync();
    var logs = await q.OrderByDescending(l => l.Timestamp)
        .Skip((page - 1) * limit).Take(limit)
        .Select(l => new
        {
            l.Id, l.EventType, l.DataJson, l.Timestamp,
            l.Agent!.SeatId, l.Agent.RoomId, l.Agent.Hostname
        }).ToListAsync();
    return Results.Ok(new { total, page, logs });
});

// --- Admin: Blacklist CRUD ---
app.MapGet("/api/admin/blacklist", async (AppDbContext db) =>
    Results.Ok(await db.Blacklist.ToListAsync()));

app.MapPost("/api/admin/blacklist", async (BlacklistRequest req, AppDbContext db) =>
{
    var item = new WebsiteBlacklist { UrlPattern = req.UrlPattern, Category = req.Category };
    db.Blacklist.Add(item);
    await db.SaveChangesAsync();
    return Results.Ok(item);
});

app.MapDelete("/api/admin/blacklist/{id:int}", async (int id, AppDbContext db) =>
{
    var item = await db.Blacklist.FindAsync(id);
    if (item is null) return Results.NotFound();
    db.Blacklist.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

// --- Helpers & DTOs ---
static async Task<bool> ValidateAgent(Guid agentId, HttpRequest http, AppDbContext db)
{
    var agent = await GetAgentFromKey(http, db);
    return agent != null && agent.Id == agentId;
}

static async Task<AgentDevice?> GetAgentFromKey(HttpRequest http, AppDbContext db)
{
    if (!http.Headers.TryGetValue("X-Api-Key", out var keyVal)) return null;
    return await db.Agents.FirstOrDefaultAsync(a => a.ApiKey == keyVal.ToString());
}

record RegisterRequest(string SeatId, string RoomId, string Hostname, string OsVersion);
record HeartbeatRequest(Guid AgentId, DateTime Timestamp, long UptimeSeconds);
record LogBatchRequest(Guid AgentId, List<LogEntry> Logs);
record LogEntry(string EventType, JsonElement Data, DateTime Timestamp);
record BlacklistRequest(string UrlPattern, string Category);
```

## appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=smartlab.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```
