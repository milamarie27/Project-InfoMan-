# 🎓 Online Clearance System

A complete student clearance management system built with **ASP.NET Core 8 Web API** + **Vanilla JS frontend**, using **MySQL** on Heliohost.

---

## 📁 Project Structure

```
OnlineClearance/
├── OnlineClearance.sln
├── database_setup.sql               ← Run this on MySQL first
├── OnlineClearance.API/             ← ASP.NET Core 8 Backend
│   ├── Controllers/
│   │   ├── AuthController.cs        ← Login, Register
│   │   ├── StudentsController.cs    ← Student CRUD
│   │   ├── ClearanceController.cs   ← Clearance management
│   │   ├── SetupControllers.cs      ← Courses, Curriculum, Periods, Subjects, Offerings, Orgs
│   │   ├── MiscControllers.cs       ← Announcements, Signatories, Status
│   │   └── ReportsController.cs     ← Reports + Excel Export
│   ├── Models/
│   │   └── Entities.cs              ← All EF Core entities
│   ├── Data/
│   │   └── AppDbContext.cs          ← DbContext + seed data
│   ├── DTOs/
│   │   └── Dtos.cs                  ← All request/response DTOs
│   ├── Helpers/
│   │   └── JwtHelper.cs             ← JWT token generation
│   ├── appsettings.json             ← DB connection string + JWT config
│   └── Program.cs                   ← App startup, middleware, auto-migrate
└── OnlineClearance.Frontend/        ← HTML/CSS/JS Frontend
    ├── index.html
    ├── css/app.css
    └── js/app.js
```

---

## 🚀 Setup Instructions

### Step 1 — Database

1. Open **phpMyAdmin** at Heliohost (or use MySQL Workbench / HeidiSQL)
2. Connect to: `server=johnny.heliohost.org` with the provided credentials
3. Run the entire `database_setup.sql` file
4. This creates all tables, constraints, seed data, and a default admin user

**Default Admin Credentials:**
- Username: `admin`
- Password: `Admin@1234`

---

### Step 2 — Backend (ASP.NET Core API)

#### Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

#### Run locally

```bash
cd OnlineClearance/OnlineClearance.API
dotnet restore
dotnet run
```

The API will start at `http://localhost:5000`

Swagger UI: `http://localhost:5000/swagger`

#### EF Core Migrations (if needed)

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> **Note:** The app auto-runs migrations on startup via `db.Database.Migrate()` in `Program.cs`. If you ran `database_setup.sql` manually, you can remove that call to avoid conflicts.

---

### Step 3 — Frontend

Open `OnlineClearance.Frontend/index.html` in a browser.

> If the API is running on a different port, update the `API` constant at the top of `js/app.js`:
> ```js
> const API = 'http://localhost:5000/api';
> ```

For a live server, use **VS Code Live Server** extension, or:
```bash
npx serve OnlineClearance.Frontend
```

---

## 🔐 User Roles

| Role | Access |
|------|--------|
| `admin` | Full access: setup, students, clearances, reports, announcements |
| `signatory` | View/approve assigned clearances, post announcements |
| `student` | View own clearance status, view announcements |

---

## 📋 API Endpoints Summary

### Auth
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login |
| POST | `/api/auth/register/student` | Register student |
| POST | `/api/auth/register/signatory` | Register signatory (admin only) |

### Students
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/students` | List all students |
| GET | `/api/students/{id}` | Get by ID |
| GET | `/api/students/by-number/{num}` | Get by student number |
| PUT | `/api/students/{id}` | Update student |
| DELETE | `/api/students/{id}` | Delete student |

### Clearance
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/clearance/generate` | Generate clearance entries |
| GET | `/api/clearance/summary/{studentId}/{periodId}` | Student clearance summary |
| GET | `/api/clearance/subjects` | List subject clearances |
| PUT | `/api/clearance/subjects/{id}/approve` | Approve/reject subject |
| POST | `/api/clearance/subjects/bulk-approve` | Bulk approve |
| GET | `/api/clearance/organizations` | List org clearances |
| PUT | `/api/clearance/organizations/{id}/approve` | Approve/reject org |

### Reports
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/reports/clearance` | Full clearance report |
| GET | `/api/reports/cleared` | Cleared students |
| GET | `/api/reports/pending` | Pending students |
| GET | `/api/reports/export` | Download Excel |

### Setup (Admin Only)
- `/api/courses` — Course CRUD
- `/api/curriculum` — Curriculum CRUD
- `/api/periods` — Academic Period CRUD + activate
- `/api/subjects` — Subject CRUD
- `/api/offerings` — Subject Offering CRUD
- `/api/organizations` — Organization CRUD
- `/api/signatories` — Signatory management
- `/api/announcements` — Announcements CRUD

---

## 🔄 Clearance Workflow

```
Admin activates Academic Period
        ↓
Admin generates clearance entries per student
(creates rows in clearance_subjects + clearance_organization)
        ↓
Student logs in → views pending clearances
        ↓
Instructors approve/reject subject clearances
Org signatories approve/reject org clearances
        ↓
When ALL entries = Cleared → student is "Fully Cleared"
        ↓
Admin exports Excel report
```

---

## 📦 NuGet Packages Used

| Package | Purpose |
|---------|---------|
| `Pomelo.EntityFrameworkCore.MySql` | MySQL driver for EF Core |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT authentication |
| `BCrypt.Net-Next` | Password hashing |
| `Swashbuckle.AspNetCore` | Swagger UI |
| `ClosedXML` | Excel export |

---

## ⚙️ Configuration

`appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=johnny.heliohost.org;database=smileyface_OnlineClearance;user=smileyface_OnlineClearance;password=Altheajean1120;AllowUserVariables=true;SslMode=None;"
  },
  "JwtSettings": {
    "SecretKey": "OnlineClearanceSecretKey2024!SuperSecure#JWT",
    "Issuer": "OnlineClearanceAPI",
    "Audience": "OnlineClearanceClient",
    "ExpiryInHours": 8
  }
}
```

---

## 🛡️ Security Notes

- All passwords are hashed with **BCrypt**
- All endpoints (except login/register) require a **JWT Bearer token**
- Role-based authorization enforced per controller action
- CORS is configured to allow all origins (restrict in production)

---

## 🐛 Troubleshooting

| Issue | Fix |
|-------|-----|
| `Connection refused` | Make sure API is running on port 5000 |
| `CORS error` in browser | Check the API CORS config in `Program.cs` |
| `Migration error` | Run `database_setup.sql` manually then comment out `db.Database.Migrate()` |
| `SSL error` with MySQL | `SslMode=None` is already set in the connection string |
| Login returns 401 | Check username/password; default admin is `admin` / `Admin@1234` |
