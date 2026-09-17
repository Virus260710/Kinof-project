# KINOF API

ASP.NET Core 8 + EF Core + SQLite backend for password login and email OTP.

## Run locally

```powershell
dotnet run --project .\Kinof.Api\Kinof.Api.csproj --launch-profile http
```

The API listens on `http://localhost:5106`. The SQLite database and all 15
tables are created from the initial migration on first start.

Development seed accounts:

- `student` / `Student123!`
- `admin` / `Admin123!`

Change these passwords before using a shared environment.

## SMTP (Resend)

เก็บค่า SMTP ที่ **dotnet user-secrets** ของ `Kinof.Api` — **ห้าม commit API key** และอย่าใส่ใน `appsettings.json`  
ถ้าใส่ครบแล้ว **อย่า set `Email:Password` ซ้ำ**

```powershell
dotnet user-secrets set "Email:SmtpHost" "smtp.resend.com" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:SmtpPort" "587" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:Username" "resend" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:Password" "re_xxxxxxxx" --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:FromAddress" "onboarding@resend.dev" --project .\Kinof.Api\Kinof.Api.csproj
```

`onboarding@resend.dev` ส่งได้เฉพาะเมลเจ้าของบัญชี Resend  
ถ้าจะทดสอบ OTP ใน inbox จริง ให้ชี้ seed student ไปที่เมลเจ้าของบัญชี:

```powershell
dotnet user-secrets set "Seed:StudentEmail" "your-resend-account@gmail.com" --project .\Kinof.Api\Kinof.Api.csproj
```

รีสตาร์ท API หลังเปลี่ยน secrets เมื่อส่งสำเร็จ `deliveryMode` เป็น `smtp` และหน้าเว็บไม่โชว์ OTP

**Development fallback:** ถ้าไม่มี password หรือ SMTP ส่งไม่สำเร็จ API log OTP/ลิงก์ใน console — OTP ยังใช้ได้  
**Production:** ไม่มี password หรือส่งล้ม → เริ่มต้นไม่ขึ้น หรือ API ตอบ 503 ไม่แอบสำเร็จ

The frontend uses `http://localhost:5106` by default. Override it with
`VITE_API_URL` when needed.

## Face enrollment

`POST /api/auth/register/face` รับภาพแบบ data URL จาก frontend แล้วส่งต่อใน
หน่วยความจำไปยัง InsightFace service ที่ `http://localhost:8001` จากนั้น API
เก็บเฉพาะ embedding 512 มิติในฐานข้อมูล ไม่เก็บรูปภาพ

รัน Face Service ตามขั้นตอนใน `../face-service/README.md` หรือเปลี่ยน URL ผ่าน
configuration key `FaceService:BaseUrl`
