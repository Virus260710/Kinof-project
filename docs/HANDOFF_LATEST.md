# Handoff ล่าสุด — KINOF Phase 0 (Email OTP + Face + Booking)

> อัปเดต: 25 ส.ค. 2569  
> Workspace: `C:\Users\User\Desktop\Kinof-project`  
> GitHub: https://github.com/Virus260710/Kinof-project.git  
> Branch: `cursor/phase0-backend-email-otp`

---

## สรุปสิ่งที่ทำเสร็จแล้ว

### Backend — `backend/Kinof.Api/`

| รายการ | สถานะ |
|--------|--------|
| ASP.NET Core 8 + EF Core + SQLite | ✅ |
| Auth: login, register, verify/resend OTP | ✅ |
| **GET /api/auth/me** | ✅ |
| **POST /api/auth/refresh** | ✅ |
| **POST /api/auth/register/face** | ✅ (512-d embedding) |
| **GET /api/rooms**, **GET /api/rooms/available** | ✅ |
| **GET /api/bookings/me**, **POST /api/bookings** | ✅ |
| Email OTP (MailKit + console fallback) | ✅ |
| JWT + refresh token | ✅ |
| DbSeeder: student/admin + ห้องแล็บ 1-4 | ✅ |
| Port: **`http://localhost:5106`** | ✅ |

**Endpoints ทั้งหมด:**

```
GET  /api/health
POST /api/auth/register
POST /api/auth/login
POST /api/auth/verify-email-otp
POST /api/auth/resend-email-otp
POST /api/auth/refresh
GET  /api/auth/me                    [JWT]
POST /api/auth/register/face         [JWT]
GET  /api/rooms                      [JWT]
GET  /api/rooms/available            [JWT]
GET  /api/bookings/me                [JWT]
POST /api/bookings                   [JWT]
```

### Frontend — `kinof-app/`

| รายการ | สถานะ |
|--------|--------|
| Login / Register / OTP verify | ✅ |
| **Face enrollment** `/register/face`, `/scan`, `/success` | ✅ |
| MediaPipe auto-capture + blink liveness | ✅ |
| Auth client + auto refresh token | ✅ |
| **BookRoom → API จริง** | ✅ |
| UserHome/Profile แสดงข้อมูลจาก auth | ✅ |
| Admin pages | ⚠️ ยัง mock |

---

## วิธีรัน

```powershell
# Terminal 1 — Backend
cd C:\Users\User\Desktop\Kinof-project\backend
dotnet run --project .\Kinof.Api\Kinof.Api.csproj

# Terminal 2 — Frontend
cd C:\Users\User\Desktop\Kinof-project\kinof-app
npm install
npm run dev
```

เปิด: `http://localhost:5173`

### ทดสอบ Login → OTP

1. Login ด้วย `student` / `Student123!` หรือ `admin` / `Admin123!`
2. ถ้า **ไม่ตั้ง SMTP password** → OTP แสดงใน **console backend**
3. ถ้าตั้ง SMTP แล้ว → OTP ส่งไปอีเมลจริง
4. หลัง OTP สำเร็จ → user ไป `/register/face` ถ้ายังไม่ลงทะเบียนใบหน้า

### Face enrollment

- MediaPipe FaceDetector + FaceLandmarker
- Auto-capture เมื่อใบหน้าอยู่กึ่งกลาง + กระพริบตา (liveness)
- ส่ง embedding 512 มิติไป `POST /api/auth/register/face`
- **หมายเหตุ:** embedding จาก landmarks เป็น interim — production ควรใช้ InsightFace service (Phase 2)

---

## สิ่งที่ยังไม่ทำ

| ลำดับ | งาน |
|-------|-----|
| 1 | Forgot password API + UI |
| 2 | Entry OTP สำหรับ Kiosk (`/api/auth/entry-otp`) |
| 3 | InsightFace Python service (embedding จริง) |
| 4 | Admin pages เชื่อม API |
| 5 | Schedule API (`GET /api/schedule/me`) |
| 6 | git push branch ขึ้น GitHub |

---

## ข้อกำหนดสำคัญ

- OTP ทาง **email เท่านั้น** — ไม่ใช้ Google Authenticator
- External users สมัครแล้ว **active ทันที**
- **อย่า copy Tracking Agent** จาก smartlab repo
- Kiosk flow (Phase 2): face ผ่าน → เข้าได้; ไม่ผ่าน → entry OTP

---

## ไฟล์สำคัญ

```
backend/Kinof.Api/Services/AuthService.cs
backend/Kinof.Api/Services/BookingService.cs
kinof-app/src/hooks/useFaceCapture.js
kinof-app/src/pages/face/
kinof-app/src/api/auth.js
kinof-app/src/api/bookings.js
kinof-app/src/App.jsx
```
