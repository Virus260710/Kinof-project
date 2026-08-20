# Prompt สำหรับ Agent — Kinof GitHub Repo

> **ใช้ repo จริง:** https://github.com/Virus260710/Kinof-project.git  
> ดู: `docs/GITHUB_SETUP.md`

---

## Prompt — ทำต่อจาก GitHub (Composer 2.5 Fast)

```
ทำต่อ Kinof-project จาก GitHub

Clone/Open: C:\Users\User\Desktop\Kinof-project
Remote: https://github.com/Virus260710/Kinof-project.git

อ่าน:
- docs/GITHUB_SETUP.md
- docs/EMAIL_OTP.md, DATABASE.md, UI_AUTH_PHASE.md

Frontend มีแล้ว: kinof-app/ (React mock — แทน fakeLogin ด้วย API)

ทำ:
1. .gitignore (exclude node_modules)
2. backend/Kinof.Api — EF Core, 15 ตาราง, MailKit Email OTP
3. POST login → send email OTP, verify-email-otp, resend
4. ปรับ Login.jsx + เพิ่ม OtpVerify page + react-router-dom
5. Branch: cursor/phase0-backend-email-otp

OTP ทาง email — ไม่ใช้ Google Authenticator
ไม่มี Tracking Agent
```

---

## Checklist

- [ ] `git clone` แล้ว copy `docs/` จาก projectfinal
- [ ] Open Folder: Kinof-project
- [ ] Mode: Agent
