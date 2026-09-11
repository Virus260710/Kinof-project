# Smart Lab — Agent Handoff (Phase 1: Tracking Agent)

> เอกสารนี้สำหรับ Agent ตัวถัดไปที่จะ implement โค้ดจริง  
> สถานะ: **Scaffold พร้อมแล้ว — รอ implement โค้ด**

## สิ่งที่ต้องทำก่อนรัน

เครื่อง dev ต้องติดตั้ง:

1. **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** — ใช้ทั้ง Backend และ Agent
2. **Git** — มีแล้ว (repo init ที่ `C:\Users\User\Projects\smartlab`)

ตรวจสอบ:
```powershell
dotnet --version   # ต้องได้ 8.x
```

> **หมายเหตุ:** เครื่องปัจจุบันยังไม่มี .NET SDK / Python / Node — โค้ดถูกเตรียมเป็น spec ในเอกสารนี้

---

## คำตอบ: งาน Tracking Agent ต้องใช้ AI Model ไหม?

### **ไม่ต้องใช้ AI Model สำหรับ Tracking Agent**

Tracking Agent เป็นงาน **system monitoring + network filtering** ไม่ใช่งาน Machine Learning

| งาน | ใช้ Model/เทคโนologi อะไร | ต้อง AI ไหม |
|-----|---------------------------|-------------|
| Log การเปิดโปรแกรม | **WMI** (`Win32_ProcessStartTrace`) หรือ polling `Process.GetProcesses()` | ไม่ |
| Log เปิด-ปิดเครื่อง | **Windows Event Log** (Event ID 6005/6006/1074) | ไม่ |
| Block เว็บอันตราย | แก้ **`C:\Windows\System32\drivers\etc\hosts`** ตาม blacklist | ไม่ |
| ส่ง log ไป server | **HTTP REST** (HttpClient) | ไม่ |
| Heartbeat | Timer + POST `/api/agent/heartbeat` | ไม่ |
| Local queue offline | **SQLite** (`Microsoft.Data.Sqlite`) | ไม่ |

### AI Model ใช้เมื่อไหร่? (Phase 2+ เท่านั้น)

| งาน | Model ที่แนะนำ |
|-----|--------------|
| Face Recognition (Phase 2) | **InsightFace buffalo_l** (ArcFace) |
| Anti-spoofing (optional) | Silent-Face-Anti-Spoofing |
| 2FA (Phase 3) | TOTP RFC 6238 — ไม่ใช่ AI |

---

## โครงสร้างโปรเจกต (สร้างเมื่อ switch เป็น Agent mode)

```
smartlab/
├── smartlab.sln
├── README.md
├── docs/
│   ├── AGENT_HANDOFF.md          ← ไฟล์นี้
│   ├── MODEL_RECOMMENDATIONS.md
│   └── API.md
├── backend/
│   └── SmartLab.Api/
│       ├── SmartLab.Api.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   └── DbInitializer.cs
│       ├── Models/
│       │   ├── AgentDevice.cs
│       │   ├── AgentLog.cs
│       │   └── WebsiteBlacklist.cs
│       └── wwwroot/
│           └── admin/
│               ├── index.html
│               └── app.js
└── agent/
    └── SmartLab.Agent/
        ├── SmartLab.Agent.csproj
        ├── Program.cs
        ├── Worker.cs
        ├── appsettings.json
        ├── Services/
        │   ├── ProcessMonitorService.cs
        │   ├── PowerMonitorService.cs
        │   ├── WebsiteBlockerService.cs
        │   ├── LogQueueService.cs
        │   └── ApiClientService.cs
        └── install/
            └── register-agent.ps1
```

---

## สรุปลำดับงาน + Model แนะนำ (Quick Reference)

### ทำอะไรก่อน (ลำดับ)

| ลำดับ | งาน | ต้องมีเว็บไหม |
|-------|-----|---------------|
| **1** | ติดตั้ง .NET 8 SDK | ไม่ |
| **2** | Backend API (รับ log, heartbeat, blacklist) | ไม่ — แค่ API |
| **3** | Windows Tracking Agent | ไม่ |
| **4** | Admin HTML 1 หน้า (ดู log) | หน้าเดียว พอ |
| 5 | Face Recognition + 2FA + Kiosk | Phase 2 |
| 6 | เว็บนักศึกษา QR + External register | Phase 4 |

### Cursor AI Model แนะนำ

| งาน | Model |
|-----|-------|
| **Implement Phase 1 (copy spec → รัน)** | **Composer 2.5 Fast** |
| Debug Windows / WMI / hosts permission | Claude Sonnet 5 Thinking |
| ออกแบบ Face Recognition Phase 2 | Claude Opus 5 Thinking |

### ML Model ในระบบ Smart Lab

| Phase | ใช้ AI ไหม | Model |
|-------|-----------|-------|
| Phase 1 Tracking Agent | **ไม่** | WMI + hosts file |
| Phase 2 Face Auth | **ใช่** | InsightFace buffalo_l |
| Phase 3 2FA | ไม่ | TOTP |

### Prompt สำหรับ AI ตัวถัดไป

→ ดู [docs/NEXT_AGENT_PROMPT.md](./NEXT_AGENT_PROMPT.md) copy ไปวางได้เลย

---

## Checklist สำหรับ Agent ตัวถัดไป

### Step 1 — สร้าง Solution + Backend API

- [ ] สร้าง `smartlab.sln` + `SmartLab.Api` (ASP.NET Core 8 Minimal API)
- [ ] ใช้ **SQLite** + Entity Framework Core (ไม่ต้องติด PostgreSQL ตอน dev)
- [ ] Implement endpoints ตาม [API.md](./API.md)
- [ ] Seed blacklist เริ่มต้น (gambling, malware domains)
- [ ] ทดสอบ: `dotnet run --project backend/SmartLab.Api`

### Step 2 — สร้าง Windows Agent

- [ ] สร้าง `SmartLab.Agent` (.NET 8 Worker Service, `net8.0-windows`)
- [ ] `ProcessMonitorService` — ตรวจ process start/stop ทุก 2 วินาที
- [ ] `PowerMonitorService` — log boot/shutdown
- [ ] `WebsiteBlockerService` — sync hosts file จาก `/api/agent/config`
- [ ] `LogQueueService` — SQLite local queue, flush batch ทุก 30 วินาที
- [ ] `ApiClientService` — register, heartbeat, send logs
- [ ] ทดสอบ: `dotnet run --project agent/SmartLab.Agent`

### Step 3 — Admin Log Viewer

- [ ] Static HTML ใน `wwwroot/admin/` — ดู agents online + logs
- [ ] CRUD blacklist (form ง่ายๆ)

### Step 4 — Installer

- [ ] `register-agent.ps1` — รับ seat_id, room_id, api_url → เขียน appsettings
- [ ] (optional) ติดตั้งเป็น Windows Service: `sc create SmartLabAgent ...`

---

## วิธีรัน (หลัง implement)

```powershell
# Terminal 1 — Backend
cd C:\Users\User\Projects\smartlab
dotnet run --project backend/SmartLab.Api
# → http://localhost:5000
# → Admin: http://localhost:5000/admin/

# Terminal 2 — Agent (dev mode)
cd C:\Users\User\Projects\smartlab
dotnet run --project agent/SmartLab.Agent
```

---

## Config Agent (appsettings.json)

```json
{
  "SmartLab": {
    "ApiBaseUrl": "http://localhost:5000",
    "ApiKey": "",
    "SeatId": "seat-01",
    "RoomId": "lab-a",
    "HeartbeatIntervalSeconds": 30,
    "LogFlushIntervalSeconds": 30,
    "ProcessPollIntervalSeconds": 2
  }
}
```

---

## สิ่งที่ยังไม่ทำ (Phase 2+)

- Face Recognition (InsightFace)
- 2FA TOTP
- QR Booking
- PostgreSQL migration (production)
- Excel export

---

## อ้างอิง

- Plan หลัก: `~/.cursor/plans/smart_lab_scope_plan_b1054386.plan.md`
- Requirements: AGENT-1 ถึง AGENT-9 ใน Module 3
