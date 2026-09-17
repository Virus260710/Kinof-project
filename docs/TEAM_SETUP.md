# KINOF — คู่มือ Setup สำหรับทีม (Handoff)

> อัปเดต: 17 ก.ย. 2569  
> Repo: https://github.com/Virus260710/Kinof-project  
> Branch หลักที่พัฒนาอยู่: `cursor/phase0-backend-email-otp`

---

## 1. ภาพรวมโปรเจกต

**KINOF** = ระบบจัดการห้องแล็บอัจฉริยะ (จองห้อง, เชิญเพื่อน, สแกนหน้า/OTP เข้าห้อง, Admin)

```
React (5173)  →  ASP.NET Core 8 (5106)  →  SQLite (kinof.db)
                      ↓
              Face Service Python (8001)
              Windows Agent (เครื่องแล็บ)
```

| โฟลเดอร์ | คืออะไร |
|----------|---------|
| `kinof-app/` | Frontend React + Vite + Tailwind |
| `backend/Kinof.Api/` | Backend API + Database |
| `face-service/` | สแกนใบหน้า InsightFace (แยก service) |
| `windows-agent/` | Agent บนเครื่องแล็บ (heartbeat, ล็อกอิน, บล็อกเว็บ/โปรแกรม) |
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

เปิด **4 Terminal** แยกกัน:

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

### Terminal 4 — Windows Agent (ต้อง Run as administrator)

ใส่ `Kinof:ApiKey` ใน `windows-agent/appsettings.json` (dev: `dev-agent-key-1`) แล้ว:

```powershell
cd windows-agent
dotnet run
```

→ ล็อกอินบัญชี KINOF บนเครื่องนั้น ที่นั่งจึงเป็น Occupied  
→ บล็อกเว็บผ่าน hosts และปิดโปรแกรมตามแท็บ Monitor **บล็อกโปรแกรม**  
รายละเอียด: `windows-agent/README.md` และ `docs/implementation/AGENT.md`

---

## 6. บัญชีทดสอบ (Seed)

| Username | Password | บทบาท |
|----------|----------|--------|
| `student` | `Student123!` | นักศึกษา |
| `admin` | `Admin123!` | ผู้ดูแล |
| `superadmin1` | `SuperAdmin123!` | Super Admin |

**Login flow:** กรอก user/pass → ระบบส่ง **OTP 6 หลัก** ทางอีเมล

### ส่งอีเมลจริง (Resend SMTP + user-secrets)

ค่า SMTP จริงอยู่ที่ **dotnet user-secrets** ของ `Kinof.Api` — **ห้ามใส่ API key ใน `appsettings.json` / ห้าม commit**  
ถ้าใส่ครบแล้ว **อย่า set `Email:Password` ซ้ำ**

| Key | ค่า |
|-----|-----|
| `Email:SmtpHost` | `smtp.resend.com` |
| `Email:SmtpPort` | `587` |
| `Email:Username` | `resend` |
| `Email:Password` | Resend API key — user-secrets เท่านั้น |
| `Email:FromAddress` | ผู้ส่งที่ verify แล้ว |

ครั้งแรก:

```powershell
cd backend
dotnet user-secrets set "Email:SmtpHost" "smtp.resend.com" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:SmtpPort" "587" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:Username" "resend" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:Password" "re_xxxxxxxx" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:FromAddress" "onboarding@resend.dev" --project .\Kinof.Api\Kinof.Api.csproj
```

รีสตาร์ท Backend หลังเปลี่ยน secrets  
`onboarding@resend.dev` ส่งได้เฉพาะเมลเจ้าของบัญชี Resend — เมลอื่นต้องใช้โดเมนที่ verify แล้ว  
ทดสอบ inbox: `dotnet user-secrets set "Seed:StudentEmail" "your-resend-account@gmail.com" --project .\Kinof.Api\Kinof.Api.csproj` แล้วรีสตาร์ท API

ถ้าส่งสำเร็จ หน้า OTP จะไม่โชว์ `devOtp` (`deliveryMode = smtp`)  
รายละเอียด: `docs/EMAIL_OTP.md`

### OTP ในโหมด Dev เมื่อยังไม่มีรหัส หรือ SMTP ล้ม

ถ้ายังไม่ตั้ง password หรือส่งไม่ถึง → ดู OTP ใน **Terminal Backend**:

```
Development email fallback: login OTP for xxx@... is 123456
```

หรือ

```
Development email fallback (SMTP send failed): login OTP for xxx@... is 123456
```

คำเชิญเพื่อน / Entry OTP / รีเซ็ตรหัส / เชิญแอดมิน ก็ log แบบเดียวกันใน Development  
**Production ไม่มี fallback นี้** — ส่งไม่สำเร็จจะได้ HTTP 503

---

## 7. URL ทดสอบสำคัญ

| หน้า | URL |
|------|-----|
| เว็บหลัก | http://localhost:5173 |
| Login | http://localhost:5173/login |
| จองห้อง | Sidebar → จองห้องแล็บ |
| คำเชิญ | Sidebar → คำเชิญ |
| Kiosk Lab | เปิดจาก **จัดการข้อมูล → ห้องแล็บ** คัดลอกลิงก์เครื่องประตู หรือ `http://localhost:5173/kiosk/{roomId}?key=dev-kiosk-key-1` |
| (คีย์ dev) | `dev-kiosk-key-1` = ห้องแรกตามชื่อ (เช่น Lab A), `dev-kiosk-key-2` = ห้องถัดไป — ครั้งแรกเก็บใน localStorage ของเบราว์เซอร์นั้น |

---

## 8. Flow ทดสอบหลัก

### 8.1 Login + ลงทะเบียนใบหน้า
1. Login `student` → ตอน Development รหัส OTP อยู่หน้าเว็บและ Terminal Backend (ยังไม่ยิง Resend จนกว่าจะขึ้นเครื่องจริง)
2. ถ้ายังไม่ enroll → `/register/face/scan`
3. ต้องเปิด Face Service (Terminal 3)

### 8.2 จองห้อง + เชิญเพื่อน
1. จองห้อง → เลือกวัน/เวลา → เพิ่มเพื่อน (ต้องมี account ในระบบ)
2. Step 3 → **ส่งคำเชิญอัตโนมัติ** → รอเพื่อนตอบรับ
3. เพื่อน login → **คำเชิญ** → ยอมรับ
4. กลับมาที่ผู้จอง → กด **ยืนยันการจอง** เมื่อครบทุกคน

### 8.3 เข้าห้อง Kiosk
1. ต้องมี **ตารางเรียน** หรือ **การจอง confirmed** ในช่วงเวลานั้น
2. เปิด Kiosk พร้อมคีย์เครื่องนั้น (`?key=dev-kiosk-key-N` ครั้งแรก หรือคัดลอกลิงก์จากหน้าแอดมิน) — ผู้ใช้ทั่วไปไม่ต้องกรอกคีย์บนจอสแกน
3. Kiosk → **สแกนใบหน้า** หรือ **OTP สำรอง** (ขอจากเมนู รหัสเข้าห้อง)
4. ได้แค่สิทธิ์เข้าห้อง **ที่นั่งยังไม่ถูกจอง** จนกว่าจะ login บน Agent

---

## 9. Config ที่ควรรู้

### `backend/Kinof.Api/appsettings.json`

| Key | ค่า default | หมายเหตุ |
|-----|-------------|----------|
| `ConnectionStrings:Default` | `kinof.db` | SQLite local |
| `Frontend:BaseUrl` | `http://localhost:5173` | ลิงก์ในอีเมล |
| `FaceService:BaseUrl` | `http://localhost:8001` | ต้องรัน Face Service |
| `Email:*` ใน `appsettings.json` | placeholder (`Password` ว่าง) | ค่าจริงจาก user-secrets (Resend) |
| `Email:SmtpHost` (secrets) | `smtp.resend.com` | ทับค่าใน appsettings |
| `Email:Username` (secrets) | `resend` | Resend SMTP |
| `Email:Password` | **อย่าใส่ใน git** | user-secrets หรือ `Email__Password` เท่านั้น |
| `Jwt:Key` | dev key | **เปลี่ยนใน production** |

รายละเอียด SMTP: `docs/EMAIL_OTP.md`

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
| OTP ไม่มาอีเมล | ตอน Development ตั้งใจไม่ส่งเมล — ดูรหัสบนหน้า OTP / Terminal Backend |
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
- SMTP จริงผ่าน Resend (user-secrets) — Development ถ้าส่งล้มยัง log console ได้
- ลงทะเบียนใบหน้า + Face Service
- จองห้อง + เชิญเพื่อน (รอ accept ก่อน confirm)
- Kiosk OTP + Face scan (ตรวจสิทธิ์เข้าห้อง — ไม่จ่ายที่นั่ง)
- Kiosk device auth (`X-Kiosk-Key` ผูกห้อง · แอดมินสร้าง/เพิกถอน · คีย์ตัวอย่างในโหมด dev)
- ป้ายจำนวนค้างบน Sidebar (ผู้ใช้: คำเชิญ · แอดมิน: ตรวจสอบการใช้งาน / ศูนย์แก้ไขปัญหา)
- Admin: ห้อง, ตารางเรียน, Dashboard, Tracking, Monitor, Export (Excel/CSV)
- คะแนนพฤติกรรม (no-show หักอัตโนมัติ · Agent flagged รอแอดมินตรวจ ดี/แย่)
- Problem Reports
- Windows Agent: heartbeat, ล็อกอินเครื่อง, ออกจากระบบ, ซ่อนถาดระบบ
- บล็อกเว็บบนเครื่องจริง (hosts จากรายการ Monitor)
- บล็อกโปรแกรมบนเครื่องจริง (ปิด process ตาม `process_name` จาก Monitor)
- บล็อกเว็บตามหมวด UT1 + รายการโปรแกรมที่อนุญาต + สรุปโปรแกรมที่ไม่รู้จัก

### ❌ ยังไม่ทำ / ทำบางส่วน
- ทดสอบเดโมครบรอบบนเครื่องนี้ แล้วรอบตรวจ Opus ก่อนพรีเซนต์
- เตรียมขึ้นของจริง: HTTPS, CORS โดเมนจริง, Jwt:Key ใหม่, รหัส seed ใหม่, โดเมน Resend ที่ verify แล้ว
- ติดตั้ง Agent ทีละเครื่อง + Kiosk ที่ประตู (ไม่ใช่ localhost)
- แต่ละเครื่องตั้ง Resend ใน user-secrets เอง — อย่า commit API key

---

## 12. เอกสารอ้างอิงใน repo

| ไฟล์ | เนื้อหา |
|------|---------|
| `docs/EMAIL_OTP.md` | Login/Entry OTP + วิธีตั้ง Resend SMTP |
| `docs/HANDOFF_LATEST.md` | สถานะล่าสุด + API list |
| `docs/implementation/AGENT.md` | Windows Agent ของจริง |
| `windows-agent/README.md` | วิธีรัน / ทดสอบ Agent |
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
- [ ] รัน backend + frontend (Face Service / Agent ตามงาน)
- [ ] Login `student` / `Student123!` — ถ้าตั้ง SMTP password แล้ว OTP มาที่อีเมล ไม่เช่นนั้นดู console
- [ ] เปิด http://localhost:5173 ได้

---

**ติดปัญหา:** อ่าน `docs/HANDOFF_LATEST.md` หรือถามในกลุ่ม พร้อมแนบ screenshot + log จาก Terminal Backend
