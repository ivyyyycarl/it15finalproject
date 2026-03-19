# MonsterASP Update Deployment Guide

This guide shows how to deploy an updated version of your system to MonsterASP safely.

## 1) Build the latest release locally

Open PowerShell and run:

```powershell
cd "c:\Users\mukim\source\repos\INTEGRATE SUPPORT SALES MANAGEMENT SYSTEM\INTEGRATE SUPPORT SALES MANAGEMENT SYSTEM"

# Clean previous publish outputs (prevents nested artifacts folders)
Remove-Item ".\artifacts\api" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "..\deploy" -Recurse -Force -ErrorAction SilentlyContinue

dotnet ef database update --project ".\INTEGRATE SUPPORT SALES MANAGEMENT SYSTEM.csproj" --startup-project ".\INTEGRATE SUPPORT SALES MANAGEMENT SYSTEM.csproj"
dotnet publish ".\SupportSalesManagement.Frontend\SupportSalesManagement.Frontend.csproj" -c Release -o "..\deploy\frontend"
robocopy "..\deploy\frontend\wwwroot" ".\wwwroot" /MIR /R:2 /W:2
dotnet publish ".\INTEGRATE SUPPORT SALES MANAGEMENT SYSTEM.csproj" -c Release -o "..\deploy\api"
```

Deploy-ready output is:

`..\deploy\api`

---

## 2) Backup production before uploading

Do this in MonsterASP first:

- Backup current website files (at least `/wwwroot`)
- Backup production database (export `.sql`)

Recommended backup names:

- `wwwroot_backup_YYYY_MM_DD`
- `db_backup_YYYY_MM_DD.sql`

---

## 3) Open the correct folder in MonsterASP

1. Sign in to MonsterASP admin panel.
2. Open your website.
3. Go to **Files** (File manager).
4. Open `/wwwroot` (this is your live site root).

---

## 4) Upload the update

1. In `/wwwroot`, click **Upload file**.
2. From your PC, open:
   `...\INTEGRATE SUPPORT SALES MANAGEMENT SYSTEM\deploy\api`
3. Select and upload the **contents inside** `deploy\api` (not the parent folder itself).
4. When prompted, choose overwrite/replace.

Important:

- Upload in batches if browser upload is slow.
- If upload fails midway, run upload again for missing files only.

---

## 5) Production settings to verify

Ensure production values are correct:

- `ASPNETCORE_ENVIRONMENT=Production`
- `DatabaseSettings:ConnectionString`
- `JwtSettings:*`
- `Stripe:*`
- `EmailSettings:*`

If MonsterASP does not support machine env vars for your plan, use `web.config` environment variables.

---

## 6) Restart app/site

After upload:

- Restart or recycle app/site from MonsterASP panel.
- If no restart button is available, touching `web.config` usually restarts the app.

---

## 7) Smoke test after deployment

Test immediately:

- Site home page loads
- Login works
- API calls return data
- Notifications/SignalR still work

If something breaks:

1. Restore file backup (`wwwroot_backup_...`)
2. Restore DB backup if needed
3. Retry deployment

---

## 8) Optional: safer repeated deployments

For frequent updates:

- Keep publish output outside the API project folder (for example `..\deploy`).
- Always deploy from fresh `dotnet publish` output.
- Keep one backup from previous stable release.

