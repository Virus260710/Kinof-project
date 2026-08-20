# เริ่มต้น Smart Lab (KINOF) — ตั้งแต่แชทใหม่

## โฟลเดอร์โปรเจกต (ใช้ path นี้)

```
C:\Users\User\Desktop\projectfinal
```

> โฟลเดอร์นี้ยังว่าง — **copy โปรเจกตจาก** `C:\Users\User\Projects\smartlab` **มาที่นี่ก่อน** (ดูขั้นตอนด้านล่าง)

**หลัง copy จะมี (ไม่รวม Tracking Agent):**
- `docs/` — แผน, DB schema, flows, prompts
- `README.md`

**ไม่ copy จาก smartlab:**
- `agent/` — Tracking Agent (เก็บไว้ที่ smartlab เดิม)
- `backend/SmartLab.Api/` แบบ Agent-only — จะสร้าง backend ใหม่ใน Phase 0

**Phase 0 สร้างใหม่ใน projectfinal:**
- `frontend/` — หน้า Login KINOF 7 หน้า
- `backend/` — Auth API + DB 14 ตาราง
- Tracking Agent → **Phase ภายหลัง** (หรือใช้จาก smartlab แยก repo)

---

## ย้ายมา Desktop\projectfinal (ไม่เอา Tracking Agent)

เปิด **PowerShell** แล้วรัน — **copy เฉพาะ docs + README**:

```powershell
$src = "C:\Users\User\Projects\smartlab"
$dst = "C:\Users\User\Desktop\projectfinal"

New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item -Recurse -Force "$src\docs" "$dst\docs"
Copy-Item -Force "$src\README.md" "$dst\README.md"

# Git ใหม่
cd $dst
git init
git checkout -b cursor/phase0-kinof-login
git add docs README.md
git commit -m "Add KINOF planning docs and database design for Phase 0 login."
```

> **ไม่ copy:** `agent/`, `backend/` (Tracking Agent อยู่ smartlab เดิม)

จากนั้นใน Cursor: **File → Open Folder** → `C:\Users\User\Desktop\projectfinal`

---

## ขั้นตอนเริ่มแชทใหม่ (ทีละขั้น)

### ขั้น 1 — ติดตั้ง Prerequisites

| โปรแกรม | ดาวน์โหลด | ตรวจสอบ |
|---------|-----------|---------|
| **Node.js 20 LTS** | https://nodejs.org | `node --version` |
| **.NET 8 SDK** | https://dotnet.microsoft.com/download/dotnet/8.0 | `dotnet --version` |
| **Git** | มีแล้วในโปรเจกต | `git --version` |

### ขั้น 2 — เปิด Workspace ใน Cursor

1. Cursor → **File → Open Folder**
2. เลือก `C:\Users\User\Projects\smartlab` (หรือ path ใหม่ถ้าย้าย)
3. รอ index เสร็จ

### ขั้น 3 — ตั้งค่า Chat

| การตั้งค่า | ค่าที่ใช้ |
|-----------|----------|
| **Mode** | **Agent** (ไม่ใช่ Plan) |
| **Model รอบ 1** | **Composer 2.5 Fast** |
| **Model รอบ 2** | Sonnet 5 Thinking (Face scan) |

### ขั้น 4 — สร้างแชทใหม่ + วาง Prompt

1. **New Chat** (Ctrl+L หรือ New Agent)
2. Copy prompt จาก `docs/NEXT_AGENT_PROMPT.md` → **Prompt รอบ 1**
3. แก้ path ถ้าย้ายโฟลเดอร์
4. ส่ง

### ขั้น 5 — ตรวจหลัง Agent ทำเสร็จ

```powershell
cd C:\Users\User\Desktop\projectfinal   # path โปรเจกต

# Backend
dotnet run --project backend/SmartLab.Api
# → http://localhost:5000

# Frontend (หลัง Agent สร้าง)
cd frontend
npm install
npm run dev
# → http://localhost:5173
```

---

## ลำดับ Phase ทั้งโปรเจกต (projectfinal)

```
Phase 0  ← เริ่มที่นี่ (แชทใหม่)
  Login + Register + DB 14 ตาราง + Frontend KINOF
  (backend ใหม่ทั้งหมด — ไม่เอา Tracking Agent จาก smartlab)

Phase 1  ← ทำทีหลัง / แยก repo
  Tracking Agent (อ้างอิง docs ใน smartlab เดิม ถ้าต้องการ)

Phase 2
  Kiosk สแกนหน้าเข้าห้อง + OTP สำรอง

Phase 3
  จองห้อง + Dashboard
```

**smartlab เดิม** (`C:\Users\User\Projects\smartlab`) เก็บ Tracking Agent ไว้ใช้/reference แยก — **ไม่ merge เข้า projectfinal**

---

## เอกสารสำคัญ (ให้ Agent อ่าน)

| ไฟล์ | ใช้เมื่อ |
|------|---------|
| `docs/NEXT_AGENT_PROMPT.md` | Copy prompt แชทใหม่ |
| `docs/DATABASE.md` | Schema 14 ตาราง |
| `docs/UI_AUTH_PHASE.md` | หน้า Login 7 หน้า |
| `docs/FLOWS.md` | User flow 3 กลุ่ม |
| `docs/AUTH_ADAPTIVE.md` | Face + OTP สำรอง |

---

## แชทใหม่แยกกี่รอบ?

| รอบ | Model | งาน |
|-----|-------|-----|
| **1** | Composer 2.5 Fast | Login UI + Auth API + DB (backend ใหม่ — ไม่มี Agent) |
| **2** | Sonnet 5 Thinking | FaceCamera + liveness |
| **3** | Composer 2.5 Fast | จองห้อง + Dashboard |
| **4** | Composer 2.5 Fast | Kiosk + (optional) Tracking Agent ใหม่ |

แต่ละรอบ = **New Chat** + prompt ใหม่ + อ้าง docs

---

## Checklist ก่อนกดส่ง Prompt

- [ ] เปิด folder ถูก path
- [ ] Mode = **Agent**
- [ ] ติดตั้ง Node.js + .NET 8 SDK แล้ว
- [ ] Copy prompt จาก `NEXT_AGENT_PROMPT.md`
- [ ] แก้ path ใน prompt ถ้าย้ายโฟลเดอร์
