# Prompt — Forgot Password + Google Placeholder + Session Bootstrap

> **Workspace:** `C:\Users\User\Desktop\Kinof-project`  
> **Branch:** `cursor/phase0-backend-email-otp`  
> **Commit บน GitHub:** `b77e255` (อ่าน `git status` + `git diff` ก่อน — อาจมี uncommitted จากแชทอื่น)

---

## คัดลอก Prompt นี้ไปแชทใหม่

```
ทำต่อโปรเจกต์ KINOF ที่ C:\Users\User\Desktop\Kinof-project

GitHub: https://github.com/Virus260710/Kinof-project.git
Branch: cursor/phase0-backend-email-otp

=== ทำเสร็จแล้ว (อย่าสร้างซ้ำ) ===

Login flow หลัก:
- Login.jsx — เลือก user/admin, username+password → API
- OtpVerify.jsx — OTP 6 หลัก email, resend, role check
- Auth API: login, verify-email-otp, resend-email-otp, refresh, /me
- JWT + refresh ใน localStorage (kinofAuth)
- apiFetch() refresh อัตโนมัติเมื่อ 401
- Register, face enrollment, booking — มีแล้ว

Backend: backend/Kinof.Api/ port 5106
Frontend: kinof-app/ port 5173
EmailSender (MailKit) — reuse สำหรับส่ง email reset
DB: ตาราง password_reset_tokens มีแล้ว (Entities.cs + migration)

=== อ่านก่อนทำ ===
docs/HANDOFF_LATEST.md
docs/EMAIL_OTP.md
docs/UI_AUTH_PHASE.md
docs/DATABASE.md (password_reset_tokens — หมดอายุ 1 ชม.)
backend/Kinof.Api/Services/AuthService.cs
backend/Kinof.Api/Services/EmailSender.cs
backend/Kinof.Api/Data/Entities.cs
kinof-app/src/pages/Login.jsx
kinof-app/src/App.jsx
kinof-app/src/api/auth.js

=== งานที่ต้องทำ (เรียงลำดับ) ===

## 1. Forgot Password (ครบ flow)

### Backend
- POST /api/auth/forgot-password
  - Request: { email }
  - สร้าง reset token (crypto random) → hash เก็บ password_reset_tokens
  - หมดอายุ 1 ชม.
  - ส่ง email ลิงก์: http://localhost:5173/reset-password?token=...
  - Security: response เหมือนกันเสมอ ไม่เปิดเผยว่า email มีในระบบหรือไม่
  - Dev ไม่มี SMTP: log link/token ใน console (แบบ login OTP)

- POST /api/auth/reset-password
  - Request: { token, newPassword }
  - ตรวจ hash, ยังไม่ used, ยังไม่หมดอายุ
  - อัปเดต password_hash (BCrypt), mark used_at
  - Response: { ok: true }

- เพิ่ม SendPasswordResetEmailAsync ใน IEmailSender / EmailSender

### Frontend
- เพิ่มลิงก์ "ลืมรหัสผ่าน" ใน Login.jsx → /forgot-password
- ForgotPassword.jsx — กรอก email
- ResetPassword.jsx — อ่าน ?token= จาก URL, รหัสใหม่ + ยืนยัน
- Routes ใน App.jsx: /forgot-password, /reset-password
- auth.js: forgotPassword(), resetPassword()
- ใช้ Card/theme จาก kinof-app

## 2. Social Login — Google placeholder เท่านั้น

- **เอาปุ่ม Facebook ออก** จาก Login.jsx
- **คงปุ่ม Google ไว้** — disabled + ข้อความ "เร็ว ๆ นี้"
- **ไม่ implement OAuth จริง** — อย่าเพิ่ม dependency Google/Facebook SDK

## 3. Session bootstrap ตอนเปิดแอป

- App.jsx useEffect ตอน mount:
  - มี kinofAuth → เรียก GET /api/auth/me
  - สำเร็จ → sync auth.user + localStorage (faceEnrolled ฯลฯ)
  - 401 → ลอง refresh token ก่อน
  - ยัง fail → clear kinofAuth, ไป /login
- loading สั้น ๆ ระหว่าง bootstrap (แนะนำ)

=== ข้อกำหนด ===
- OTP login ใช้ email เท่านั้น — reset ใช้ email link/token ไม่ใช้ TOTP
- Minimize scope — อย่า refactor ไม่เกี่ยว
- หยุด backend process เก่าก่อน rebuild ได้ (port 5106)
- ทดสอบ: forgot → email/console → reset → login รหัสใหม่ + bootstrap /me
- git commit/push เฉพาะเมื่อ user ขอ

=== รัน dev ===
# หยุด backend เก่าก่อนถ้า port 5106 ถูกใช้
cd C:\Users\User\Desktop\Kinof-project\backend
dotnet run --project .\Kinof.Api\Kinof.Api.csproj

cd C:\Users\User\Desktop\Kinof-project\kinof-app
npm run dev
```

---

## Test checklist

- [ ] Login มีลิงก์ "ลืมรหัสผ่าน"
- [ ] ไม่มีปุ่ม Facebook — มีแค่ Google (disabled)
- [ ] forgot-password → email/console link
- [ ] reset-password → ตั้งรหัสใหม่ได้
- [ ] login ด้วยรหัสใหม่ + OTP ผ่าน
- [ ] เปิดแอปใหม่ → /me sync user
- [ ] token หมดอายุ → logout อัตโนมัติ
