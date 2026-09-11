# Agent Implementation — SmartLab.Agent

> Windows Worker Service — ไม่ใช้ AI Model

## SmartLab.Agent.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="8.0.1" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.11" />
    <PackageReference Include="System.Management" Version="8.0.0" />
  </ItemGroup>
</Project>
```

## Program.cs

```csharp
using SmartLab.Agent;
using SmartLab.Agent.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "SmartLabAgent");
builder.Services.AddSingleton<LogQueueService>();
builder.Services.AddSingleton<ApiClientService>();
builder.Services.AddSingleton<ProcessMonitorService>();
builder.Services.AddSingleton<PowerMonitorService>();
builder.Services.AddSingleton<WebsiteBlockerService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

## Worker.cs

```csharp
using SmartLab.Agent.Services;

namespace SmartLab.Agent;

public class Worker(
    ILogger<Worker> logger,
    ApiClientService api,
    ProcessMonitorService processMonitor,
    PowerMonitorService powerMonitor,
    WebsiteBlockerService webBlocker,
    LogQueueService queue,
    IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SmartLab Agent starting...");
        await api.EnsureRegisteredAsync(stoppingToken);
        await queue.InitializeAsync();
        await webBlocker.SyncFromServerAsync(stoppingToken);

        queue.Enqueue("agent_start", new { hostname = Environment.MachineName });

        var heartbeatSec = config.GetValue("SmartLab:HeartbeatIntervalSeconds", 30);
        var flushSec = config.GetValue("SmartLab:LogFlushIntervalSeconds", 30);
        var configSyncSec = config.GetValue("SmartLab:ConfigSyncIntervalSeconds", 300);

        var lastHeartbeat = DateTime.MinValue;
        var lastFlush = DateTime.MinValue;
        var lastConfigSync = DateTime.MinValue;
        var bootTime = DateTime.UtcNow;

        processMonitor.Start();
        powerMonitor.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            processMonitor.Poll(queue);
            powerMonitor.Poll(queue);

            if ((now - lastHeartbeat).TotalSeconds >= heartbeatSec)
            {
                await api.SendHeartbeatAsync((long)(now - bootTime).TotalSeconds, stoppingToken);
                lastHeartbeat = now;
            }

            if ((now - lastFlush).TotalSeconds >= flushSec)
            {
                var pending = await queue.DequeueAllAsync();
                if (pending.Count > 0)
                    await api.SendLogsAsync(pending, stoppingToken);
                lastFlush = now;
            }

            if ((now - lastConfigSync).TotalSeconds >= configSyncSec)
            {
                await webBlocker.SyncFromServerAsync(stoppingToken);
                lastConfigSync = now;
            }

            await Task.Delay(1000, stoppingToken);
        }

        queue.Enqueue("agent_stop", new { });
        processMonitor.Stop();
    }
}
```

## Services/LogQueueService.cs

```csharp
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SmartLab.Agent.Services;

public class LogQueueService
{
    private readonly string _dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartLab", "queue.db");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();
        await new SqliteCommand("""
            CREATE TABLE IF NOT EXISTS pending_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                event_type TEXT NOT NULL,
                data_json TEXT NOT NULL,
                timestamp TEXT NOT NULL
            )
            """, conn).ExecuteNonQueryAsync();
    }

    public void Enqueue(string eventType, object data)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO pending_logs (event_type, data_json, timestamp) VALUES (@t, @d, @ts)";
        cmd.Parameters.AddWithValue("@t", eventType);
        cmd.Parameters.AddWithValue("@d", JsonSerializer.Serialize(data));
        cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public async Task<List<LogEntry>> DequeueAllAsync()
    {
        var entries = new List<LogEntry>();
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        await using (var read = conn.CreateCommand())
        {
            read.CommandText = "SELECT event_type, data_json, timestamp FROM pending_logs ORDER BY id";
            await using var reader = await read.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(new LogEntry(
                    reader.GetString(0),
                    JsonDocument.Parse(reader.GetString(1)).RootElement,
                    DateTime.Parse(reader.GetString(2))));
            }
        }

        await new SqliteCommand("DELETE FROM pending_logs", conn).ExecuteNonQueryAsync();
        return entries;
    }
}

public record LogEntry(string EventType, JsonElement Data, DateTime Timestamp);
```

## Services/ApiClientService.cs

```csharp
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartLab.Agent.Services;

public class ApiClientService(IConfiguration config, ILogger<ApiClientService> logger)
{
    private readonly HttpClient _http = new();
    private Guid _agentId;
    private string _apiKey = config["SmartLab:ApiKey"] ?? "";

    public async Task EnsureRegisteredAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_apiKey)) return;

        var baseUrl = config["SmartLab:ApiBaseUrl"] ?? "http://localhost:5000";
        var body = new
        {
            seatId = config["SmartLab:SeatId"] ?? "seat-01",
            roomId = config["SmartLab:RoomId"] ?? "lab-a",
            hostname = Environment.MachineName,
            osVersion = Environment.OSVersion.ToString()
        };

        var resp = await _http.PostAsJsonAsync($"{baseUrl}/api/agent/register", body, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        _agentId = result.GetProperty("agentId").GetGuid();
        _apiKey = result.GetProperty("apiKey").GetString()!;
        logger.LogInformation("Registered agent {AgentId}", _agentId);
    }

    private void ApplyAuth()
    {
        _http.DefaultRequestHeaders.Remove("X-Api-Key");
        _http.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
    }

    public async Task SendHeartbeatAsync(long uptimeSeconds, CancellationToken ct)
    {
        ApplyAuth();
        var baseUrl = config["SmartLab:ApiBaseUrl"] ?? "http://localhost:5000";
        await _http.PostAsJsonAsync($"{baseUrl}/api/agent/heartbeat", new
        {
            agentId = _agentId,
            timestamp = DateTime.UtcNow,
            uptimeSeconds
        }, ct);
    }

    public async Task SendLogsAsync(List<LogEntry> logs, CancellationToken ct)
    {
        ApplyAuth();
        var baseUrl = config["SmartLab:ApiBaseUrl"] ?? "http://localhost:5000";
        await _http.PostAsJsonAsync($"{baseUrl}/api/agent/logs", new
        {
            agentId = _agentId,
            logs = logs.Select(l => new { eventType = l.EventType, data = l.Data, timestamp = l.Timestamp })
        }, ct);
    }

    public async Task<JsonElement?> GetConfigAsync(CancellationToken ct)
    {
        ApplyAuth();
        var baseUrl = config["SmartLab:ApiBaseUrl"] ?? "http://localhost:5000";
        var resp = await _http.GetAsync($"{baseUrl}/api/agent/config", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
    }
}
```

## Services/ProcessMonitorService.cs

```csharp
namespace SmartLab.Agent.Services;

public class ProcessMonitorService(ILogger<ProcessMonitorService> logger)
{
    private HashSet<int> _knownPids = new();
    private Dictionary<int, string> _pidToName = new();
    private bool _running;

    public void Start()
    {
        _running = true;
        foreach (var p in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                _knownPids.Add(p.Id);
                _pidToName[p.Id] = p.ProcessName;
            }
            catch { /* access denied */ }
        }
    }

    public void Stop() => _running = false;

    public void Poll(LogQueueService queue)
    {
        if (!_running) return;
        var current = new HashSet<int>();
        var currentNames = new Dictionary<int, string>();

        foreach (var p in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                current.Add(p.Id);
                currentNames[p.Id] = p.ProcessName;
                if (!_knownPids.Contains(p.Id))
                {
                    queue.Enqueue("process_start", new
                    {
                        processName = p.ProcessName,
                        pid = p.Id,
                        exePath = TryGetPath(p)
                    });
                }
            }
            catch { /* skip */ }
        }

        foreach (var oldPid in _knownPids.Except(current))
        {
            _pidToName.TryGetValue(oldPid, out var name);
            queue.Enqueue("process_stop", new { processName = name ?? "unknown", pid = oldPid });
        }

        _knownPids = current;
        _pidToName = currentNames;
    }

    private static string TryGetPath(System.Diagnostics.Process p)
    {
        try { return p.MainModule?.FileName ?? ""; }
        catch { return ""; }
    }
}
```

## Services/PowerMonitorService.cs

```csharp
namespace SmartLab.Agent.Services;

public class PowerMonitorService
{
    private bool _bootLogged;

    public void Start()
    {
        // Boot event logged on first Poll
    }

    public void Poll(LogQueueService queue)
    {
        if (_bootLogged) return;
        queue.Enqueue("power_boot", new { bootTime = DateTime.UtcNow });
        _bootLogged = true;
    }
}
```

## Services/WebsiteBlockerService.cs

```csharp
using System.Text;
using System.Text.Json;

namespace SmartLab.Agent.Services;

public class WebsiteBlockerService(ApiClientService api, ILogger<WebsiteBlockerService> logger)
{
    private const string MarkerStart = "# SMARTLAB-BLOCK-START";
    private const string MarkerEnd = "# SMARTLAB-BLOCK-END";
    private readonly string _hostsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "drivers", "etc", "hosts");

    public async Task SyncFromServerAsync(CancellationToken ct)
    {
        var config = await api.GetConfigAsync(ct);
        if (config is null) return;

        var patterns = config.Value.GetProperty("blacklist")
            .EnumerateArray()
            .Select(b => b.GetProperty("urlPattern").GetString()!)
            .ToList();

        ApplyHostsBlock(patterns);
        logger.LogInformation("Synced {Count} blocked domains", patterns.Count);
    }

    private void ApplyHostsBlock(List<string> domains)
    {
        var lines = File.Exists(_hostsPath)
            ? File.ReadAllLines(_hostsPath).ToList()
            : new List<string>();

        // Remove old block section
        var startIdx = lines.FindIndex(l => l.Trim() == MarkerStart);
        var endIdx = lines.FindIndex(l => l.Trim() == MarkerEnd);
        if (startIdx >= 0 && endIdx >= 0)
            lines.RemoveRange(startIdx, endIdx - startIdx + 1);

        // Add new block section
        lines.Add(MarkerStart);
        foreach (var domain in domains)
            lines.Add($"127.0.0.1 {domain}");
        lines.Add(MarkerEnd);

        File.WriteAllLines(_hostsPath, lines, Encoding.UTF8);
    }
}
```

## appsettings.json

```json
{
  "SmartLab": {
    "ApiBaseUrl": "http://localhost:5000",
    "ApiKey": "",
    "SeatId": "seat-01",
    "RoomId": "lab-a",
    "HeartbeatIntervalSeconds": 30,
    "LogFlushIntervalSeconds": 30,
    "ConfigSyncIntervalSeconds": 300
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## install/register-agent.ps1

```powershell
param(
    [Parameter(Mandatory=$true)][string]$SeatId,
    [Parameter(Mandatory=$true)][string]$RoomId,
    [string]$ApiUrl = "http://localhost:5000"
)

$settingsPath = Join-Path $PSScriptRoot "..\appsettings.json"
$settings = Get-Content $settingsPath | ConvertFrom-Json
$settings.SmartLab.SeatId = $SeatId
$settings.SmartLab.RoomId = $RoomId
$settings.SmartLab.ApiBaseUrl = $ApiUrl
$settings | ConvertTo-Json -Depth 5 | Set-Content $settingsPath
Write-Host "Agent configured: Seat=$SeatId Room=$RoomId API=$ApiUrl"
```

> **หมายเหตุ:** การแก้ hosts file ต้องรัน Agent ด้วยสิทธิ์ Administrator
