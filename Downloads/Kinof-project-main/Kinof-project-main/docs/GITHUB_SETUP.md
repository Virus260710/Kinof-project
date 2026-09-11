# ทำต่อจาก GitHub — Kinof-project

> Repo: https://github.com/Virus260710/Kinof-project.git  
> **Workspace:** `C:\Users\User\Desktop\Kinof-project`

---

## สถานะปัจจุบัน (อัปเดต 25 ส.ค. 2569)

| สิ่ง | สถานะ |
|------|--------|
| Clone จาก GitHub | ✅ |
| `docs/` แผนครบ | ✅ |
| `.gitignore` | ✅ |
| Branch `cursor/phase0-backend-email-otp` | ✅ (commit local `404dd7a`) |
| Frontend `kinof-app/` | ✅ Login/Register/OTP เชื่อม API |
| Backend `backend/Kinof.Api/` | ✅ Auth + Email OTP + DB 15 ตาราง |
| Face scan / Kiosk | ⏳ Phase 2 |
| BookRoom → API จริง | ⏳ Phase 3 |
| Tracking Agent | ⏳ repo `smartlab` แยก (Phase 1) |

รายละเอียดเต็ม: **`docs/HANDOFF_LATEST.md`**

---

## โครงสร้าง Repo ปัจจุบัน

```
Kinof-project/
├── docs/                      ← แผน + HANDOFF_LATEST.md + NEXT_AGENT_PROMPT.md
├── backend/
│   └── Kinof.Api/             ← ASP.NET Core 8, SQLite, MailKit OTP
├── kinof-app/                 ← React + Vite + Tailwind
│   ├── src/pages/Login.jsx    ← เชื่อม API จริง
│   ├── src/pages/OtpVerify.jsx
│   ├── src/pages/Register.jsx
│   ├── src/pages/user/        ← Home, BookRoom (mock), Profile, Help
│   └── src/pages/admin/       ← Dashboard (mock), Monitor, Export
└── README.md
```

---

## เริ่มทำต่อ

### 1. เปิด Cursor

```
File → Open Folder → C:\Users\User\Desktop\Kinof-project
```

- Branch: `cursor/phase0-backend-email-otp`
- Mode: **Agent** | Model: **Composer 2.5 Fast**

### 2. คัดลอก Prompt

เปิด `docs/NEXT_AGENT_PROMPT.md` แล้ว copy block ในแชทใหม่

### 3. รัน dev

```powershell
# Backend → http://localhost:5106
cd C:\Users\User\Desktop\Kinof-project\backend
dotnet run --project .\Kinof.Api\Kinof.Api.csproj

# Frontend → http://localhost:5173
cd C:\Users\User\Desktop\Kinof-project\kinof-app
npm run dev
```

Seed: `student`/`Student123!`, `admin`/`Admin123!`

---

## ลำดับงานถัดไป

| ลำดับ | งาน | Phase |
|-------|-----|-------|
| 1 | ทดสอบ OTP flow end-to-end | 0 |
| 2 | Face enrollment `/register/face` | 0→2 |
| 3 | Tracking Agent (repo แยก) | 1 |
| 4 | Kiosk face + entry OTP | 2 |
| 5 | BookRoom / Dashboard → API | 3 |

---

## ความต่าง UI: Repo vs Mockup

| | Repo ปัจจุบัน | Mockup ที่ออกแบบ |
|---|-------------|------------------|
| Login | เชื่อม API + OTP | Split-screen KN/KINOF |
| OTP | ✅ OtpVerify.jsx | ✅ ตรงแผน |
| Register | ✅ ฟอร์ม + OTP | + ถ่ายหน้า (ยังไม่ทำ) |
| จองห้อง | mock | เชื่อม API Phase 3 |

**แนะนำ:** ใช้ theme/component จาก repo + เพิ่ม face scan ตาม mockup
