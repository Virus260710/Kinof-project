# KINOF Windows Agent

โค้ดจริงอยู่ที่ `windows-agent/` — Worker + หน้าล็อกอิน WinForms (`Kinof.Agent`)  
**ไม่ใช้ AI Model** · อย่า copy จาก repo smartlab

Header ทุก request: `X-Agent-Key`  
Backend: `http://localhost:5106`

## สิ่งที่ทำแล้ว

| งาน | พฤติกรรม |
|-----|----------|
| ผูกเครื่อง | `POST /api/agent/register` ด้วย API key ที่ผูก `seat_id` |
| Heartbeat | `POST /api/agent/heartbeat` ทุก 20 วินาที (ตั้งได้) — ออนไลน์ถ้าไม่เกิน 60 วินาที |
| ล็อกอินเครื่อง | บัญชี KINOF + OTP อีเมล → ที่นั่ง `Occupied` |
| ออกจากระบบ | ปุ่มบน Agent / ผู้ดูแลสั่งออกจากระบบ → `Available` |
| ซ่อนถาดระบบ | ปิดหน้าต่างแล้วยังทำงานที่ไอคอนถาด — ที่นั่งว่างเมื่อกดออกจากระบบเครื่องนี้เท่านั้น |
| บล็อกเว็บ | `GET /api/agent/website-blacklist` แล้วเขียนช่วง `# KINOF-BLOCK-START` … `# KINOF-BLOCK-END` ใน hosts (รายการมาจากโดเมนที่ใส่เอง + หมวด UT1 ที่นำเข้าแบบจำกัดจำนวน) |
| บล็อกโปรแกรม | `GET /api/agent/program-blacklist` ตามรอบ heartbeat แล้วปิด process ที่ตรง `process_name` |
| รายการอนุญาต | heartbeat คืน `programAllowlist` — ใช้แยกโปรแกรมที่ไม่รู้จัก |
| สรุปไม่รู้จัก | ทุก 60 วินาที ส่ง `POST /api/agent/logs` event `unknown_program` (ไม่ใช่ระบบ Windows ไม่ใช่ allowlist ไม่ใช่ blacklist) รวมชื่อละไม่เกินครั้งละ 5 นาที — **ไม่ขึ้นคิวน่าสงสัย** |

นโยบายที่นั่ง (อย่ากลับไปจ่ายที่นั่งที่ Kiosk):

- Kiosk = ตรวจสิทธิ์เข้าห้องเท่านั้น ไม่จองที่นั่ง
- ที่นั่ง Occupied เมื่อล็อกอินบน Agent
- ออกจากระบบเครื่องนี้เท่านั้นที่ปล่อยที่นั่ง

## ไฟล์

| ไฟล์ | หน้าที่ |
|------|--------|
| `Program.cs` | Host + หน้า `LoginForm` (อย่าติดตั้งเป็น Windows Service ในรอบนี้ เพราะต้องมี UI) |
| `Worker.cs` | ลูป heartbeat / ซิงค์ blacklist / สแกนโปรแกรม |
| `AgentApiClient.cs` | HTTP ไป backend |
| `LoginForm.cs` | ล็อกอิน / OTP / ออกจากระบบ / ถาดระบบ |
| `HostsWebsiteBlocker.cs` | เขียน hosts (ต้อง Administrator) |
| `ProgramProcessBlocker.cs` | ปิด process ตามรายการบล็อก และสรุป process ที่ไม่รู้จัก |
| `appsettings.json` | `ApiBaseUrl`, `ApiKey`, ช่วงเวลา heartbeat / sync / สแกน |

```json
{
  "Kinof": {
    "ApiBaseUrl": "http://localhost:5106",
    "ApiKey": "dev-agent-key-1",
    "HeartbeatIntervalSeconds": 20,
    "BlacklistSyncIntervalSeconds": 120,
    "ProcessScanIntervalSeconds": 3,
    "UnknownProgramReportIntervalSeconds": 60
  }
}
```

คีย์ dev จาก seeder: `dev-agent-key-1` = ที่นั่ง 1 ห้องแรก, `dev-agent-key-2` = ที่นั่ง 2  
คีย์จริง: แอดมิน `POST /api/admin/agents` ด้วย `{ "seatId": "..." }` — ได้ `apiKey` ครั้งเดียว

Kiosk ประตูใช้ header คนละตัว: `X-Kiosk-Key` ผูกห้อง (`dev-kiosk-key-1` = ห้องแรกตามชื่อ) ไม่ผูกที่นั่ง

## บล็อกโปรแกรม

1. แอดมินตั้งรายการที่ Monitor → **บล็อกโปรแกรม** (`GET/POST/DELETE /api/admin/tracking/program-blacklist`)
2. Seed เริ่มต้นมี `discord.exe`, `steam.exe`
3. ทุก heartbeat Agent ดึง `GET /api/agent/program-blacklist` → `{ processNames: ["discord.exe", "steam.exe"] }`
4. ทุก 3 วินาที สแกน process ถ้าชื่อตรง (เช่น `discord` / `discord.exe`) แล้ว `Process.Kill` เฉพาะ process นั้น
5. ส่ง `POST /api/agent/logs` event `program` (`suspicious: true`) ให้แท็บ Monitor **น่าสงสัย** ขึ้นคิวรอแอดมินตรวจ ดี/แย่

## รายการอนุญาต + ของไม่รู้จัก

1. แอดมินตั้งแท็บ Monitor → **อนุญาตโปรแกรม** (`GET/POST/DELETE /api/admin/tracking/program-allowlist`) — seed มีเบราว์เซอร์ / VS Code / Office ฯลฯ
2. Heartbeat คืน `programAllowlist` คู่กับ `programBlacklist`
3. Agent สแกน process ที่ไม่ใช่ระบบ Windows (`C:\Windows\...`) ไม่ใช่รายการอนุญาต และไม่ใช่รายการห้าม
4. ส่งสรุป `unknown_program` (`suspicious: false`) — Monitor แท็บ **ไม่รู้จัก** แสดงจำนวนครั้งและจำนวนเครื่อง **ไม่ขึ้นคิวทีละคลิก และไม่หักคะแนน**
5. ไม่ส่งทุกแท็บเบราว์เซอร์ขึ้น Monitor

## หมวดเว็บ UT1

แอดมินเลือกหมวดที่ Monitor → **บล็อกเว็บ** แล้วกดนำเข้า — backend ดึง `https://dsi.ut-capitole.fr/blacklists/download/{category}.tar.gz` เอาไฟล์ `domains` มาใส่ `website_blacklist` สูงสุด 250 โดเมนต่อหมวด (เรียงโดเมนสั้นก่อน) ตั้ง `category` ตามรหัสหมวด และ `source = ut1`

Agent เขียน hosts จากรายการนี้เหมือนโดเมนที่ใส่เอง **ไม่เททั้งไฟล์หลายหมื่นโดเมน**

## สิ่งที่ห้ามทำตอนปิดโปรแกรม

- ไม่เรียก `shutdown` / ไม่ปิดเครื่อง
- ไม่ปิด process ของตัว Agent (`Kinof.Agent`) และ process แม่ (เช่น `dotnet` ตอน `dotnet run`)
- ไม่ปิด process ระบบ Windows (`csrss`, `lsass`, `winlogon`, `svchost`, `explorer`, Defender ฯลฯ)
- ไม่ปิด process session 0 (บริการระบบ)
- ถ้าแอดมินใส่ชื่อ process ระบบในรายการ จะถูกข้ามที่ Agent

ต้องรัน as Administrator จึงจะปิด process ของผู้ใช้อื่นและเขียน hosts ได้

## API ที่ Agent ใช้

```
POST /api/agent/register
POST /api/agent/heartbeat
POST /api/agent/logs
GET  /api/agent/website-blacklist
GET  /api/agent/program-blacklist
GET  /api/agent/program-allowlist
POST /api/agent/session/login
POST /api/agent/session/verify-otp
POST /api/agent/session/resend-otp
POST /api/agent/session/logout
```

ตัวอย่าง log ตอนบล็อกโปรแกรม:

```json
{
  "events": [
    {
      "eventType": "program",
      "at": "2026-09-17T10:00:00+00:00",
      "data": {
        "program": "discord.exe",
        "activity": "บล็อก discord.exe",
        "suspicious": true,
        "matchedPattern": "discord.exe",
        "source": "agent"
      }
    }
  ]
}
```

## วิธีรัน

เปิด PowerShell **Run as administrator**:

```powershell
cd C:\Users\User\Desktop\Kinof-project\windows-agent
dotnet run
```

Backend ต้องฟังพอร์ต 5106 ก่อน รายละเอียด VM / ทดสอบ hosts ดู `windows-agent/README.md`
