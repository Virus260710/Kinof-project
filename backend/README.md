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

`bumail.net` uses Google mail servers. The repository is configured for
`smtp.gmail.com` and `kittisak.sati@bumail.net`, but the password is never
stored in source control. Create a Google App Password for that account and
store it with .NET user secrets:

```powershell
dotnet user-secrets set "Email:Password" "YOUR-16-CHAR-APP-PASSWORD" `
  --project .\Kinof.Api\Kinof.Api.csproj
```

Restart the API after setting the secret. If the account or its Google
Workspace administrator does not allow App Passwords, SMTP cannot authenticate
until that policy is enabled. Development falls back to showing the OTP in the
API console and the OTP page explicitly reports that fallback.

The frontend uses `http://localhost:5106` by default. Override it with
`VITE_API_URL` when needed.
