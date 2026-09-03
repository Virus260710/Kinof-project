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

## SMTP

Store credentials with .NET user secrets (never commit API keys):

```powershell
dotnet user-secrets set "Email:SmtpHost" "smtp.resend.com" `
  --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:SmtpPort" "587" `
  --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:Username" "resend" `
  --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:Password" "re_xxxxxxxxx" `
  --project .\Kinof.Api\Kinof.Api.csproj
dotnet user-secrets set "Email:FromAddress" "onboarding@resend.dev" `
  --project .\Kinof.Api\Kinof.Api.csproj
```

Restart the API after setting secrets.

**Resend test sender:** `onboarding@resend.dev` can only deliver to the Resend
account owner's email. To test OTP in a real inbox immediately, point the seed
student account at that address:

```powershell
dotnet user-secrets set "Seed:StudentEmail" "your-resend-account@gmail.com" `
  --project .\Kinof.Api\Kinof.Api.csproj
```

Restart the API; the seeder updates the existing `student` row on startup.

**Development fallback:** if SMTP is missing or sending fails, the API logs the
OTP in the backend console and the OTP page shows the yellow CMD hint. OTP
verification still works.

The frontend uses `http://localhost:5106` by default. Override it with
`VITE_API_URL` when needed.

## Face enrollment

`POST /api/auth/register/face` รับภาพแบบ data URL จาก frontend แล้วส่งต่อใน
หน่วยความจำไปยัง InsightFace service ที่ `http://localhost:8001` จากนั้น API
เก็บเฉพาะ embedding 512 มิติในฐานข้อมูล ไม่เก็บรูปภาพ

รัน Face Service ตามขั้นตอนใน `../face-service/README.md` หรือเปลี่ยน URL ผ่าน
configuration key `FaceService:BaseUrl`
