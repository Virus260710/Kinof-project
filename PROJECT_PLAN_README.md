# Smart Lab Management System

ระบบบริหารจัดการห้องแล็บอัจฉริยะ — โปรเจกต Zenith Comp

## Phase 1 (กำลังทำ): Tracking Agent

- Windows Agent — log โปรแกรม, เปิด-ปิดเครื่อง, block เว็บ
- Backend API — รับ log + ส่ง config
- Admin Dashboard — ดู log + จัดการ blacklist

## เริ่มต้น

### ติดตั้ง Prerequisites

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### สร้างโปรเจกต (Agent ตัวถัดไป)

โค้ดเต็มอยู่ใน `docs/implementation/`:

```
docs/implementation/BACKEND.md  → copy เป็น backend/SmartLab.Api/
docs/implementation/AGENT.md    → copy เป็น agent/SmartLab.Agent/
docs/implementation/ADMIN.md    → copy เป็น wwwroot/admin/
```

### รัน

```powershell
# Backend
dotnet run --project backend/SmartLab.Api

# Agent (terminal ใหม่, ต้อง Run as Administrator สำหรับ web block)
dotnet run --project agent/SmartLab.Agent
```

- API: http://localhost:5000
- Admin: http://localhost:5000/admin/

## เอกสาร

| ไฟล์ | คำอธิบาย |
|------|----------|
| [docs/AGENT_HANDOFF.md](docs/AGENT_HANDOFF.md) | คู่มือสำหรับ agent ตัวถัดไป |
| [docs/API.md](docs/API.md) | API endpoints |
| [docs/MODEL_RECOMMENDATIONS.md](docs/MODEL_RECOMMENDATIONS.md) | Model/tech ที่ใช้แต่ละ phase |

## Tracking Agent ใช้ AI Model ไหม?

**ไม่ใช้** — ใช้ WMI, Windows Event Log, hosts file, HTTP REST เท่านั้น  
AI (InsightFace) ใช้ใน Phase 2 สำหรับ Face Recognition
