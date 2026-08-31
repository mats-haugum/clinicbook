# Running the project locally on Windows 11

This is a lightweight **local dev** setup - plain `docker run` containers plus
`dotnet run` / `npm run dev` - not the same as `deploy/docker-compose.yml`,
which builds production images behind Caddy and is meant for the Ubuntu
server, not your dev machine.

Prerequisites (already installed if you've followed along): Docker Desktop,
the .NET SDK, the EF Core CLI tool (`dotnet tool install --global
dotnet-ef` if `dotnet ef --version` doesn't work), and Node.js.

## 1. Start SQL Server and Redis in Docker

```powershell
docker run -d --name clinicbook-mssql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Admin@123" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
docker run -d --name clinicbook-redis -p 6379:6379 redis:7-alpine
```

The password **must** be `Admin@123` - `appsettings.json` and the integration
tests' `CustomWebApplicationFactory.cs` both expect it (a SQL Server instance
has exactly one `sa` password, so both have to agree).

Give SQL Server about 15-20 seconds to finish initializing before the next
step - first boot does some internal setup.

## 2. Apply EF Core migrations (creates the database schema + seed data)

```powershell
cd Backend\ClinicAppointmentBookingSystem
dotnet ef database update
```

This is Entity Framework Core's migration tool - it reads the `Migrations/`
folder and applies any that haven't run yet against `ClinicBookingDB`,
including the `HasData` seed rows (specialities, clinics, doctors,
categories).

## 3. Run the backend

```powershell
dotnet run
```

Leave this running. It listens on `http://localhost:5291` (see
`Properties/launchSettings.json`). Swagger docs are at
`http://localhost:5291/doc`.

## 4. Run the frontend (new terminal)

```powershell
cd Frontend
npm install
npm run dev
```

`Frontend/.env` already points `VITE_API_URL` at `http://localhost:5291`, so
no config needed. Vite will print the local URL (usually
`http://localhost:5173`) - open that in a browser.

## 5. Run the tests yourself

```powershell
cd Backend\ClinicAppointmentBookingSystem.IntegrationTests
dotnet test
```

Requires the SQL Server container from step 1 to be running (the tests wipe
and recreate their own `ClinicBookingDB_Test` database automatically each
run - nothing else to set up).

## Cleaning up afterward

```powershell
docker stop clinicbook-mssql clinicbook-redis
docker rm clinicbook-mssql clinicbook-redis
```

Stopping without removing also works if you want to resume later without
redoing steps 1-2:

```powershell
docker start clinicbook-mssql clinicbook-redis
```
