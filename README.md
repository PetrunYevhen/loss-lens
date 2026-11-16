# LossLens

Web app for **daily production metrics** at an electronics factory (EMS). It shows how the shift/day is going and where exactly output is being lost.

> Status: MVP on generated demo data. Full requirements: [REQUIREMENTS.md](REQUIREMENTS.md).

## What it is and why

Operators, line supervisors and managers ask the same questions every day:

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

## EMS and smartwatch: do they relate to the metrics?

**EMS is not a metric.** It is the domain: Electronics Manufacturing Services, i.e. contract manufacturing of electronics (SMT lines, SPI / AOI / ICT / FCT stations). The metrics above are chosen for this kind of production process.

**A smartwatch does not change the metrics themselves.** It depends on what was meant (open question Q1 in the requirements, to be confirmed with the customer):

1. The watch as the **product being assembled**: it is just a product on the line, with the same FPY / defects / plan. Nothing special.
2. The watch as a **notification device** for a supervisor or manager (1-3 numbers plus a push when a threshold is breached). This is a delivery channel, not a metric, and needs a separate alerts module.

There is no watch support in the MVP. Watch notifications are in the backlog.

## Roles

| Role | Sees |
|---|---|
| Operator | Current shift, plan vs actual per hour, defects of own line |
| Line Supervisor | FPY by station, defect Pareto, trend (assigned lines) |
| Manager | Summary of all lines, comparison, trends |

In the MVP the role is picked manually (demo only, **not secure**). JWT comes later.

## Stack

- **Backend:** ASP.NET Core / C# (`Api` -> `Core` -> `Infrastructure`)
- **Frontend:** Angular + TypeScript (standalone components, signals), charts with Apache ECharts
- **Data:** deterministic mock generator behind `IMetricsRepository`; a real source can be plugged in later without changing the API
- **UI language:** Ukrainian by default (strings in a resource file); code, comments, docs and commits are in English

## Project structure

```
LossLens/
├── README.md
├── REQUIREMENTS.md
├── docs/                          # additional documentation
├── backend/
│   ├── src/
│   │   ├── LossLens.Api/          # thin controllers, demo role (X-Demo-Role)
│   │   │   ├── Controllers/       # Lines, Kpi, PlanVsActual, Fpy, Defects, Config
│   │   │   └── Auth/              # DemoRoleMiddleware
│   │   ├── LossLens.Core/         # pure formulas, no I/O
│   │   │   ├── Metrics/           # PlanAttainment, Fpy, Defect, Rag
│   │   │   ├── Models/            # Line, Station, Shift, hourly/test/defect records
│   │   │   ├── Abstractions/      # IMetricsRepository
│   │   │   └── Config/            # thresholds, opportunitiesPerUnit
│   │   └── LossLens.Infrastructure/
│   │       └── Mock/              # MockDataGenerator, MockMetricsRepository
│   └── tests/
│       ├── LossLens.Core.Tests/   # xUnit for all formulas, incl. edge cases
│       └── LossLens.Api.Tests/    # integration tests, incl. 403 by role
└── frontend/                      # Angular app (Angular config files come from ng new)
    └── src/app/
        ├── dashboard/             # page + kpi-card component
        ├── plan/                  # plan vs actual per hour
        ├── fpy/                   # trend and FPY by station
        ├── defects/               # Pareto, DPMO, defect table
        └── shared/
            ├── api/               # typed API client services
            ├── models/            # DTO interfaces mirroring the API contract
            ├── filters/           # filter bar, URL-synced filter state
            ├── role/              # demo role selector, header interceptor
            ├── ui/                # status badge (RAG), loading/empty/error view state
            └── i18n/              # uk strings
```

`frontend/src/app` contains only empty placeholder files. Angular's own files (`package.json`, `angular.json`, `app.ts`, ...) are not there yet, because `ng new` refuses to run in a non-empty directory. To generate them without conflicts, scaffold in a temp folder and merge:

```bash
ng new frontend-tmp --style=scss --routing --skip-git
rsync -a frontend-tmp/ frontend/ && rm -rf frontend-tmp
```

The placeholder files do not overlap with Angular's generated files, so nothing is overwritten.

## API (short)

Base path `/api/v1`: `/lines`, `/kpi/summary`, `/plan-vs-actual`, `/fpy/trend`, `/fpy/stations`, `/defects/pareto`, `/config/thresholds`. Details and examples are in section 6 of [REQUIREMENTS.md](REQUIREMENTS.md).

## Running

The backend needs PostgreSQL, a signing key for the tokens and, to sign in, a demo password. Secrets live in `.env`, which git ignores. Copy `backend/.env.example` (local run) or `.env.example` (Docker) to `.env` and set the values:

| Variable | Meaning |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string (local run) |
| `Jwt__Key` / `JWT_KEY` | secret that signs access tokens, 32+ characters (`openssl rand -base64 48`) |
| `Auth__DemoPassword` / `DEMO_PASSWORD` | password of the demo users, 8+ characters |

On the first start the backend applies the migrations, fills the database with deterministic demo data (30 days, 3 lines, 3 shifts, up to the current hour) and creates the demo users `manager`, `supervisor` (all lines), `operator1` and `operator2` (one line each). Data for the hours that passed since the last start is added on every start. Set `Data__SeedMockData=false` to switch the demo data off.

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

Sign in at http://localhost:4200 with a demo user and the demo password. Tests: `dotnet test` in `backend`, `npm test` in `frontend`.

## Milestones

M0 scaffold -> M1 formulas + tests -> M2 mock + API -> M3 frontend shell -> M4 dashboard and plan -> M5 FPY -> M6 defects -> M7 polish. Details in section 10 of the requirements.

## Open questions for the customer

Smartwatch (Q1), real data source (Q2), which FPY definition is the headline number (Q3), shift schedule (Q4), thresholds (Q5), defect opportunities per board (Q6). Full list in section 1.3 of the requirements.
