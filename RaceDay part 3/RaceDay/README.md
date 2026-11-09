# RaceDay

| Project | What it is |
|---|---|
| `RaceDay.Api` | Part 2: ASP.NET Core Web API (EF Core, SQL Server, session login, Swagger) |
| `RaceDay.Web` | Part 3: ASP.NET Core MVC front end. Gets **all** data from the API over HTTP (no database access, no duplicated business rules) |
| `RaceDay.Tests` | xUnit tests for the API |

## System description
**Organisers** create events (banner, description, date, location, distance, type), add distance/age categories (with price and capacity), confirm enrolments and capture then publish results.
**Participants** browse and filter upcoming events, enrol in a category, follow their enrolment status (Pending / Confirmed), see published results (for example "47th of 312 finishers") and manage their profile and picture.

## Database
The app uses the **existing `RaceDayDB`** database on `localhost\SQLEXPRESS01` (tables `Users`, `Events`, `Categories`, `Entries`, `Results`). No migrations are used and nothing is recreated.

1. Run `docs/part3_upgrade.sql` once in SSMS. It **only adds** the columns Part 3 needs (banner, distance, age limits, results published flag, profile fields). Existing rows are kept. It is safe to run twice.
2. *(Optional)* run `docs/part3_set_test_passwords.sql`. The original seed users hold placeholder passwords, so this gives the 4 existing users the password `Password123!` (it changes only their `password_hash`). Or simply register new accounts in the app.

Connection string: `RaceDay.Api/appsettings.json` (`Server=localhost\SQLEXPRESS01;Database=RaceDayDB;...`). Change the server name if yours differs.

Roles and statuses are stored lowercase (`organiser`, `participant`, `pending`, `confirmed`, `cancelled`).

## How the two apps work together
1. The user logs in on the MVC site. MVC calls `POST /api/auth/login`.
2. The API creates a session and returns a session cookie. MVC keeps that cookie **inside the user's own MVC session** (with the user's name and role) and attaches it to every later API call.
3. The MVC session drives navigation and page access (role filters). The API still checks the role on every call, so rules are never replicated in MVC.
4. Logging out clears both sessions. An expired session sends the user back to the login page.
5. Uploaded images are stored by the API; MVC streams them through `/media`, so the browser only talks to MVC.

## Run it

### 1. Start the API (must be on http://localhost:5080)
```bash
dotnet run --project RaceDay.Api --launch-profile http
```
Swagger: http://localhost:5080/swagger

### 2. Start the MVC app
```bash
dotnet run --project RaceDay.Web
```
Open http://localhost:5090

### Visual Studio
Solution > Set Startup Projects > **Multiple startup projects**: start `RaceDay.Api` and `RaceDay.Web`.

## Existing users (from Part 1)
| Email | Role |
|---|---|
| thandiwe.nkosi@raceday.co.za | organiser |
| pieter.vanwyk@raceday.co.za | organiser |
| lindiwe.dube@example.com | participant |
| sipho.mahlangu@example.com | participant |

Password `Password123!` after running the optional password script.

## Tests
```bash
dotnet test
```

## Docs
`docs/endpoints.md`, `docs/erd.md`, `docs/part3_upgrade.sql`, `docs/part3_set_test_passwords.sql`

## CI
(Add a screenshot of the green GitHub Actions run here.)

## Video
(Add your unlisted YouTube link here.)
