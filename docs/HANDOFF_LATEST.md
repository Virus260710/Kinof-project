# Handoff ล่าสุด — KINOF Phase 3C (Kiosk Face entry) — Phase 3 ครบแล้ว

> อัปเดต: 17 ก.ย. 2569
> Workspace: `C:\Users\User\Desktop\Kinof-project`
> GitHub: https://github.com/Virus260710/Kinof-project.git
> Branch: `cursor/phase0-backend-email-otp`
> **อย่า commit/push จนกว่า user จะสั่ง**
> เฝ้าเว็บ/โปรแกรมตามนโยบายแล้ว: นำเข้าหมวด UT1 (จำกัดโดเมนต่อหมวด) · รายการโปรแกรมที่อนุญาตในแล็บ · สรุปโปรแกรมที่ไม่รู้จัก ไม่ขึ้นคิว Monitor ทีละแถว
> Kiosk ประตูใช้ `X-Kiosk-Key` ผูกห้อง (แนวเดียวกับ `X-Agent-Key` ที่ผูกที่นั่ง) — ไม่จ่ายที่นั่งที่ประตู

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
| **GET/accept/decline Invitation API + user search** | ✅ |
| **Problem Reports API (user/admin/status/images)** | ✅ |
| Email OTP (MailKit SMTP จริง + console fallback เฉพาะ Development) | ✅ |
| JWT + refresh token | ✅ |
| DbSeeder: student/admin + ห้องแล็บ 1-4 | ✅ |
| Superadmin 3 คน + Admin/Schedule/Room CRUD + Excel import | ✅ |
| Booking overlap กับตารางเรียน (block ทั้งรอบ) | ✅ |
| `GET /api/schedule/me` + pending auto-link | ✅ |
| Admin audit log 90 วัน | ✅ |
| **Entry OTP request/resend/active (email, JWT)** | ✅ Phase 3A |
| **Kiosk API สาธารณะ: ข้อมูลห้อง + verify entry OTP** | ✅ Phase 3B |
| **Kiosk device auth: `X-Kiosk-Key` ผูกห้อง (สร้าง/เพิกถอนโดยแอดมิน)** | ✅ |
| **EntryService: ตรวจสิทธิ์ (schedule/booking) → assign seat → `access_logs`** | ✅ Phase 3B |
| **Rate limit Kiosk: ล้มเหลว 5 ครั้ง / 15 นาที ต่อห้อง แยก scope `otp` / `face`** | ✅ Phase 3B/3C |
| **FaceMatchingService: identify 1:N ด้วย cosine similarity (threshold 0.5)** | ✅ Phase 3C |
| **Kiosk API สาธารณะ: verify-face → EntryService (`AuthMethod.Face`)** | ✅ Phase 3C |
| **POST /api/admin/exports** | ✅ Excel/CSV จาก `agent_logs` ตามช่วงวันที่กรุงเทพ |
| **GET /api/behavior** | ✅ คะแนน 100 · no-show หักอัตโนมัติ · flagged รอแอดมินตรวจก่อนหัก |
| **นำเข้าหมวดเว็บ UT1** | ✅ แอดมินเลือกหมวด → โดเมนเข้า `website_blacklist.category` · จำกัด 250/หมวด (สูงสุด 400) |
| **รายการโปรแกรมที่อนุญาต** | ✅ ตาราง `program_allowlist` + หน้า Monitor |
| **สรุปโปรแกรมที่ไม่รู้จัก** | ✅ Agent ส่ง `unknown_program` รวมครั้ง/เครื่อง — ไม่ขึ้นคิวน่าสงสัย ไม่หักคะแนน |
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
POST /api/auth/entry-otp/request     [JWT]
POST /api/auth/entry-otp/resend      [JWT]
GET  /api/auth/entry-otp/active      [JWT]
GET  /api/kiosk/rooms/{roomId}       [X-Kiosk-Key ผูกห้อง]
POST /api/kiosk/entry/verify-otp     [X-Kiosk-Key ผูกห้อง]
POST /api/kiosk/entry/verify-face    [X-Kiosk-Key ผูกห้อง]
GET  /api/rooms                      [JWT]
GET  /api/rooms/available            [JWT]
GET  /api/bookings/me                [JWT]
POST /api/bookings                   [JWT]
GET  /api/invitations/users          [JWT]
GET  /api/invitations/me             [JWT]
POST /api/invitations/{id}/accept    [JWT]
POST /api/invitations/{id}/decline   [JWT]
POST /api/problem-reports            [JWT]
GET  /api/problem-reports/me         [JWT]
GET  /api/problem-reports            [JWT, admin]
GET  /api/problem-reports/{id}       [JWT]
PATCH /api/problem-reports/{id}/status [JWT, admin]
GET  /api/problem-reports/{id}/images/{imageId} [JWT]
GET  /api/schedule/me                [JWT]
GET  /api/behavior                   [JWT] คะแนนพฤติกรรม + ประวัติหักคะแนน
GET  /api/nav-badges                 [JWT] จำนวนค้างบน Sidebar
GET  /api/admin/behavior/reviews     [JWT, admin+] คิวต้องสงสัยรอตรวจ
POST /api/admin/behavior/reviews/{id}/clear    [JWT, admin+] ดี — ไม่หัก
POST /api/admin/behavior/reviews/{id}/penalize [JWT, admin+] แย่ — บล็อกแล้วหัก
GET  /api/admin/users                [JWT, superadmin]
POST /api/admin/users                [JWT, superadmin]
PUT  /api/admin/users/{id}           [JWT, superadmin]
POST /api/admin/users/{id}/disable   [JWT, superadmin]
POST /api/admin/users/{id}/enable    [JWT, superadmin]
POST /api/admin/users/{id}/resend-invite [JWT, superadmin]
GET  /api/admin/audit-logs           [JWT, superadmin]
GET/POST/PUT/DELETE /api/admin/rooms [JWT, admin+]
GET/POST /api/admin/kiosk-devices    [JWT, admin+]  สร้างได้ apiKey ครั้งเดียว
POST /api/admin/kiosk-devices/{id}/revoke [JWT, admin+]
GET/POST/PUT/DELETE /api/admin/schedules [JWT, admin+]
POST /api/admin/schedules/import/preview [JWT, admin+]
POST /api/admin/schedules/import/confirm [JWT, admin+]
GET  /api/admin/schedules/template   [JWT, admin+]
GET  /api/admin/dashboard            [JWT, admin+]
GET  /api/admin/tracking/summary|rooms|seats|activity [JWT, admin+]
POST /api/admin/exports              [JWT, admin+]  Excel/CSV ตามช่วงวันที่กรุงเทพ
GET/POST/DELETE /api/admin/tracking/website-blacklist [JWT, admin+]
GET  /api/admin/tracking/website-blacklist/categories [JWT, admin+] หมวด UT1 + จำนวนที่นำเข้า
POST /api/admin/tracking/website-blacklist/import [JWT, admin+] นำเข้าโดเมนหมวด UT1 (จำกัดต่อหมวด)
DELETE /api/admin/tracking/website-blacklist/categories/{category} [JWT, admin+] นำหมวด UT1 ออก
GET/POST/DELETE /api/admin/tracking/program-blacklist [JWT, admin+]
GET/POST/DELETE /api/admin/tracking/program-allowlist [JWT, admin+]
GET  /api/admin/tracking/unknown-programs [JWT, admin+] สรุปโปรแกรมที่ไม่รู้จัก
POST /api/agent/register | heartbeat | logs          [X-Agent-Key]
GET  /api/agent/website-blacklist | program-blacklist | program-allowlist [X-Agent-Key]
POST /api/agent/session/login | verify-otp | resend-otp | logout [X-Agent-Key]
```

### Frontend — `kinof-app/`

| รายการ | สถานะ |
|--------|--------|
| Login / Register / OTP verify | ✅ |
| **Face enrollment** `/register/face`, `/scan`, `/success` | ✅ |
| MediaPipe auto-capture + blink liveness | ✅ |
| Auth client + auto refresh token | ✅ |
| Auth persistence: `sessionStorage["kinofAuth"]` | ✅ |
| **BookRoom → API จริง** | ✅ |
| **Invitation → API จริง (ค้นหาผู้ใช้/ตอบรับ/ปฏิเสธ)** | ✅ |
| **UserHelp + AdminHelpCenter → Problem Reports API** | ✅ |
| UserHome/Profile แสดงข้อมูลจาก auth | ✅ |
| **Sidebar ป้ายจำนวนค้าง (คำเชิญ / Monitor / ศูนย์ปัญหา)** | ✅ |
| **Entry OTP หน้า `/entry-otp` + sidebar user + การ์ดหน้าหลัก/โปรไฟล์** | ✅ Phase 3A |
| **Kiosk `/kiosk/:roomId` — fullscreen นอก shell, ไม่ต้อง login (OTP path)** | ✅ Phase 3B |
| **Kiosk ส่ง `X-Kiosk-Key` จาก localStorage / `?key=` ครั้งแรก ไม่กรอกบนจอสแกน** | ✅ |
| **Kiosk สแกนใบหน้า: กล้อง + auto-capture ไม่ต้องกระพริบตา → verify-face** | ✅ Phase 3C |
| **Kiosk fallback: สแกนไม่ผ่าน 3 ครั้ง → ไปหน้า OTP อัตโนมัติ** | ✅ Phase 3C |
| Display name และ booking slots ใช้ source กลาง | ✅ |
| จัดการข้อมูล: ตารางเรียน / ห้อง / ผู้ดูแล (superadmin) + import Excel | ✅ |
| **จัดการข้อมูล → ห้องแล็บ: สร้าง/เพิกถอนอุปกรณ์ Kiosk ต่อห้อง (apiKey ครั้งเดียว)** | ✅ |
| Profile ตารางเรียน + BookRoom ติดเรียน จาก `GET /api/schedule/me` | ✅ |
| Log แอดมิน (superadmin only) | ✅ |
| Admin Dashboard / Monitor / Tracking | ✅ |
| **Admin Export → Excel/CSV ไฟล์จริงจาก `POST /api/admin/exports`** | ✅ |
| **คะแนนพฤติกรรมที่โปรไฟล์จาก `GET /api/behavior`** | ✅ |
| **Monitor แท็บน่าสงสัย → แอดมินกด ดี/แย่** | ✅ |
| **Monitor นำเข้าหมวด UT1 / อนุญาตโปรแกรม / สรุปไม่รู้จัก** | ✅ |

### Windows Agent — `windows-agent/`

| รายการ | สถานะ |
|--------|--------|
| Heartbeat + ล็อกอินบัญชี KINOF + OTP + ออกจากระบบเครื่องนี้ | ✅ |
| ที่นั่ง Occupied เมื่อ login บน Agent — Kiosk ไม่จ่ายที่นั่ง | ✅ |
| ซ่อนถาดระบบ (ปิดหน้าต่างแล้วยังทำงาน) | ✅ |
| บล็อกเว็บผ่าน hosts จาก `website_blacklist` | ✅ |
| นำเข้าหมวด UT1 แล้วซิงค์ `category` (จำกัดโดเมนต่อหมวด ไม่เททั้งไฟล์) | ✅ |
| บล็อกโปรแกรมจาก `program_blacklist` — ดึงตาม heartbeat แล้วปิด process ที่ตรงชื่อ | ✅ |
| รายการอนุญาต `program_allowlist` จาก heartbeat — ของไม่รู้จักส่งสรุป `unknown_program` | ✅ |
| ไม่ปิดเครื่อง / ไม่ปิดตัว Agent / ไม่ปิด process ระบบ Windows | ✅ |
| ส่ง `POST /api/agent/logs` event `program` ให้ Monitor เห็นการบล็อก | ✅ |

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

# Terminal 3 — Face Service (จำเป็นเมื่อต้อง enroll ใบหน้า หรือสแกนหน้าที่ Kiosk)
cd C:\Users\User\Desktop\Kinof-project\face-service
py -3.11 -m uvicorn app.main:app --host 0.0.0.0 --port 8001

# Terminal 4 — Windows Agent (Run as administrator)
cd C:\Users\User\Desktop\Kinof-project\windows-agent
dotnet run
```

> dependency ของ face-service ติดตั้งไว้ที่ **Python 3.11 global** — `.venv` ในโฟลเดอร์นั้นยังว่าง
> (มีแค่ pip/setuptools) ถ้าเรียกด้วย `.venv\Scripts\python.exe` จะได้ `No module named uvicorn`

เปิด: `http://localhost:5173`

### ผล smoke test Phase 3B (7 ก.ย. 2569)

- `dotnet build` ผ่าน (0 warnings / 0 errors) และ `npm run build` ผ่าน
- `GET /api/kiosk/rooms/{id}` คืน `{ id, name, building, status }` เมื่อมี `X-Kiosk-Key` ของห้องนั้น; guid ที่ไม่มี → 404; คีย์ผิด/ห้องอื่น/ถูกเพิกถอน → 401
- Enroll `student` ในคาบที่กำลังเรียน (สร้างผ่าน `POST /api/admin/schedules`) → ขอ entry OTP → กรอกที่ Kiosk → **granted** พร้อม `seatNumber=1`, `seatLabel="คอม 01"`, `computerName="PC-LAB-A-01"`
- กรอก OTP เดิมซ้ำ → denied (one-time use ทำงาน)
- `admin` ที่ไม่มีตารางเรียน/การจอง → denied `"ไม่มีตารางเรียนหรือการจองห้องนี้ในช่วงเวลานี้"`
- `access_logs` มี 2 แถว: granted (มี `seat_id`) และ denied (มี `deny_reason`) ทั้งคู่ `auth_method = OtpFallback`
- `seats` ของที่นั่งที่ได้เปลี่ยนเป็น `Occupied` (คืนค่าเป็น `Available` หลังทดสอบแล้ว)
- `agent_logs` ได้ event `login` (`source: "kiosk"`) สำหรับที่นั่งที่มี agent
- กรอกรหัสผิด 5 ครั้งในห้องเดียว → ครั้งที่ 6 ได้ HTTP 429 และห้องอื่นไม่ถูกล็อก
- หน้า `/kiosk/{roomId}` ใน browser: welcome (ชื่อห้อง + เวลากรุงเทพเดินจริง + ปุ่มสแกนใบหน้า disabled "เร็วๆ นี้ (Phase 3C)"), หน้ากรอก OTP 6 ช่อง + keypad, และหน้า denied — render ถูกต้อง
  (หน้า success ยังไม่ได้ดูใน browser — ตรวจแล้วที่ระดับ API; ดูขั้นตอนทดสอบด้วยมือด้านล่าง)

### ผล smoke test ก่อนหน้า (6 ก.ย. 2569)

- `npm run build` ผ่าน
- `dotnet build .\Kinof.Api\Kinof.Api.csproj` ผ่าน (0 warnings / 0 errors)
- Student/Admin login → email OTP → dashboard ผ่าน และ auth อยู่ใน `sessionStorage`
- สร้าง booking กลุ่มผ่าน API และตอบรับคำเชิญผ่านหน้า Invitation สำเร็จ
- หลังตอบรับ UserHome แสดง `Lab A`, `8 ก.ย. 2569`, `09:00 - 11:30` ถูกต้อง
- UserHelp ส่ง problem report และ AdminHelpCenter เปลี่ยนสถานะเป็น `กำลังดำเนินการ` สำเร็จ
- Admin login + OTP ผ่าน และมี TopBar จาก App เพียงชั้นเดียว
- Student login + OTP ผ่าน; profile แสดงข้อมูลจาก `auth.user`
- ค้นหาห้องว่างผ่าน API และสร้าง booking จริงสำเร็จสำหรับ 4 ก.ย. 2569 รอบที่ 1
- UserHome อัปเดตรายการจองทันทีหลัง `POST /api/bookings`
- `/forgot-password` และ `/reset-password?token=...` render ได้
- หมายเหตุ: SQLite ที่กำลังใช้งานมีข้อมูลห้องเดิมชื่อ `Lab A`/`Lab B`; source seeder ปัจจุบันกำหนดชื่อห้องแล็บ 1-4

### ทดสอบ Login → OTP

1. Login ด้วย `student` / `Student123!` หรือ `admin` / `Admin123!` หรือ superadmin `superadmin1` / `SuperAdmin123!`
2. SMTP ใช้ **Resend** ผ่าน user-secrets ของ `Kinof.Api` (`smtp.resend.com` / username `resend` / API key เป็น `Email:Password`) — **ห้ามใส่รหัสใน appsettings ที่ commit และอย่า set Password ซ้ำถ้าใส่แล้ว**
3. `onboarding@resend.dev` ส่งได้เฉพาะเมลเจ้าของบัญชี Resend — ชี้ `Seed:StudentEmail` ไปเมลนั้นถ้าจะทดสอบ inbox ของ `student`
4. ถ้ามี host+user+password และส่งสำเร็จ → OTP ไปอีเมลจริง (`deliveryMode = smtp`, หน้าเว็บไม่โชว์ `devOtp`)
4. **Development** ถ้ายังไม่มี password หรือ SMTP ล้ม → log OTP/ลิงก์ใน console backend (`deliveryMode = console`)
5. **Production** ถ้าไม่มี password หรือ SMTP ล้ม → HTTP 503 ไม่แอบสำเร็จ
6. หลัง OTP สำเร็จ → user ไป `/register/face` ถ้ายังไม่ลงทะเบียนใบหน้า
7. จองห้องต้องเรียก `GET /api/rooms/available` และ `POST /api/bookings` จริง
8. Login admin ต้องมี TopBar เพียงชั้นเดียว
9. ตรวจ build ด้วย `npm run build`

รายละเอียด Resend + user-secrets: `docs/EMAIL_OTP.md` และ `docs/TEAM_SETUP.md`

### Face enrollment

- MediaPipe FaceDetector + FaceLandmarker
- Auto-capture เมื่อใบหน้าอยู่กึ่งกลาง + กระพริบตา (liveness)
- Frontend ส่งภาพ JPEG ชั่วคราวไป `POST /api/auth/register/face`
- Backend forward ภาพใน memory ไป FastAPI + InsightFace `buffalo_l`
- เก็บเฉพาะ normalized embedding 512 มิติใน DB และไม่เก็บรูปบน server
- ถ้ากล้องหรือ API error มีปุ่ม retry; ปุ่มข้ามที่วนกลับหน้าเดิมถูกเปลี่ยนเป็น logout

---

## สิ่งที่ยังไม่ทำ

ไม่มีงานฟีเจอร์หลักค้างแล้ว

### ลำดับงานถัดไป (ก่อนขึ้นของจริง)

อย่าข้ามข้อ เรียงตามนี้:

1. **ทดสอบเดโมบนเครื่องนี้ให้ครบรอบ**  
   ล็อกอิน + OTP เมล Resend · Kiosk พร้อมคีย์ · Agent รันแอดมิน · บล็อกเว็บ/โปรแกรม · ส่งออก · คะแนนพฤติกรรม
2. **รอบตรวจ Opus (แชทใหม่ สั้น)**  
   ไล่คีย์ Kiosk/Agent, JWT, เมลห้ามอยู่ใน git, ที่นั่ง Occupied ค้าง, นโยบายประตูไม่จ่ายที่นั่ง — แก้เฉพาะที่ชี้ว่าอันตรายก่อนพรีเซนต์
3. **เตรียมเครื่องจริง (ยังไม่กดขึ้นเน็ตมหาวิทยาลัย)**  
   โดเมน + HTTPS · CORS ไม่ใช่แค่ localhost · `Jwt:Key` ใหม่ · รหัสแอดมิน/นักศึกษาใหม่ · Resend โดเมนที่ verify แล้ว · `VITE_API_URL` และ Face Service URL ของเซิร์ฟเวอร์ · อย่าใช้ SQLite เป็นคำตอบระยะยาวถ้ามีผู้ใช้พร้อมกันมาก
4. **ติดตั้งที่แล็บ**  
   Agent ทีละเครื่อง (แอดมิน + API จริง + คีย์ที่นั่ง) · Kiosk ที่ประตู (`X-Kiosk-Key` ของห้องนั้น)

เดโม/พรีเซนต์จากโน้ตบุ๊กทำได้หลังข้อ 1 (ข้อ 2 ควรทำก่อนโชว์กรรมการถ้ามีเวลา)

### นโยบายเฝ้าเว็บ/โปรแกรม (ตัดสินแล้ว — MVP ทำแล้ว)

อย่าขึ้น “น่าสงสัย” ทั้งก้อนแล้วให้แอดมินไล่ดู และอย่าทำฐานอนุญาต/ห้ามทั้งอินเทอร์เน็ต

| อย่าง | วิธี |
|-------|------|
| เว็บ | ค่าเริ่มต้นใช้ได้ ห้ามเป็น **หมวด** ไม่ใช่ allowlist ทั้งเว็บ — แอดมินเลือกหมวด UT1 แล้วระบบนำเข้าโดเมนหมวดนั้นเข้า `website_blacklist` พร้อม `category` |
| แหล่งหมวด | [Blacklists UT1](https://dsi.ut-capitole.fr/blacklists/index_en.php) (เช่น `social_networks`, สตรีม, เกม) — **จำกัด 250 โดเมนต่อหมวด** (สูงสุด 400) ไม่เททั้งไฟล์ลง hosts |
| โปรแกรม | รายการอนุญาตของห้องแล็บ (`program_allowlist`) + รายการห้ามสั้นๆ ที่รู้แล้ว (`discord.exe`, `steam.exe`) |
| ของไม่รู้จัก | รวมเป็นสรุปแท็บ Monitor **ไม่รู้จัก** (ซ้ำกี่ครั้ง / กี่เครื่อง) ไม่ขึ้นคิวทีละคลิก |
| คะแนน | หักเมื่อแอดมินกดบล็อกในคิวน่าสงสัย หรือ no-show — ไม่หักอัตโนมัติแค่เพราะโปรแกรม/เว็บไม่รู้จัก |
| ห้าม | ให้ AI ตัดสินว่าเว็บ/โปรแกรมไม่ดี · ส่งทุกแท็บเบราว์เซอร์ขึ้น Monitor |

Windows Agent (heartbeat / logout / บล็อกเว็บ / บล็อกโปรแกรม / สรุปโปรแกรมไม่รู้จัก) ทำแล้ว — รายละเอียด `docs/implementation/AGENT.md` และ `windows-agent/README.md`

---

## Kiosk device auth (`X-Kiosk-Key`)

แนวเดียวกับ Agent แต่ผูก **ห้อง** ไม่ผูกที่นั่ง

- แอดมิน `GET/POST /api/admin/kiosk-devices` และ `POST /api/admin/kiosk-devices/{id}/revoke` (JWT admin+)
- `apiKey` ส่งกลับครั้งเดียวตอนสร้าง แล้วไม่โชว์ในรายการอีก
- `GET /api/kiosk/rooms/{roomId}`, `POST /api/kiosk/entry/verify-otp`, `POST /api/kiosk/entry/verify-face` ต้องมี header `X-Kiosk-Key` ของห้องนั้น — คีย์ห้องอื่น / เพิกถอนแล้ว / ไม่ส่ง → 401
- หน้า `/kiosk/:roomId` เก็บคีย์ใน `localStorage` ของเครื่องประตู หรือรับครั้งแรกจาก `?key=` แล้วตัดออกจาก URL — จอสแกนไม่ให้ผู้ใช้กรอกคีย์
- เปิดตั้งค่าใหม่ด้วย `?setup=1`
- โหมด dev: seeder ใส่ `dev-kiosk-key-1` … ตามลำดับชื่อห้อง (`ห้องแล็บ 1` = key-1)
- นโยบายที่นั่งไม่เปลี่ยน: Kiosk ตรวจสิทธิ์เข้าห้องอย่างเดียว ที่นั่ง Occupied เมื่อ login บน Agent

หน้าแอดมิน: **จัดการข้อมูล → ห้องแล็บ → อุปกรณ์ Kiosk ต่อห้อง**

---

## Phase 3B — สรุปสิ่งที่ทำ (เสร็จแล้ว)

### `backend/Kinof.Api/Services/EntryService.cs` — shared 3B + 3C

`AuthorizeAndAssignSeatAsync(userId, roomId, authMethod, ct)` — ลำดับการตัดสิน:

1. `Room.Status == Open` · `User.Status == Active` — ไม่ผ่าน → denied + `access_logs`
2. สิทธิ์เข้า (ต้องได้อย่างน้อย 1 ข้อ)
   - **กลุ่ม 1 (schedule):** `ScheduleEnrollments` + `Schedules.IsActive` ที่ `RoomId` ตรง,
     `DayOfWeek` = วันนี้ตามเวลากรุงเทพ (`BangkokTime.ToLocal`) และ now ∈ `[StartTime, EndTime)`
     (เทียบ `TimeOnly` ในหน่วยความจำเหมือน `ScheduleService` เพราะ SQLite เก็บเวลาเป็น text)
   - **กลุ่ม 2/3 (booking):** `Bookings` `Confirmed` ที่ `RoomId` ตรง + now ∈ `[StartTime, EndTime)`
     ครอบทั้งเจ้าของการจองและสมาชิกใน `GroupMembers` (แถวนี้ถูกสร้างตอนตอบรับคำเชิญ = accepted แล้ว)
3. ไม่มีสิทธิ์ → `access_logs` denied + คืน `denied`
4. เลือกที่นั่งแรกในห้องที่ `Status == Available` เรียงตาม `SeatNumber` — ไม่มีว่าง → denied `"ไม่มีที่นั่งว่างในห้องนี้"`
5. `Seat.Status = Occupied` + `access_logs` granted (`auth_method`, `seat_id`) ใน `SaveChanges` เดียว
6. ถ้าที่นั่งนั้นมี `Agent` → เพิ่ม `agent_logs` event `login` (`source: "kiosk"`) ให้หน้า Monitor เห็นทันที
7. คืน `EntryDecision` (displayName, roomName, seatNumber, seatLabel, computerName)

`seatLabel` ใช้ `TrackingService.SeatLabel(n)` = `คอม 01` (ตัวเดียวกับหน้า Tracking) และ
`displayName` ใช้ `TrackingService.ShortDisplayName` เพื่อไม่โชว์นามสกุลเต็มบนจอสาธารณะ

### `KioskEndpoints.cs` + `KioskService.cs` — `X-Kiosk-Key` ผูกห้อง ไม่ใช้ JWT

```
GET  /api/kiosk/rooms/{roomId}    → { id, name, building, status } · 404 ถ้าไม่มีห้อง
POST /api/kiosk/entry/verify-otp  → body { roomId, code: "123456" }
     granted: { granted: true, user: { displayName, username },
                room: { id, name, building }, seatNumber, seatLabel, computerName }
     denied:  { granted: false, message }   (HTTP 200 เพื่อให้ Kiosk แสดงผลได้เลย)
     429:     { granted: false, message }   เมื่อชนกับ rate limit
```

- หา `entry_otps` ที่ `UsedAt == null` + `ExpiresAt > now` (ล่าสุด 200 แถว) แล้ว `BCrypt.Verify` ทีละแถว
- ถ้า OTP ผูก `RoomId` ไว้ ต้องตรงกับ `roomId` ที่ verify ไม่งั้น denied (แจ้งว่าเป็นรหัสของห้องอื่น)
- mark `UsedAt` **ก่อน** เรียก `EntryService` → รหัสใช้ได้ครั้งเดียวแม้สุดท้ายจะไม่มีสิทธิ์เข้า
- รหัสผิด/หมดอายุใช้ข้อความกลาง `"รหัสไม่ถูกต้องหรือหมดอายุแล้ว..."` (ไม่บอกว่าผิดหรือหมดอายุ)
- **Rate limit:** `KioskAttemptLimiter` (singleton, in-memory) — verify ล้มเหลว 5 ครั้ง / 15 นาที **ต่อ roomId**
  → 429; นับเฉพาะกรณีรหัสไม่ผ่าน ไม่นับกรณี "ไม่มีสิทธิ์เข้า" เพื่อไม่ให้ผู้ใช้สุจริตทำให้เครื่องล็อก
  (ถ้า scale API หลาย instance ต้องย้ายไป cache กลาง)

### Frontend

- `kinof-app/src/api/kiosk.js` — `getKioskRoom`, `verifyKioskOtp` (fetch ตรง ไม่ผ่าน `apiFetch` จึงไม่แนบ JWT; ส่ง `X-Kiosk-Key`)
- `kinof-app/src/pages/kiosk/KioskEntry.jsx` — route `/kiosk/:roomId` **นอก user/admin shell** (ไม่มี Sidebar/TopBar)
  4 สถานะ: welcome (ชื่อห้อง + สถานะ + นาฬิกากรุงเทพ) → otp (6 ช่อง + keypad บนจอ + คีย์บอร์ดจริง)
  → success (ชื่อผู้ใช้ + ที่นั่ง, auto reset 30 วิ) → denied (เหตุผล + auto reset 20 วิ)
- ปุ่ม "สแกนใบหน้า" disabled พร้อมข้อความ "เร็วๆ นี้ (Phase 3C)" — **3C เปิดใช้แล้ว** (ดูหัวข้อ Phase 3C)
- `App.jsx` เพิ่ม `<Route path="/kiosk/:roomId" element={<KioskEntry />} />` และข้าม session bootstrap
  สำหรับ path `/kiosk/...` เพื่อไม่ให้เด้งไป `/login`

### ทดสอบด้วยมือ (หน้า success)

1. Admin สร้างคาบที่คลุมเวลาปัจจุบันในห้องที่ต้องการ (`จัดการข้อมูล → ตารางเรียน`) แล้วเพิ่มรหัส `6600000001`
   — หรือใช้ booking ที่ `Confirmed` และกำลังอยู่ในช่วงเวลา
2. Login `student / Student123!` → `/entry-otp` → ขอรหัสของห้องนั้น → copy OTP จาก console backend
3. เปิด `http://localhost:5173/kiosk/{roomId}?key=dev-kiosk-key-1` (ครั้งแรก) → "ใช้รหัสจากเว็บ" → กรอก 6 หลัก → ต้องเข้าห้องได้ (ไม่จ่ายที่นั่ง)
4. ถ้าที่นั่งในห้องเป็น `Occupied`/`Offline` หมด จะได้ `"ไม่มีที่นั่งว่างในห้องนี้"` — ปล่อยที่นั่งก่อนทดสอบซ้ำ

### ข้อสังเกตที่ค้างไว้

- enum ใน DB เก็บเป็น PascalCase (`OtpFallback`, `Granted`, `Occupied`) ตาม `HasConversion<string>()`
  ที่ใช้ทั้งโปรเจกต์ ต่างจากตัวอย่าง snake_case ใน `docs/DATABASE.md` — API ที่ expose ออกไปแปลงเป็น
  lowercase อยู่แล้ว จึงคงรูปแบบเดิมไว้เพื่อไม่ให้ข้อมูลเก่าพัง
- ยังไม่มี flow ปล่อยที่นั่ง — seat จะค้าง `Occupied` จนกว่า agent จะส่ง `logout` (Phase 7)
- SQLite ที่ใช้ทดสอบมีที่นั่ง `Lab A` #2–#30 เป็น `Offline` มาก่อนหน้านี้ เหลือที่ว่างจริงแค่ #1
- Kiosk ใช้ `X-Kiosk-Key` ผูกห้อง (สร้างจากแอดมิน / คีย์ตัวอย่าง `dev-kiosk-key-N` ในโหมด dev) + rate limit ต่อห้องตามที่ตัดสินใจไว้

---

## Phase 3C — สรุปสิ่งที่ทำ (เสร็จแล้ว)

**ผลลัพธ์:** ที่ Kiosk สแกนใบหน้า → identify ใน backend → ผ่านแล้วเรียก `EntryService` ตัวเดิมด้วย
`AuthMethod.Face` → ได้ที่นั่ง; ไม่ผ่าน → แนะนำ/พาไปทาง entry OTP ของ 3B

### `backend/Kinof.Api/Services/FaceMatchingService.cs` (ใหม่)

- `IdentifyAsync(float[] probe, ct)` → `FaceMatch(UserId, Score, Matched)` หรือ `null` เมื่อยังไม่มีใครลงทะเบียนใบหน้า
- โหลด `face_embeddings` join `users` ที่ `Status == Active && FaceEnrolled` → deserialize JSON เป็น `float[512]`
  (แถวที่ deserialize ไม่ได้/มิติไม่ครบ ถูกข้ามพร้อม log warning) → cosine similarity → เลือก best match
- `MatchThreshold = 0.5` ตาม `docs/AUTH_ADAPTIVE.md`
- **ตัดสินใจ:** ค้นแบบ linear ทั้งตาราง (ไม่กรองด้วยสิทธิ์เข้าห้องก่อน) — ทำให้ข้อความ deny แยกได้ว่า
  "ไม่รู้จักใบหน้า" กับ "รู้จักแต่ไม่มีสิทธิ์" และ embedding เป็น unit vector 512-d ตัวละ 1 dot product

### `backend/Kinof.Api/Services/FaceImage.cs` (ใหม่)

ย้าย `TryDecodeImage` จาก `AuthService` มาเป็น `FaceImage.TryDecode` เพราะทั้ง enroll และ Kiosk
รับ payload รูปแบบเดียวกัน (data URL JPEG/PNG/WebP, 1 KB–5 MB) — พฤติกรรมเดิมไม่เปลี่ยน

### `KioskService.VerifyFaceAsync` + `POST /api/kiosk/entry/verify-face`

```
POST /api/kiosk/entry/verify-face    [X-Kiosk-Key]
  Request: { roomId, imageBase64: "data:image/jpeg;base64,..." }
  granted: เหมือน verify-otp ทุก field (user/room/seatNumber/seatLabel/computerName)
  denied:  { granted: false, message, suggestOtp, identified? }
  429:     { granted: false, message, suggestOtp: true }
```

ลำดับการทำงาน: validate roomId → `FaceImage.TryDecode` → เช็ค rate limit scope `face` →
`FaceEmbeddingClient.CreateEmbeddingAsync` → `FaceMatchingService.IdentifyAsync` →
`EntryService.AuthorizeAndAssignSeatAsync(..., AuthMethod.Face, ...)`

`suggestOtp` บอก Kiosk ว่าควรลองสแกนใหม่หรือควรเปลี่ยนไปใช้ OTP:

| กรณี | คำตอบ | นับ rate limit |
|------|-------|----------------|
| ภาพใช้ไม่ได้ / ไม่พบใบหน้า / เจอหลายหน้า (face-service 4xx) | `suggestOtp: false` → สแกนใหม่ | ไม่นับ |
| Face Service ล่ม/timeout (503) | `suggestOtp: true` | ไม่นับ |
| ไม่มี embedding ในระบบเลย | `suggestOtp: true` | นับ |
| score < 0.5 (รวมช่วง 0.4–0.5 — MVP treat as fail ตามที่ล็อกไว้) | `suggestOtp: true` | นับ |
| รู้จักใบหน้าแต่ไม่มีตารางเรียน/การจอง | `suggestOtp: false`, `identified: true` | ไม่นับ |

- **ไม่ส่ง score ออกไปที่ client** — จอสาธารณะไม่ต้องรู้ค่า (score อยู่ใน log ฝั่ง server เท่านั้น)
- **ไม่เก็บรูป** — ภาพอยู่ใน memory แล้วส่งต่อ face-service เหมือน enrollment
- `access_logs` เขียนเฉพาะเมื่อรู้ตัว user แล้ว (เหมือน 3B) → face ที่ไม่รู้จักไม่สร้างแถวขยะ
- **Rate limit:** `KioskAttemptLimiter` เปลี่ยน key จาก `roomId` เป็น `"{scope}:{roomId}"`
  (`KioskService.OtpScope` / `FaceScope`) → หน้าไม่ผ่าน 5 ครั้งในห้องเดียวไม่ล็อกทาง OTP ของห้องนั้น
- ไม่มี migration ใหม่ และไม่ได้เพิ่ม endpoint ใน face-service (match ทำใน C#)

### Frontend

- `kinof-app/src/api/kiosk.js` — `verifyKioskFace(roomId, imageBase64)` (verify-otp/verify-face ใช้ helper `postEntry` ร่วมกัน)
- `kinof-app/src/hooks/useFaceCapture.js` — เพิ่ม option `requireBlink` / `holdMs` / `capturingHint` และคืน `captureNow`
  - Kiosk เรียกด้วย `requireBlink: false` → **ไม่โหลด FaceLandmarker เลย** ใช้แค่ FaceDetector (เบากว่า)
    แล้ว auto-capture เมื่อใบหน้าอยู่กึ่งกลางค้าง 1.5 วินาที
  - หน้า enroll ยังใช้ค่า default (กระพริบตา) — พฤติกรรมเดิมไม่เปลี่ยน
- `kinof-app/src/pages/kiosk/KioskEntry.jsx` — เพิ่ม step `face` (component `KioskFaceScan` ในไฟล์เดียวกัน
  เพื่อให้กล้องเปิดเฉพาะตอนอยู่ในหน้านั้น) ปุ่ม "สแกนใบหน้า" จาก welcome ใช้งานได้แล้ว
  - ปุ่ม "ถ่ายภาพเลย" (manual shutter) + "สแกนใหม่" + "ใช้รหัสจากเว็บ" + "ยกเลิก"
  - `suggestOtp` + ไม่ผ่านครบ 3 ครั้ง (`MAX_FACE_ATTEMPTS`) → เด้งไปหน้า OTP พร้อม banner บอกเหตุผล
  - `identified: true` → ไปหน้า denied เต็มจอ (OTP ก็ตอบเหมือนกัน ไม่ต้องให้ลองซ้ำ)
  - กล้องเปิดไม่ได้ → แสดงเหตุผล + ปุ่มไป OTP ได้ทันที
  - ไม่มี TopBar/Sidebar — Kiosk ยัง standalone ตามเดิม

### ผล smoke test Phase 3C (7 ก.ย. 2569)

`dotnet build` ผ่าน (0 errors, warning เดิม 1 ตัวใน `ScheduleService.cs`) และ `npm run build` ผ่าน
รัน backend 5106 + face-service 8001 (InsightFace `buffalo_l`, `/health` = ready) แล้วทดสอบผ่าน API จริง:

1. enroll ใบหน้าทดสอบให้ `student` → `POST /api/kiosk/entry/verify-face` ห้อง Lab A ในคาบที่กำลังเรียน
   → **granted** `seatNumber=1`, `seatLabel="คอม 01"`, `computerName="PC-LAB-A-01"`
2. ส่งรูปคนละคน → `{ granted: false, suggestOtp: true }` ข้อความแนะนำใช้รหัสจากเว็บ
3. Entry OTP path ของ 3A/3B ยัง granted ปกติหลังแก้ limiter (ไม่ regress)
4. ปิดคาบเรียนแล้วสแกนหน้าเดิม → `{ granted: false, suggestOtp: false, identified: true }`
   ข้อความ "ไม่มีตารางเรียนหรือการจองห้องนี้ในช่วงเวลานี้"
5. สแกนไม่ผ่าน 5 ครั้งในห้องเดียว → ครั้งที่ 6 ได้ **HTTP 429**; และ `verify-otp` ของห้องเดียวกัน
   ยังตอบข้อความปกติ (scope แยกกันจริง)
6. `access_logs`: `auth_method = Face` ทั้งแถว `Granted` (มี `seat_id`) และ `Denied` (มี `deny_reason`)
7. หน้า `/kiosk/{roomId}` ใน browser: welcome เปิดปุ่ม "สแกนใบหน้า" แล้ว, หน้าสแกนแสดงกรอบวงรี +
   progress + hint, ปุ่ม "ใช้รหัสจากเว็บ" ไปหน้า keypad ได้
   (เครื่องทดสอบไม่มีกล้องว่าง — หน้าสแกนแสดง fallback "กล้องกำลังถูกใช้งานโดยโปรแกรมอื่น" ถูกต้อง
   **ยังไม่ได้ทดสอบ capture จากกล้องจริงบนเบราว์เซอร์** ต้องลองด้วยมือที่เครื่องมีกล้อง)

> ข้อมูลทดสอบถูกคืนค่าหลังเทสครบ: embedding เดิมของ `student` (เทียบไบต์ต่อไบต์แล้วตรง),
> `schedules.is_active` ของคาบ `KIOSK3B` และที่นั่งที่ถูกจับจอง — เหลือไว้แค่แถว `access_logs` เป็นหลักฐาน

### ทดสอบด้วยมือ (แนะนำ — ต้องมีกล้อง)

1. รัน face-service: `cd face-service; py -3.11 -m uvicorn app.main:app --host 0.0.0.0 --port 8001`
   (dependency อยู่ที่ Python 3.11 global ไม่ได้อยู่ใน `.venv` ของโฟลเดอร์นั้น)
2. Login `student / Student123!` → `/register/face` → ลงทะเบียนใบหน้าตัวเอง
3. Admin สร้างคาบเรียนที่คลุมเวลาปัจจุบันในห้องที่จะทดสอบ (หรือใช้ booking ที่ `Confirmed` อยู่)
4. เปิด `http://localhost:5173/kiosk/{roomId}?key=dev-kiosk-key-1` → "สแกนใบหน้า" → มองกล้องนิ่ง ~1.5 วินาที → ต้องเข้าห้องได้ (ไม่จ่ายที่นั่ง)
5. ให้คนที่ไม่ได้ลงทะเบียนใบหน้าลองสแกน 3 ครั้ง → ต้องเด้งไปหน้ากรอก OTP เอง

### Seed สำหรับเทส

`student / Student123!` · `admin / Admin123!` · `superadmin1 / SuperAdmin123!`

---

### ตัดสินใจแล้ว — MVP booking

- **Schedule ชนจองแม้ส่วนเดียว → block ทั้งรอบ** (backend + frontend ทำแล้ว)
- **Partial booking หลังเลิกเรียน — ไม่ทำใน MVP** (เลื่อนออก / post-MVP)

---

## ข้อกำหนดสำคัญ

- OTP ทาง **email เท่านั้น** — ไม่ใช้ Google Authenticator
- External users สมัครแล้ว **active ทันที**
- **อย่า copy Tracking Agent** จาก smartlab repo
- **Booking vs schedule: block ทั้งรอบ** — ไม่ partial ใน MVP
- Kiosk flow (Phase 2): face ผ่าน → เข้าได้; ไม่ผ่าน → entry OTP

---

## ไฟล์สำคัญ

```
backend/Kinof.Api/Services/EntryService.cs      ← authorize + seat + access_log (3B/3C ใช้ร่วม)
backend/Kinof.Api/Services/KioskService.cs      ← verify entry OTP + verify face + rate limiter + X-Kiosk-Key
backend/Kinof.Api/Services/KioskDeviceService.cs ← แอดมินสร้าง/เพิกถอนอุปกรณ์ต่อห้อง
backend/Kinof.Api/Services/FaceMatchingService.cs ← identify 1:N cosine (3C)
backend/Kinof.Api/Services/FaceImage.cs         ← decode/validate รูป base64 (enroll + kiosk)
backend/Kinof.Api/KioskEndpoints.cs
backend/Kinof.Api/Services/EntryOtpService.cs
backend/Kinof.Api/Services/EmailSender.cs
backend/Kinof.Api/Services/AuthService.cs
backend/Kinof.Api/Program.cs
backend/Kinof.Api/Services/WebsiteBlacklistService.cs
backend/Kinof.Api/Services/Ut1WebsiteCategoryService.cs
backend/Kinof.Api/Services/ProgramAllowlistService.cs
windows-agent/Worker.cs
windows-agent/ProgramProcessBlocker.cs
windows-agent/AgentApiClient.cs
docs/implementation/AGENT.md
kinof-app/src/pages/admin/AdminMonitor.jsx
kinof-app/src/pages/kiosk/KioskEntry.jsx
kinof-app/src/api/kiosk.js
kinof-app/src/pages/admin/AdminData.jsx
kinof-app/src/pages/user/EntryOtp.jsx
kinof-app/src/api/auth.js
kinof-app/src/App.jsx
docs/AUTH_ADAPTIVE.md
docs/EMAIL_OTP.md
```
