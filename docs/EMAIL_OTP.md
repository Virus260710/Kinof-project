# OTP ทาง Email — KINOF

> **เปลี่ยนจาก Google Authenticator → รับ OTP ผ่านอีเมล**

## OTP 2 แบบ (ส่ง email ทั้งคู่)

| แบบ | ใช้เมื่อ | Flow |
|-----|---------|------|
| **Login OTP** | Login เว็b ชั้น 2 | กรอก user/pass → ระบบส่ง OTP 6 หลักไป **email** → กรอกที่ `/login/otp` |
| **Entry OTP** | Kiosk สแกนไม่ผ่าน | Login เว็b → กด "ขอรหัสเข้าห้อง" → OTP ส่งไป **email** → กรอกที่ Kiosk |

---

## Flow Login + Email OTP

```
1. กรอก username/email + password → กด "เข้าสู่ระบบ"
2. Backend ตรวจ password ถูก → สร้าง OTP 6 หลัก → ส่ง email
3. ไปหน้า /login/otp
   ข้อความ: "รหัส OTP ถูกส่งไปที่ u***@gmail.com"
4. กรอก OTP 6 หลัก → Verify → ได้ JWT → Dashboard
5. ปุ่ม "ส่ง OTP ใหม่" (rate limit 3 ครั้ง/ชม.)
```

---

## API

```
POST /api/auth/login
  Request:  { username, password }
  Response: { requiresOtp: true, userId, maskedEmail: "u***@gmail.com" }

POST /api/auth/verify-email-otp
  Request:  { userId, code: "123456" }
  Response: { accessToken, refreshToken, user }

POST /api/auth/resend-email-otp
  Request:  { userId }
  Response: { ok: true, maskedEmail }
```

---

## Flow Entry OTP (สำรองเข้าห้อง)

```
1. Login เว็บด้วย JWT แล้ว
2. ไป /entry-otp → เลือกห้อง (optional) → กด "ขอรหัสเข้าห้อง"
3. Backend สร้าง OTP 6 หลัก, hash, หมดอายุ 10 นาที, ส่ง email
4. หน้าเว็บแสดง maskedEmail + countdown — ไม่แสดงรหัสจริง
5. ถ้า Kiosk สแกนหน้าไม่ผ่าน → กรอก OTP ที่เครื่อง (Phase 3B)
```

---

## API — Entry OTP (Phase 3A)

```
POST /api/auth/entry-otp/request   [JWT]
  Request:  { roomId: "guid"|null }
  Response: { ok, maskedEmail, expiresAt, roomId?, roomName?, deliveryMode }

POST /api/auth/entry-otp/resend    [JWT]
  Request:  { roomId: "guid"|null }
  Response: เหมือน request
  Rate limit: รวม request+resend ไม่เกิน 3 ครั้ง/ชม. ต่อ user

GET  /api/auth/entry-otp/active    [JWT]
  Response: { hasActive, expiresAt?, roomId?, roomName?, maskedEmail? }
  ไม่ส่ง code จริง
```

- ต้อง user Active; ถ้ามี roomId ห้องต้องมีจริงและ Open
- OTP เก่าที่ยังไม่ used ของ user ถูก invalidate เมื่อขอใหม่
- Development (`Email:SkipSmtpInDevelopment` = true ใน `appsettings.Development.json`) → **ไม่ยิง Resend** log OTP ใน console + โชว์บนหน้าเว็บ (`deliveryMode = console`) จนกว่าจะขึ้นเครื่องจริง
- Production ส่งล้ม → HTTP 503

---

## ส่ง Email จริง (SMTP / Resend)

Backend ใช้ **MailKit** ส่งเมลเมื่อมีครบ `SmtpHost` + `Username` + `Password` แล้ว `deliveryMode` จะเป็น `smtp`  
หน้า `/login/otp` **ไม่โชว์รหัส OTP** เมื่อ `deliveryMode` เป็น `smtp`

รหัส SMTP / Resend API key **ห้ามใส่ใน `appsettings.json` ที่ commit** — ใช้ **dotnet user-secrets** หรือ environment เท่านั้น  
ถ้าใส่ secrets ครบแล้ว **อย่า set `Email:Password` ซ้ำ** และอย่า `user-secrets list` ลงแชท/commit

### Resend SMTP (ที่ใช้อยู่)

ค่าจริงอยู่ที่ user-secrets ของโปรเจกต์ `backend/Kinof.Api` (ทับค่าใน `appsettings.json`):

| Key | ค่า |
|-----|-----|
| `Email:SmtpHost` | `smtp.resend.com` |
| `Email:SmtpPort` | `587` |
| `Email:Username` | `resend` |
| `Email:Password` | Resend API key (`re_...`) — **user-secrets เท่านั้น** |
| `Email:FromAddress` | ผู้ส่งที่ verify แล้ว |
| `Email:FromName` | เช่น `KINOF Lab System` |

ครั้งแรกที่เครื่อง (ข้ามได้ถ้าใส่ครบแล้ว):

```powershell
cd backend
dotnet user-secrets set "Email:SmtpHost" "smtp.resend.com" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:SmtpPort" "587" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:Username" "resend" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:Password" "re_xxxxxxxx" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:FromAddress" "onboarding@resend.dev" --project .\Kinof.Api\Kinof.Api.csproj
```

รีสตาร์ท API หลังเปลี่ยน secrets

**ข้อจำกัด Resend:** `onboarding@resend.dev` ส่งได้เฉพาะอีเมลเจ้าของบัญชี Resend  
ถ้า OTP ต้องถึงเมลอื่น ให้ verify domain ที่ [resend.com](https://resend.com) แล้วตั้ง `Email:FromAddress` เป็นที่อยู่บนโดเมนนั้น

เพื่อทดสอบ inbox ทันที ให้ชี้บัญชี `student` ไปที่เมลเจ้าของ Resend (ไม่ใช่ password):

```powershell
dotnet user-secrets set "Seed:StudentEmail" "your-resend-account@gmail.com" --project .\Kinof.Api\Kinof.Api.csproj
```

รีสตาร์ท API — seeder จะอัปเดตอีเมลแถว `student` ใน Development (ข้ามถ้าอีเมลนั้นถูกบัญชีอื่นใช้แล้ว เพราะ unique)

`appsettings.json` ที่ commit ได้มีแค่ placeholder (`Password` ว่าง) — ค่า Resend มาจาก secrets

หรือ Environment: `Email__SmtpHost`, `Email__Username`, `Email__Password`, `Email__FromAddress` (ขีดล่างสองเส้น)

### พฤติกรรมตามสภาพแวดล้อม

| สภาพ | ผล |
|------|-----|
| มี host+user+password และส่งสำเร็จ | ส่งเมลจริง, `deliveryMode = smtp`, หน้าเว็บไม่โชว์ `devOtp` |
| **Development** `SkipSmtpInDevelopment: true` | ไม่ส่ง Resend — OTP ใน console และหน้าเว็บ |
| **Production** ไม่มี password หรือ SMTP ล้ม | API ตอบ 503 ไม่แอบสำเร็จ — ห้าม fallback ไป console |

หน้าเว็บโชว์ `devOtp` เฉพาะ Development เมื่อ `deliveryMode` ไม่ใช่ `smtp`

**Production** ตั้งรหัสด้วย environment หรือ secret store ของ host — อย่า commit ไฟล์ที่มี API key

**Email template (ตัวอย่าง):**
```
Subject: รหัส OTP เข้าสู่ระบบ KINOF

สวัสดี {firstName},

รหัส OTP ของคุณคือ: 123456
ใช้ได้ 10 นาที ห้ามแชร์ให้ผู้อื่น

— KINOF ระบบจองห้องแล็บ
```

---

## หน้า UI (/login/otp)

ตาม mockup — เปลี่ยนข้อความเป็น:

- **เดิม:** "รับรหัส OTP จากแอป Google Authenticator"
- **ใหม่:** "รหัส OTP ถูกส่งไปยังอีเมล **u***@gmail.com** ที่ลงทะเบียนไว้"

---

## Zenith Comp (Auth 2 ชั้น)

- **ชั้น 1:** Password (something you know)
- **ชั้น 2:** Email OTP (something you have — access to email)

ไม่ใช้ TOTP / Google Authenticator อีกต่อไป
