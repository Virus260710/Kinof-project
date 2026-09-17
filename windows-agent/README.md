# KINOF Windows Agent (MVP)

โปรแกรมบนเครื่องแล็บ: ผูกที่นั่งด้วย API key, ส่ง heartbeat, มีหน้าล็อกอินบัญชี KINOF บล็อกเว็บตามรายการ Monitor และปิดโปรแกรมที่แอดมินบล็อก

- ล็อกอินสำเร็จ → ที่นั่ง Occupied และผูกผู้ใช้จากบัญชี KINOF (ไม่ใช้ชื่อ user Windows)
- ออกจากระบบที่ Agent / ผู้ดูแลสั่งออกจากระบบ → Available
- ไม่ต้องผ่าน Kiosk ก่อน (สูตร ก)
- รหัสฉุกเฉินจากเว็บใช้ที่ประตู Kiosk เท่านั้น ไม่ใช่รหัสผ่านเครื่อง
- บล็อกเว็บ: ดึง `GET /api/agent/website-blacklist` แล้วเขียนเฉพาะช่วง `# KINOF-BLOCK-START` … `# KINOF-BLOCK-END` ใน `C:\Windows\System32\drivers\etc\hosts` (ไม่ทับทั้งไฟล์) — รายการมาจากโดเมนที่ใส่เองและหมวด UT1 ที่นำเข้าแบบจำกัดจำนวน
- บล็อกโปรแกรม: ดึง `GET /api/agent/program-blacklist` ตามรอบ heartbeat แล้วปิด process ที่ตรง `process_name` (เช่น `discord.exe`) — ไม่ปิดเครื่อง ไม่ปิดตัว Agent ไม่ปิด process ระบบ Windows
- รายการอนุญาต: ดึง `programAllowlist` จาก heartbeat แล้วสรุปโปรแกรมที่ไม่รู้จักเป็น event `unknown_program` (ไม่ขึ้นคิว Monitor ทีละแถว ไม่หักคะแนน)

## Build / รัน (ต้องเป็น Administrator)

การเขียน hosts ต้องได้สิทธิ์ Administrator — เปิด **PowerShell แบบ Run as administrator** แล้ว:

```powershell
cd C:\Users\User\Desktop\Kinof-project\windows-agent
dotnet build
```

Backend บนเครื่องโฮสต์ต้องเปิดที่พอร์ต `5106` ก่อน

ใส่ API key ใน `appsettings.json`:

```json
"Kinof": {
  "ApiBaseUrl": "http://localhost:5106",
  "ApiKey": "dev-agent-key-1",
  "HeartbeatIntervalSeconds": 20,
  "BlacklistSyncIntervalSeconds": 120,
  "ProcessScanIntervalSeconds": 3,
  "UnknownProgramReportIntervalSeconds": 60
}
```

หรือตั้งตัวแปร `Kinof__ApiKey` แทนการเขียนลงไฟล์

คีย์ dev (seeder): `dev-agent-key-1` = ที่นั่ง 1 ห้องแรก, `dev-agent-key-2` = ที่นั่ง 2

สร้างคีย์จริง: แอดมิน `POST /api/admin/agents` ด้วย `{ "seatId": "..." }` — API ส่ง `apiKey` กลับมาครั้งเดียว

รันแบบมีหน้าต่างล็อกอิน จาก PowerShell ที่ยกสิทธิ์แล้ว (อย่าติดตั้งเป็น Windows Service ในรอบนี้ เพราะต้องมี UI):

```powershell
dotnet run
```

ถ้าเปิด `Kinof.Agent.exe` โดยตรง Windows จะขึ้น UAC ให้รัน as Administrator

กดปิดหน้าต่างหรือย่อ = โปรแกรมยังทำงานที่ **ไอคอนถาดระบบ** (โล่) ดับเบิลคลิกเพื่อเปิดหน้าล็อกอินอีกครั้ง ที่นั่งว่างเมื่อกด **ออกจากระบบเครื่องนี้** เท่านั้น

ปิดโปรแกรมจริง: คลิกขวาไอคอนถาด → ปิดโปรแกรม (ถ้ายังล็อกอินอยู่ จะถามก่อนแล้วค่อยออกจากระบบ)

หรือหยุดจาก PowerShell ด้วย Ctrl+C

### Agent อยู่ใน VMware (NAT)

`localhost` ใน VM คือตัว VM เอง ไม่ใช่เครื่องโฮสต์ที่รัน backend — ตั้ง `Kinof:ApiBaseUrl` เป็น IP ของโฮสต์ เช่น `http://192.168.232.1:5106`

บนโฮสต์ให้ backend ฟังทุก interface ไม่ใช่แค่ localhost:

```powershell
dotnet run --project .\Kinof.Api\Kinof.Api.csproj --urls http://0.0.0.0:5106
```

ใน VM ดู IP โฮสต์ได้จาก default gateway ของอะแดปเตอร์ NAT (`ipconfig`) แล้วอนุญาตพอร์ต 5106 ใน Windows Firewall ของโฮสต์

## ทดสอบบล็อกเว็บ (facebook.com)

รายการ seed มี `facebook.com` อยู่แล้ว (แท็บ **บล็อกเว็บ** ใน Monitor)

1. รัน Agent as Administrator จน log บอกว่าอัปเดต hosts แล้ว
2. เปิด `C:\Windows\System32\drivers\etc\hosts` ต้องมีช่วง `# KINOF-BLOCK-START` … `# KINOF-BLOCK-END` และมี `127.0.0.1 facebook.com`
3. `ping facebook.com` ต้องชี้ไป `127.0.0.1` — เปิดเบราว์เซอร์ไป facebook.com ต้องเข้าไม่ได้
   (ถ้า Chrome/Edge ยังเข้าได้ ให้ปิด **Use secure DNS** แล้ว `ipconfig /flushdns`)
4. ที่ Monitor นำ `facebook.com` ออกจากรายการบล็อกเว็บ รอ sync (ค่าเริ่มต้น 2 นาที) หรือรีสตาร์ท Agent
5. ช่วง KINOF ใน hosts ต้องไม่มีโดเมนนั้นแล้ว และ `ping facebook.com` ต้องไม่ชี้ 127.0.0.1

## ทดสอบบล็อกโปรแกรม (discord.exe)

รายการ seed มี `discord.exe` และ `steam.exe` อยู่แล้ว (แท็บ **บล็อกโปรแกรม** ใน Monitor)

1. รัน Agent as Administrator จน log บอกว่าใช้รายการบล็อกโปรแกรมแล้ว
2. เปิด Discord (หรือโปรแกรมที่อยู่ในรายการ) — ภายในไม่กี่วินาทีต้องถูกปิด
3. ที่ Monitor แท็บ **โปรแกรม** หรือ **น่าสงสัย** ต้องมีแถว **บล็อก discord.exe**
4. นำ `discord.exe` ออกจากรายการ รอ heartbeat รอบถัดไป แล้วเปิด Discord ได้อีก
5. ใส่ `explorer.exe` หรือ `csrss.exe` ในรายการแล้วตรวจ log — Agent ต้องข้าม ไม่ปิด process ระบบ

โปรแกรมที่ Agent ไม่ปิดแม้จะอยู่ในรายการ: ตัว Agent เอง, process แม่ (`dotnet` ตอน `dotnet run`), process session 0, และรายชื่อระบบใน `ProgramProcessBlocker`

## ทดสอบหมวดเว็บ UT1

1. Login admin → Monitor → **บล็อกเว็บ** → เลือกหมวด เช่น โซเชียลเน็ตเวิร์ก → **นำเข้าหมวดนี้**
2. ต้องได้ประมาณ 250 โดเมน (`category = social_networks`, `source = ut1`) ไม่ใช่ทั้งไฟล์หลายหมื่นโดเมน
3. รัน Agent as Administrator จนอัปเดต hosts — ไฟล์ hosts ต้องมีโดเมนจากหมวดนั้น
4. แท็บ **อนุญาตโปรแกรม** ต้องมี seed เช่น `chrome.exe`, `code.exe`
5. เปิดโปรแกรมที่ไม่อยู่ในรายการอนุญาตและรายการห้าม (เช่นเครื่องคิดเลขของบุคคลที่สาม) แล้วรอประมาณ 1 นาที — แท็บ **ไม่รู้จัก** ต้องมีแถวสรุปจำนวนครั้ง/เครื่อง **ไม่ขึ้นแท็บน่าสงสัย**

## ทดสอบที่นั่ง

1. เปิด Kiosk สแกนหน้าหรือ OTP ฉุกเฉิน → ได้แค่สิทธิ์เข้าห้อง **ที่นั่งยังไม่ถูกจอง**
2. รัน Agent แล้วล็อกอินบัญชี KINOF (รหัสผ่าน + OTP อีเมล) โดยไม่ต้องสแกนประตูก่อน → ที่นั่ง Occupied
3. คืนที่นั่งอย่างใดอย่างหนึ่ง:
   - ปุ่ม **ออกจากระบบเครื่องนี้** บนหน้า Agent (อย่าแค่ปิดหน้าต่าง)
   - หรือ `.\scripts\simulate-agent.ps1 -Logout` (คีย์เดียวกับที่นั่งนั้น)
   - หรือแอดมินกด **สั่งออกจากระบบ** ในหน้า Tracking เมื่อ Agent ออนไลน์
     (ถ้ายังไม่ได้ติดตั้ง Agent จะปล่อยที่นั่งจากเซิร์ฟเวอร์ได้ — Agent ออฟไลน์สั่งไม่ได้)

อย่าใช้ปุ่ม Logout ของเว็บนักศึกษา และไม่มีปุ่มออกจากห้องบนจอ Kiosk
