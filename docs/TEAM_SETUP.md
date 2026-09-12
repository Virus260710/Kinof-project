# KINOF — คู่มือ Setup สำหรับทีม (Handoff)

> อัปเดต: 10 ก.ย. 2569  
> Repo: https://github.com/Virus260710/Kinof-project  
> Branch หลักที่พัฒนาอยู่: `cursor/phase0-backend-email-otp`

---

## 1. ภาพรวมโปรเจกต

**KINOF** = ระบบจัดการห้องแล็บอัจฉริยะ (จองห้อง, เชิญเพื่อน, สแกนหน้า/OTP เข้าห้อง, Admin)

```
React (5173)  →  ASP.NET Core 8 (5106)  →  SQLite (kinof.db)
                      ↓
              Face Service Python (8001)
```

| โฟลเดอร์ | คืออะไร |
|----------|---------|
| `kinof-app/` | Frontend React + Vite + Tailwind |
| `backend/Kinof.Api/` | Backend API + Database |
| `face-service/` | สแกนใบหน้า InsightFace (แยก service) |
| `docs/` | เอกสารแผน, flow, schema |

---

## 2. สิ่งที่ต้องติดตั้งก่อน (Prerequisites)

| โปรแกรม | เวอร์ชัน | ตรวจสอบ |
|---------|---------|---------|
| **Git** | ล่าสุด | `git --version` |
| **Node.js** | 20 LTS ขึ้นไป | `node --version` |
| **npm** | มากับ Node | `npm --version` |
| **.NET SDK** | **8.0** | `dotnet --version` |
| **Python** | **3.10 หรือ 3.11** (64-bit) | `py -3.11 --version` |

> Face Service ต้องใช้ Python 3.10/3.11 — ไม่รองรับ 3.12+ ดีในบางเครื่อง

---

## 3. Clone โปรเจกต

```powershell
git clone https://github.com/Virus260710/Kinof-project.git
cd Kinof-project
git checkout cursor/phase0-backend-email-otp
```

เปิดใน **Cursor / VS Code** ที่ root โฟลเดอร์ `Kinof-project`

---

## 4. Setup ครั้งแรก (One-time)

### 4.1 Frontend

```powershell
cd kinof-app
npm install
```

### 4.2 Backend

```powershell
cd backend
dotnet restore
dotnet ef database update --project .\Kinof.Api\Kinof.Api.csproj
```

> ถ้าไม่มี `dotnet ef` ติดตั้งก่อน:
> ```powershell
> dotnet tool install --global dotnet-ef
> ```

Database จะถูกสร้างที่ `backend/Kinof.Api/kinof.db` (SQLite)

### 4.3 Face Service

```powershell
cd face-service
py -3.11 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
```

> ครั้งแรก InsightFace จะดาวน์โหลดโมเดล `buffalo_l` (~300MB) — ต้องมีเน็ต

---

## 5. รันระบบ (ทุกครั้งที่ dev)

เปิด **3 Terminal** แยกกัน:

### Terminal 1 — Backend

```powershell
cd backend
dotnet run --project .\Kinof.Api\Kinof.Api.csproj
```

→ http://localhost:5106

### Terminal 2 — Frontend

```powershell
cd kinof-app
npm run dev
```

→ http://localhost:5173

### Terminal 3 — Face Service (สำหรับลงทะเบียน/สแกนหน้า Kiosk)

```powershell
cd face-service
.\.venv\Scripts\Activate.ps1
python -m uvicorn app.main:app --host 127.0.0.1 --port 8001
```

→ http://localhost:8001/health ต้องได้ `{"status":"ready"}`

---

## 6. บัญชีทดสอบ (Seed)

| Username | Password | บทบาท |
|----------|----------|--------|
| `student` | `Student123!` | นักศึกษา |
| `admin` | `Admin123!` | ผู้ดูแล |
| `superadmin1` | `SuperAdmin123!` | Super Admin |

**Login flow:** กรอก user/pass → ระบบส่ง **OTP 6 หลัก** ทางอีเมล

### OTP ในโหมด Dev (สำคัญ!)

ถ้า SMTP ส่งอีเมลไม่ถึง (ปกติใน dev) → ดู OTP ใน **Terminal Backend**:

```
Development email fallback (SMTP send failed): login OTP for xxx@... is 123456
```

คำเชิญเพื่อน / Entry OTP ก็ log แบบเดียวกัน

---

## 7. URL ทดสอบสำคัญ

| หน้า | URL |
|------|-----|
| เว็บหลัก | http://localhost:5173 |
| Login | http://localhost:5173/login |
| จองห้อง | Sidebar → จองห้องแล็บ |
| คำเชิญ | Sidebar → คำเชิญ |
| Kiosk Lab A | http://localhost:5173/kiosk/AB082138-C378-4E1B-B7F3-9043D882252A |
| Kiosk Lab B | http://localhost:5173/kiosk/F4D1BF2F-2483-476A-B04C-29E6862B6367 |

---

## 8. Flow ทดสอบหลัก

### 8.1 Login + ลงทะเบียนใบหน้า
1. Login `student` → OTP จาก Terminal Backend
2. ถ้ายังไม่ enroll → `/register/face/scan`
3. ต้องเปิด Face Service (Terminal 3)

### 8.2 จองห้อง + เชิญเพื่อน
1. จองห้อง → เลือกวัน/เวลา → เพิ่มเพื่อน (ต้องมี account ในระบบ)
2. Step 3 → **ส่งคำเชิญอัตโนมัติ** → รอเพื่อนตอบรับ
3. เพื่อน login → **คำเชิญ** → ยอมรับ
4. กลับมาที่ผู้จอง → กด **ยืนยันการจอง** เมื่อครบทุกคน

### 8.3 เข้าห้อง Kiosk
1. ต้องมี **ตารางเรียน** หรือ **การจอง confirmed** ในช่วงเวลานั้น
2. Kiosk → **สแกนใบหน้า** หรือ **OTP สำรอง** (ขอจากเมนู รหัสเข้าห้อง)

---

## 9. Config ที่ควรรู้

### `backend/Kinof.Api/appsettings.json`

| Key | ค่า default | หมายเหตุ |
|-----|-------------|----------|
| `ConnectionStrings:Default` | `kinof.db` | SQLite local |
| `Frontend:BaseUrl` | `http://localhost:5173` | ลิงก์ในอีเมล |
| `FaceService:BaseUrl` | `http://localhost:8001` | ต้องรัน Face Service |
| `Email:*` | SMTP Gmail | dev มัก fallback ไป console |
| `Jwt:Key` | dev key | **เปลี่ยนใน production** |

### Frontend API URL

Default: `http://localhost:5106`  
Override ได้ด้วย `.env` ใน `kinof-app/`:

```env
VITE_API_URL=http://localhost:5106
```

---

## 10. ปัญหาที่พบบ่อย

| อาการ | วิธีแก้ |
|--------|--------|
| Port 8001 ถูกใช้แล้ว | `netstat -ano \| findstr :8001` แล้ว `taskkill /PID xxx /F` |
| Face Service connect ไม่ได้ | ตรวจ Terminal 3 + `http://localhost:8001/health` |
| OTP ไม่มาอีเมล | ดู log ใน Terminal Backend (dev mode) |
| API 404 หลัง pull โค้ดใหม่ | **Restart Backend** |
| จองค้าง step 3 | Refresh หรือล้าง `sessionStorage` key `kinofBookRoomDraft` |
| `No module named uvicorn` | รัน `pip install -r requirements.txt` ใน venv |

### ล้างข้อมูลจองใน DB (dev)

```powershell
py -3.11 -c "
import sqlite3
c = sqlite3.connect(r'backend\Kinof.Api\kinof.db')
for t in ['notifications','invitations','group_members','booking_groups','bookings']:
    c.execute(f'DELETE FROM {t}')
c.commit()
print('cleared bookings')
"
```

---

## 11. สิ่งที่ทำเสร็จแล้ว vs ยังไม่ทำ

### ✅ ทำแล้ว
- Auth (Login/Register/OTP/Reset password)
- ลงทะเบียนใบหน้า + Face Service
- จองห้อง + เชิญเพื่อน (รอ accept ก่อน confirm)
- Kiosk OTP + Face scan
- Admin: ห้อง, ตารางเรียน, Dashboard, Tracking, Monitor
- Problem Reports

### ❌ ยังไม่ทำ / mock
- Admin Export (Excel/CSV)
- Windows Tracking Agent
- Behavior score
- Notification badge บน Sidebar

---

## 12. เอกสารอ้างอิงใน repo

| ไฟล์ | เนื้อหา |
|------|---------|
| `docs/HANDOFF_LATEST.md` | สถานะล่าสุด + API list |
| `docs/AUTH_ADAPTIVE.md` | Flow สแกนหน้า + OTP สำรอง |
| `docs/DATABASE.md` | Schema ฐานข้อมูล |
| `docs/FLOWS.md` | User flow 3 กลุ่ม |
| `face-service/README.md` | Face Service |

---

## 13. Git workflow แนะนำ

```powershell
git pull origin cursor/phase0-backend-email-otp
# ทำงาน...
git add <files>
git commit -m "คำอธิบายสั้นๆ"
git push origin cursor/phase0-backend-email-otp
```

Branch `main` ยังไม่ merge งานล่าสุด — **ใช้ branch `cursor/phase0-backend-email-otp` เป็นหลัก**

---

## 14. Checklist วันแรก (5 นาที)

- [ ] Clone + checkout branch
- [ ] `npm install` ใน kinof-app
- [ ] `dotnet restore` + `dotnet ef database update`
- [ ] Setup face-service venv + pip install
- [ ] รัน 3 Terminal
- [ ] Login `student` / `Student123!` + OTP จาก console
- [ ] เปิด http://localhost:5173 ได้

---

**ติดปัญหา:** อ่าน `docs/HANDOFF_LATEST.md` หรือถามในกลุ่ม พร้อมแนบ screenshot + log จาก Terminal Backend
