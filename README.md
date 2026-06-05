# ClinicBook — Clinic Appointment Booking System

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat&logo=dotnet&logoColor=white)
![React](https://img.shields.io/badge/React_19-20232A?style=flat&logo=react&logoColor=61DAFB)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=flat&logo=typescript&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=flat&logo=microsoftsqlserver&logoColor=white)

> **Exam Project — Backend Development, Year 2**  
> Noroff School of Technology and Digital Media

---

### About

ClinicBook is a full-stack web application that allows patients to book appointments at medical clinics — no account required. Patients can book as a guest, or register to manage their appointments online. Registered patients can view, reschedule, and cancel their bookings. An admin panel allows clinic staff to manage doctors, clinics, specialities, and appointment categories.

The system is built as a REST API (ASP.NET Core) backed by SQL Server via Entity Framework Core, with a React + TypeScript frontend. Authentication uses JWT access tokens with refresh token rotation.

---

### Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 19, TypeScript, Vite, Tailwind CSS |
| Backend | ASP.NET Core (.NET 10), C# |
| Database | SQL Server 2022 (Docker), Entity Framework Core |
| Authentication | JWT (access + refresh tokens), Argon2id password hashing |
| API Docs | Swagger / OpenAPI at `/doc` |
| Testing | xUnit, FluentAssertions, ASP.NET Core integration tests |

---

### HOW TO RUN

#### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server)

---

#### 1. Start the database

The application uses SQL Server running in a Docker container.

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Admin@123" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

> If the container already exists from a previous run, start it with:
> ```bash
> docker start sqlserver
> ```

---

#### 2. Run the backend

```bash
cd Backend/ClinicAppointmentBookingSystem
dotnet run
```

The API starts at `http://localhost:5000`.  
On first run, EF Core automatically applies migrations and seeds the database.

The default admin account is created automatically:
- **Email:** `admin@clinicbook.com`
- **Password:** `Admin@123`

Swagger UI is available at `http://localhost:5000/doc`.

---

#### 3. Run the frontend

In a separate terminal:

```bash
cd Frontend
npm install
npm run dev
```

The frontend starts at `http://localhost:5173`.

---

#### 4. Run the integration tests (optional)

The tests require the SQL Server container to be running (step 1). They use a separate database (`ClinicBookingDB_Test`) that is created and wiped automatically.

```bash
cd Backend/ClinicAppointmentBookingSystem.IntegrationTests
dotnet test
```

---

### ENDPOINTS

The REST API base URL is `http://localhost:5000` (configurable). Full interactive documentation is available at `/doc` (Swagger UI).

Endpoints marked **🔒 Patient** require a `Bearer` JWT token with role `Patient` in the `Authorization` header.  
Endpoints marked **🔒 Admin** require a `Bearer` JWT token with role `Admin`.  
All other endpoints are public.

---

#### Authentication — `/auth`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/auth/register` | Public | Registers a new patient account. Returns a JWT token and patient info. Returns `409` if the email is already registered. |
| POST | `/auth/login` | Public | Logs in an existing patient. Returns a JWT access token and a refresh token. Returns `401` on invalid credentials. |
| POST | `/auth/refresh` | Public | Issues a new access token and refresh token using a valid refresh token (token rotation). Returns `401` if the token is invalid or expired. |
| GET | `/auth/guest-prefill?email=` | Public | Returns non-sensitive PII (name, birthdate, gender) for a guest booking, used to pre-fill the registration form. Returns `404` if no guest booking exists for that email. |

---

#### Admin Authentication — `/admin/auth`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/admin/auth/login` | Public | Logs in an admin user. Returns a JWT with role `Admin`. No refresh token is issued — admins must re-authenticate when the token expires. |

---

#### Appointments — `/appointments`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/appointments/book/guest` | Public | Books an appointment as a guest user. Stores non-sensitive PII only. Returns `409` if the time slot conflicts with an existing appointment. |
| POST | `/appointments/book` | 🔒 Patient | Books an appointment as a registered patient. Returns `409` on time slot conflict. |
| GET | `/appointments/my` | 🔒 Patient | Returns all appointments for the currently logged-in patient. |
| PUT | `/appointments/{id}/reschedule` | 🔒 Patient | Reschedules an existing appointment to a new date/time. Returns `403` if the appointment belongs to another patient. Returns `409` on time slot conflict. |
| DELETE | `/appointments/{id}/cancel` | 🔒 Patient | Cancels (deletes) an existing appointment. Returns `403` if the appointment belongs to another patient. |

---

#### Doctors — `/doctors`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/doctors` | Public | Returns all doctors. |
| GET | `/doctors/{id}` | Public | Returns a single doctor by ID. |
| GET | `/doctors/search?name=` | Public | Searches for doctors by first or last name. Returns each match with full name, clinic name, and speciality. Returns `404` if no matches are found. |
| GET | `/doctors/{id}/availability?date=` | Public | Returns 30-minute availability slots for a doctor on a given date (08:00–17:00). Each slot has a start time, end time, and an availability flag. |
| POST | `/doctors` | 🔒 Admin | Creates a new doctor and assigns them to one or more clinics. |
| PUT | `/doctors/{id}` | 🔒 Admin | Updates a doctor's name and speciality. |
| DELETE | `/doctors/{id}` | 🔒 Admin | Deletes a doctor. Returns `409` if the doctor has existing appointments. |

---

#### Clinics — `/clinics`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/clinics` | Public | Returns all clinics. |
| GET | `/clinics/{id}` | Public | Returns a single clinic by ID. |
| POST | `/clinics` | 🔒 Admin | Creates a new clinic. Returns `409` if a clinic with that name already exists. |
| PUT | `/clinics/{id}` | 🔒 Admin | Updates a clinic's details. Returns `409` on duplicate name. |
| DELETE | `/clinics/{id}` | 🔒 Admin | Deletes a clinic. Returns `409` if the clinic has existing appointments. |

---

#### Specialities — `/specialities`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/specialities` | Public | Returns all specialities. |
| GET | `/specialities/{id}` | Public | Returns a single speciality by ID. |
| POST | `/specialities` | 🔒 Admin | Creates a new speciality. Returns `409` if a speciality with that name already exists. |
| PUT | `/specialities/{id}` | 🔒 Admin | Updates a speciality's name. Returns `409` on duplicate name. |
| DELETE | `/specialities/{id}` | 🔒 Admin | Deletes a speciality. Returns `409` if doctors are still assigned to it. |

---

#### Appointment Categories — `/appointment-categories`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/appointment-categories` | Public | Returns all appointment categories. |
| GET | `/appointment-categories/{id}` | Public | Returns a single category by ID. |
| POST | `/appointment-categories` | 🔒 Admin | Creates a new appointment category. Returns `409` if a category with that name already exists. |
| PUT | `/appointment-categories/{id}` | 🔒 Admin | Updates a category's name. Returns `409` on duplicate name. |
| DELETE | `/appointment-categories/{id}` | 🔒 Admin | Deletes a category. Returns `409` if appointments are still assigned to it. |

---

### REFERENCES

- https://developer.mozilla.org/

- https://react.dev/

- https://learn.microsoft.com/en-us/dotnet/

- AI for learning and generating code i did not know how to do myself (Claude, ChatGPT)