# ทำต่อจาก GitHub — Kinof-project

> Repo: https://github.com/Virus260710/Kinof-project.git

## สรุป: **เอามาทำต่อได้** ✅

Repo มี **Frontend React (mock UI)** อยู่แล้ว — ยังไม่มี Backend, Database, OTP, Face scan

---

## สิ่งที่มีใน Repo แล้ว

```
Kinof-project/
├── kinof-app/                 ← React + Vite + Tailwind
│   ├── src/pages/Login.jsx    ← Login (mock — fakeLogin)
│   ├── src/pages/user/        ← Home, BookRoom, Profile, Help
│   ├── src/pages/admin/       ← Dashboard, Monitor, Export, HelpCenter
│   └── src/data/mockData.js   ← ข้อมูลจำลอง
└── README.md
```

| มีแล้ว | ยังไม่มี |
|--------|----------|
| Login UI (เลือก user/admin) | Backend API |
| จองห้อง (mock) | Database |
| Admin dashboard (mock) | Email OTP |
| Tailwind + theme KINOF | Register / Face scan |
| | เชื่อม API จริง |

---

## เริ่มทำต่อ (แนะนำ)

### 1. Clone repo

```powershell
cd C:\Users\User\Desktop
git clone https://github.com/Virus260710/Kinof-project.git
cd Kinof-project
git checkout -b cursor/phase0-backend-email-otp
```

### 2. Copy docs จาก projectfinal (แผนที่วางไว้)

```powershell
Copy-Item -Recurse -Force "C:\Users\User\Desktop\projectfinal\docs" "C:\Users\User\Desktop\Kinof-project\docs"
```

### 3. เปิด Cursor

- **Open Folder:** `C:\Users\User\Desktop\Kinof-project`
- **Mode:** Agent | **Model:** Composer 2.5 Fast

### 4. ลำดับงานต่อ

| ลำดับ | งาน | หมายเหตุ |
|-------|-----|----------|
| 1 | เพิ่ม `.gitignore` (node_modules) | repo ตอนนี้ commit node_modules ไว้ |
| 2 | สร้าง `backend/` + DB 15 ตาราง | ตาม docs/DATABASE.md |
| 3 | เชื่อม Login.jsx → API จริง | แทน fakeLogin |
| 4 | เพิ่มหน้า `/login/otp` (Email OTP) | ตาม docs/EMAIL_OTP.md |
| 5 | Register + Face scan | Phase 0 ต่อ |
| 6 | เชื่อม BookRoom → API จองจริง | Phase 3 |

---

## ความต่าง UI: Repo vs Mockup ที่ออกแบบ

| | GitHub Repo | Mockup ที่ส่งมา |
|---|-------------|----------------|
| Login | เลือก user/admin ก่อน → card เล็ก | Split-screen ซ้าย KN/KINOF |
| OTP | ยังไม่มี | หน้า OTP email |
| Register | ลิงก์อย่างเดียว | Flow สมัคร + ถ่ายหน้า |

**แนะนำ:** ใช้ **component/theme จาก repo** + ปรับ Login ให้ใกล้ mockup + เพิ่ม OTP/Register

---

## Prompt สำหรับ Agent (Clone repo นี้)

```
ทำต่อจาก GitHub repo Kinof-project

Path: C:\Users\User\Desktop\Kinof-project
Remote: https://github.com/Virus260710/Kinof-project.git

อ่าน docs/ (copy จาก projectfinal):
- docs/EMAIL_OTP.md, DATABASE.md, UI_AUTH_PHASE.md, FLOWS.md

มี frontend แล้ว: kinof-app/ (React mock)

ทำ:
1. เพิ่ม .gitignore (node_modules, bin, obj, *.db)
2. สร้าง backend/ ASP.NET Core + EF Core + SQLite (15 ตาราง)
3. Auth API: login → ส่ง Email OTP, verify-email-otp
4. ปรับ kinof-app/src/pages/Login.jsx เชื่อม API จริง
5. เพิ่มหน้า OtpVerify (Email OTP) + react-router
6. ยังไม่ทำ Face scan (รอบ 2)

ไม่ copy Tracking Agent จาก smartlab
```

---

## โครงสร้างเป้าหมาย

```
Kinof-project/
├── docs/              ← จาก projectfinal
├── kinof-app/         ← frontend มีแล้ว (ปรับ + เพิ่ม OTP)
├── backend/           ← สร้างใหม่
│   └── Kinof.Api/
└── .gitignore
```
