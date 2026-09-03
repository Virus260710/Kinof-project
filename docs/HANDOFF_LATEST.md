# Handoff ล่าสุด — KINOF Phase 0 (Email OTP + Face + Booking)

> อัปเดต: 3 ก.ย. 2569
> Workspace: `C:\Users\User\Desktop\Kinof-project`
> GitHub: https://github.com/Virus260710/Kinof-project.git
> Branch: `cursor/phase0-backend-email-otp` (มีงาน local ที่ยังไม่ push)

---

## สรุปสิ่งที่ทำเสร็จแล้ว

### Backend — `backend/Kinof.Api/`

| รายการ | สถานะ |
|--------|--------|
| ASP.NET Core 8 + EF Core + SQLite | ✅ |
| Auth: login, register, verify/resend OTP | ✅ |
| **GET /api/auth/me** | ✅ |
| **POST /api/auth/refresh** | ✅ |
| **POST /api/auth/forgot-password**, **reset-password** | ✅ |
| **POST /api/auth/register/face** | ✅ (รูป → InsightFace → 512-d embedding) |
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
POST /api/auth/forgot-password
POST /api/auth/reset-password
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
| App shell เป็นเจ้าของ TopBar/Sidebar/auth/booking state | ✅ |
| Display name และ booking slots ใช้ source กลาง | ✅ |
| Invitation / schedule / profile score | ⚠️ ยัง mock พร้อม TODO(backend) |
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

# Terminal 3 — Face Service (จำเป็นเมื่อต้อง enroll ใบหน้า)
cd C:\Users\User\Desktop\Kinof-project\face-service
python -m uvicorn app.main:app --host 127.0.0.1 --port 8001
```

เปิด: `http://localhost:5173`

### ผล smoke test ล่าสุด (3 ก.ย. 2569)

- `npm run build` ผ่าน
- Admin login + OTP ผ่าน และมี TopBar จาก App เพียงชั้นเดียว
- Student login + OTP ผ่าน; profile แสดงข้อมูลจาก `auth.user`
- ค้นหาห้องว่างผ่าน API และสร้าง booking จริงสำเร็จสำหรับ 4 ก.ย. 2569 รอบที่ 1
- UserHome อัปเดตรายการจองทันทีหลัง `POST /api/bookings`
- `/forgot-password` และ `/reset-password?token=...` render ได้
- หมายเหตุ: SQLite ที่กำลังใช้งานมีข้อมูลห้องเดิมชื่อ `Lab A`/`Lab B`; source seeder ปัจจุบันกำหนดชื่อห้องแล็บ 1-4

### ทดสอบ Login → OTP

1. Login ด้วย `student` / `Student123!` หรือ `admin` / `Admin123!`
2. ถ้า **ไม่ตั้ง SMTP password** → OTP แสดงใน **console backend**
3. ถ้าตั้ง SMTP แล้ว → OTP ส่งไปอีเมลจริง
4. หลัง OTP สำเร็จ → user ไป `/register/face` ถ้ายังไม่ลงทะเบียนใบหน้า
5. จองห้องต้องเรียก `GET /api/rooms/available` และ `POST /api/bookings` จริง
6. Login admin ต้องมี TopBar เพียงชั้นเดียว
7. ตรวจ build ด้วย `npm run build`

### Face enrollment

- MediaPipe FaceDetector + FaceLandmarker
- Auto-capture เมื่อใบหน้าอยู่กึ่งกลาง + กระพริบตา (liveness)
- Frontend ส่งภาพ JPEG ชั่วคราวไป `POST /api/auth/register/face`
- Backend forward ภาพใน memory ไป FastAPI + InsightFace `buffalo_l`
- เก็บเฉพาะ normalized embedding 512 มิติใน DB และไม่เก็บรูปบน server
- ถ้ากล้องหรือ API error มีปุ่ม retry; ปุ่มข้ามที่วนกลับหน้าเดิมถูกเปลี่ยนเป็น logout

---

## สิ่งที่ยังไม่ทำ

| ลำดับ | งาน |
|-------|-----|
| 1 | Entry OTP สำหรับ Kiosk (`/api/auth/entry-otp`) |
| 2 | ติดตั้ง Python 3.10/3.11 และทดสอบ InsightFace ด้วยกล้องจริงบนเครื่อง |
| 3 | Admin pages เชื่อม API |
| 4 | Invitation API |
| 5 | Schedule API (`GET /api/schedule/me`) และ profile score/history |
| 6 | git commit/push branch ขึ้น GitHub เมื่อผู้ใช้สั่ง |

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
kinof-app/src/utils/displayName.js
kinof-app/src/App.jsx
```
