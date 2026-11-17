# LossLens

Web app for **daily production metrics** at an electronics factory (EMS). It shows how the shift/day is going and where exactly output is being lost.

> Status: MVP on generated demo data, PostgreSQL, JWT sign-in. Full requirements: [REQUIREMENTS.md](REQUIREMENTS.md).

## What it is and why

Operators, line supervisors and managers ask the same questions every day about the boards (PCB assemblies) the plant builds:

- Are we meeting the plan?
- How many boards pass the test on the first attempt?
- Which defects are the most frequent, and at which station?

LossLens calculates the answers in the backend and visualizes them on a dashboard with a color-coded status (green / amber / red). The name means "a lens on losses": see where good output is being lost.

## Metrics

| Metric | What it shows | Formula |
|---|---|---|
| **Plan attainment** | Share of the planned output actually produced as good units | `good / planned * 100` |
| **First Pass Yield (FPY)** | Share of units that pass a test on the first attempt, without rework or retest. Per station | `firstPass / tested * 100` |
| **RTY** (headline "Line FPY") | FPY of the whole line = product of station FPYs | `PRODUCT(stationFPY)` |
| **Defect analysis** | Pareto of defect causes plus DPMO | `defects / (units * opportunities) * 1e6` |

All formulas live **only in the backend** (`LossLens.Core`). The frontend only displays results.

## EMS and the product

**EMS is not a metric.** It is the domain: Electronics Manufacturing Services, i.e. contract manufacturing of electronics. The product is a **printed circuit board**. It goes through an SMT line and is checked at four stations: SPI (solder paste), AOI (optical), ICT (in-circuit) and FCT (functional). The metrics above are chosen for this process.

Push notifications to a phone or a smartwatch for supervisors are not in the MVP (open question Q1 in the requirements).

## Roles

| Role | Sees |
|---|---|
| Operator | Own line only: plan vs actual per hour, defects |
| Line Supervisor | Assigned lines: FPY by station, defect Pareto, trend |
| Manager | All lines: summary, comparison, trends |

Users sign in with a user name and password and get a JWT (60 minutes). The server reads the role and the lines of the user from the database on every request, so a new line or a changed assignment works with the same token. Another line gives `403`, an unknown id `404`, no token `401`, too many sign-in attempts `429`.

## Stack

- **Backend:** ASP.NET Core 9 / C#, EF Core + PostgreSQL. Layers: `Api` -> `Application` -> `Core`; `Infrastructure` -> `Core`
- **Frontend:** Angular + TypeScript (standalone components, signals), charts with Apache ECharts
- **Data:** a repository behind `IMetricsRepository`; demo data is generated into the database, a real source can write to the same tables without changing the API
- **UI language:** Ukrainian by default (strings in a resource file); code, comments, docs and commits are in English

## Project structure

```
LossLens/
├── README.md
├── REQUIREMENTS.md
├── docker-compose.yml             # database, backend, frontend (nginx)
├── design/                        # UI mockups
├── backend/
│   ├── src/
│   │   ├── LossLens.Api/          # thin controllers, JWT, errors, rate limit, Swagger
│   │   ├── LossLens.Application/  # services, DTOs, validation, scope rules (who sees which line)
│   │   ├── LossLens.Core/         # models, pure metric formulas, abstractions, no I/O
│   │   └── LossLens.Infrastructure/  # EF Core, migrations, repositories, security, demo data
│   └── tests/                     # Core, Application, Infrastructure, Api (endpoint tests on a real PostgreSQL)
└── frontend/
    └── src/app/
        ├── login/                 # sign-in page
        ├── dashboard/             # KPI cards
        ├── plan/                  # plan vs actual per hour
        ├── fpy/                   # trend and FPY by station
        ├── defects/               # Pareto, DPMO
        └── shared/                # api clients, auth, filters, charts, ui states, uk strings
```

## API (short)

Base path `/api/v1`: `/auth/login`, `/lines`, `/shifts`, `/kpi/summary`, `/plan-vs-actual`, `/fpy/trend`, `/fpy/stations`, `/defects/pareto`, `/config/thresholds`; `/health` is outside it. Every endpoint except login and health needs `Authorization: Bearer <token>`. Details are in section 6 of [REQUIREMENTS.md](REQUIREMENTS.md). Swagger UI is on `/swagger` in Development, or with `Swagger__Enabled=true`.

## Running

The backend needs PostgreSQL, a signing key for the tokens and, to sign in, a demo password. Secrets live in `.env`, which git ignores. Copy `backend/.env.example` (local run) or `.env.example` (Docker) to `.env` and set the values:

| Variable | Meaning |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string (local run) |
| `Jwt__Key` / `JWT_KEY` | secret that signs access tokens, 32+ characters (`openssl rand -base64 48`) |
| `Auth__DemoPassword` / `DEMO_PASSWORD` | password of the demo users, 8+ characters |

On the first start the backend applies the migrations, fills the database with deterministic demo data (30 days, 3 lines, 3 shifts, up to the current hour) and creates the demo users `manager`, `supervisor` (all lines), `operator1` and `operator2` (one line each). Data for the hours that passed since the last start is added on every start. Set `Data__SeedMockData=false` to switch the demo data off.

Optional settings (in `appsettings.json` or as environment variables): `RateLimiting__Login__PermitLimit` and `RateLimiting__Login__WindowSeconds` (sign-in attempts per client address, default 10 per 60 s), `Https__Redirect` (redirect HTTP to HTTPS, on in Development only by default). Behind the nginx container the client address is read from `X-Forwarded-For`, trusted only from private networks.

**With Docker** (database, backend and frontend):

```bash
cp .env.example .env   # then set POSTGRES_PASSWORD, JWT_KEY, DEMO_PASSWORD
docker compose up --build
```

The app is on http://localhost:4200, the API on http://localhost:5141.

**Without Docker:**

```bash
# backend: needs a running PostgreSQL and backend/.env
cd backend/src/LossLens.Api && dotnet run

# frontend: proxies /api to http://localhost:5141
cd frontend && npm ci && npm start
```

Sign in at http://localhost:4200 with a demo user and the demo password. Tests: `npm test` in `frontend`, `dotnet test` in `backend`.

The endpoint tests (`LossLens.Api.Tests/Integration`) start the real API against a throwaway PostgreSQL container, so they need Docker. They never touch the database from your `.env`: the test run sets its own connection string, and variables that are already set win over `.env`. To run only the fast tests without Docker, use `dotnet test --filter "Category!=Integration"`.

## Where the data comes from

**In a real plant** nobody types numbers into LossLens. Each station tests every board and writes the result (passed on the first attempt or not, defect code) to the test equipment or the MES. Once an hour the counts per line, station and hour are collected and written to the tables `HourlyRecords`, `TestRecords` and `DefectEntries`. LossLens only reads them.

**In this demo** the backend generates the data itself (`MockDataGenerator`) and `MockDataSeeder` writes it to the same tables on every start: the catalog (lines, stations, shifts) once, then the hours that are missing up to the current one. `Data__SeedMockData=false` switches it off.

**Next step:** an import for the real data, either a batch `POST` from the equipment or the MES or a CSV file, both through one service, and an hourly worker. Lines, stations and users are created by an admin API, not in the MVP yet.

## Milestones

M0 scaffold -> M1 formulas + tests -> M2 mock + API -> M3 frontend shell -> M4 dashboard and plan -> M5 FPY -> M6 defects -> M7 polish. M0-M6 are done, M7 is in progress. Details in section 10 of the requirements.

## Open questions for the customer

Alerts on a phone or a smartwatch (Q1), real data source (Q2), which FPY definition is the headline number (Q3), shift schedule (Q4), thresholds (Q5), defect opportunities per board (Q6). Full list in section 1.3 of the requirements.
