# Model & Technology Recommendations — Smart Lab

## สรุปสั้นๆ

| Phase | งาน | ใช้ AI Model ไหม | แนะนำ |
|-------|-----|-----------------|-------|
| **Phase 1 (ตอนนี้)** | Tracking Agent | **ไม่ใช้** | C# + WMI + hosts file |
| Phase 2 | Face Recognition | **ใช่** | InsightFace buffalo_l |
| Phase 3 | 2FA | ไม่ใช้ AI | **Email OTP** (MailKit SMTP) |
| Phase 4 | QR Booking | ไม่ใช้ | JWT + qrcode |

---

## Phase 1: Tracking Agent — ไม่ต้องใช้ AI

### Process Monitoring
- **Windows:** `System.Management` (WMI) — `Win32_ProcessStartTrace` / polling
- **Alternative:** `System.Diagnostics.Process` diff ทุก N วินาที (ง่ายกว่า, ใช้ CPU มากกว่าเล็กน้อย)

### Power Events
- **Windows Event Log:** `System` log, Event ID 6005 (boot), 6006 (shutdown), 1074 (user shutdown)

### Website Blocking
- **hosts file** redirect ไป `127.0.0.1` — ไม่ต้อง proxy
- Admin ส่ง blacklist ผ่าน API → Agent sync hosts และรายการโปรแกรมตามรอบที่ตั้งไว้
- **แผน:** เว็บห้ามตามหมวดจาก [UT1](https://dsi.ut-capitole.fr/blacklists/index_en.php) ไม่ทำ allowlist ทั้งเว็บ ไม่ใช้ AI จัดหมวด ไม่เททั้งไฟล์ลง hosts — **ทำแล้ว:** แอดมินเลือกหมวดแล้วระบบนำเข้าโดเมนจำกัดจำนวนต่อหมวด ซิงค์ `website_blacklist.category`
- **บล็อกโปรแกรม:** ตาราง `program_blacklist` + หน้า Monitor — Agent ดึงตาม heartbeat แล้วปิด process ที่ตรง `process_name` (ไม่ปิดเครื่อง / ไม่ปิดตัว Agent / ไม่ปิด process ระบบ)
- **แผนโปรแกรม:** รายการอนุญาตของแล็บ + สรุปของที่ไม่รู้จัก ไม่ขึ้นคิวทีละคลิก — **ทำแล้ว:** `program_allowlist` + แท็บ Monitor ไม่รู้จัก

### Data Storage
- **Backend dev:** SQLite + EF Core
- **Backend prod:** PostgreSQL
- **Agent local queue:** SQLite (offline resilience)

### Real-time Admin (Phase 5)
- SignalR หรือ Socket.io — ไม่ใช่ AI

---

## Phase 2: Face Recognition — ใช้ AI

### แนะนำหลัก: InsightFace (buffalo_l)

```
Python FastAPI microservice
├── insightface.app.FaceAnalysis(name='buffalo_l')
├── det_size=(640, 640)
├── embedding: 512 dimensions
└── match threshold: cosine similarity >= 0.4
```

**ทำไมไม่ใช้ cloud (Azure/AWS):**
- ข้อมูลใบหน้านักศึกษา — PDPA concern
- ต้อง offline ได้ในห้อง lab
- ไม่มีค่าใช้จ่าย API

**Hardware:**
- Inference: CPU ได้ (100–300ms/frame)
- Train/enroll: ไม่ต้อง train — ใช้ pre-trained model

---

## Phase 3: 2FA — ไม่ใช้ AI

- **RFC 6238 TOTP** — ~~Google Authenticator~~ → ใช้ **Email OTP** แทน (ดู EMAIL_OTP.md)
- Library: `OtpNet` (C#) หรือ `pyotp` (Python)

---

## Tech Stack สรุป

```
Phase 1 (Tracking):
  Agent:     C# .NET 8 Worker Service (net8.0-windows)
  Backend:   ASP.NET Core 8 Minimal API + SQLite
  Admin:     Static HTML/JS (wwwroot)

Phase 2+ (Full System):
  Face Svc:  Python FastAPI + InsightFace
  Backend:   ASP.NET Core หรือ FastAPI + PostgreSQL
  Frontend:  React + TypeScript (ต้องติด Node.js)
  Agent:     (มีแล้วจาก Phase 1)
```
