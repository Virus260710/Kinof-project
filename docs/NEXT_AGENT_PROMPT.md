# Prompt สำหรับ Agent — Kinof (แชทใหม่)

> **Workspace:** `C:\Users\User\Desktop\Kinof-project`  
> **GitHub:** https://github.com/Virus260710/Kinof-project.git  
> **Branch:** `cursor/phase0-backend-email-otp`  
> อ่านสถานะล่าสุด: `docs/HANDOFF_LATEST.md`  
> **แชท Support / สอบถาม:** ดู `docs/SUPPORT_AGENT_PROMPT.md`

---

## คัดลอก Prompt นี้ไปแชทใหม่

```
ทำต่อโปรเจกต์ KINOF (Smart Lab Management) ที่ C:\Users\User\Desktop\Kinof-project

GitHub: https://github.com/Virus260710/Kinof-project.git
Branch: cursor/phase0-backend-email-otp

=== ทำเสร็จแล้ว (อยู่ในไฟล์แล้ว — อย่าสร้างใหม่) ===

Backend (backend/Kinof.Api/):
- ASP.NET Core 8 + EF Core + SQLite, port http://localhost:5106
- AuthService: login, register, verify-email-otp, resend-email-otp
- EmailSender (MailKit) — dev fallback แสดง OTP ใน console ถ้าไม่มี SMTP password
- JWT + refresh token, OTP 6 หลัก หมดอายุ 10 นาที, resend limit 3/hr
- Migration 15 ตาราง, DbSeeder: student/Student123!, admin/Admin123!
- Commit: 404dd7a Add backend auth and email OTP flow

Frontend (kinof-app/):
- Login.jsx, Register.jsx, OtpVerify.jsx เชื่อม API จริง
- src/api/auth.js, react-router /login /login/otp /register
- User/Admin pages ยังใช้ mock data (BookRoom, Dashboard ฯลฯ)

=== อ่านก่อนทำ ===
docs/HANDOFF_LATEST.md
docs/EMAIL_OTP.md, docs/DATABASE.md, docs/FLOWS.md, docs/UI_AUTH_PHASE.md
backend/README.md

=== งานถัดไป (เรียงลำดับ) ===
1. ทดสอบ login → OTP → เข้าระบบ end-to-end (SMTP หรือ console OTP)
2. Face enrollment: หน้า /register/face, กล้อง live auto-capture (MediaPipe + InsightFace)
3. API /me + refresh token ถ้ายังไม่มี endpoint
4. เชื่อม BookRoom mock → booking API (Phase 3)
5. git status — commit/push ถ้ามีการเปลี่ยนแปลง

=== ข้อห้าม / ข้อกำหนด ===
- OTP ทาง email เท่านั้น — ไม่ใช้ Google Authenticator
- External user สมัครแล้ว active ทันที ไม่ต้อง admin approve
- อย่า copy Tracking Agent จาก smartlab repo — ทำ Phase 1 แยก
- ใช้ component/theme จาก kinof-app ที่มีอยู่
```

---

## การตั้งค่า Cursor แนะนำ

| การตั้งค่า | ค่า |
|-----------|-----|
| Open Folder | `C:\Users\User\Desktop\Kinof-project` |
| Mode | Agent |
| Model | Composer 2.5 Fast (งาน Face scan → Sonnet Thinking) |

---

## Checklist ก่อนเริ่ม

- [ ] Open Folder ชี้ไปที่ `Kinof-project` (ไม่ใช่ smartlab)
- [ ] Branch: `cursor/phase0-backend-email-otp`
- [ ] อ่าน `docs/HANDOFF_LATEST.md`
- [ ] Backend รันได้ที่ port 5106
- [ ] Frontend รันได้ที่ port 5173

---

## รัน dev

```powershell
# Backend
cd C:\Users\User\Desktop\Kinof-project\backend
dotnet run --project .\Kinof.Api\Kinof.Api.csproj

# Frontend
cd C:\Users\User\Desktop\Kinof-project\kinof-app
npm run dev
```

SMTP (optional):
```powershell
dotnet user-secrets set "Email:Password" "APP-PASSWORD" --project backend\Kinof.Api\Kinof.Api.csproj
```
