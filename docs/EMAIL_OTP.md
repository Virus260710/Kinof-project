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

## ส่ง Email (Backend)

**.NET:** MailKit + SMTP  
**Node:** nodemailer

```json
// appsettings.json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "your-app@gmail.com",
    "Password": "app-password",
    "FromAddress": "noreply@kinof.local",
    "FromName": "KINOF Lab System"
  }
}
```

**Dev ไม่มี SMTP:** log OTP ลง console / ใช้ [Mailpit](https://mailpit.axllent.org/) local

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
