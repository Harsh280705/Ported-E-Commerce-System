# Yantra Shop — Legacy E-Commerce ported to Vue 3 + .NET 10 Minimal API

Decoupled e-commerce system: a Vue 3 SPA storefront backed by a stateless
ASP.NET Core Minimal API (EF Core 10 + SQL Server), shipped as three Docker
services behind Nginx. Ported from the Week-3 legacy MVC 5 / EF 6 application
(see `docs/MIGRATION.md`), preserving its catalog, cart, checkout, pricing, and
admin behaviors on a modern stack.

## Stack

- **Frontend:** Vue 3, TypeScript (strict), Vite, Vue Router, Pinia, Axios,
  Bootstrap 5, Vitest + Vue Test Utils, Playwright.
- **Backend:** .NET 10 Minimal APIs, EF Core 10, SQL Server, JWT + rotating
  refresh tokens, FluentValidation, Serilog, ProblemDetails, built-in OpenAPI,
  xUnit + Moq + `WebApplicationFactory`.
- **Infra:** Docker, Docker Compose, Nginx (SPA + `/api/` + `/openapi/` proxy),
  SQL Server in an isolated container with a named volume.

## Prerequisites (Windows)

Docker Desktop, .NET 10 SDK (local backend dev/tests), Node.js 20+ + npm,
Git. Host port **8080** free (configurable via `HOST_PORT`).

## Quick start (production-like)

```powershell
Copy-Item .env.example .env   # then set strong DB_PASSWORD + JWT_KEY in .env
docker compose --project-name portecommerce build
docker compose --project-name portecommerce up -d
```

| What | URL |
|---|---|
| Storefront | http://localhost:8080/ |
| API | http://localhost:8080/api/ (e.g. `/api/products`) |
| OpenAPI JSON | http://localhost:8080/openapi/v1.json |
| Swagger UI | dev only: backend `/swagger` (e.g. `http://localhost:5000/swagger`) |
| Health | http://localhost:8080/health (DB: `/health/db`) |

Seeded logins: admin **`admin@yantra.local` / `Admin!123`**,
customer **`customer@example.com` / `Customer!123`**.

Useful commands (all with `--project-name portecommerce`):

```powershell
docker compose ps
docker compose logs api            # api / nginx / db
docker compose restart api nginx   # data persists (volume untouched)
docker compose down                # stops; KEEPS database volume
# NEVER: docker compose down -v (deletes all store data)
```

## Run backend independently

```powershell
cd backend
$env:ConnectionStrings__Default="Server=localhost,1433;Database=EcommerceDb;User Id=sa;Password=<pwd>;TrustServerCertificate=True"
$env:JWT_KEY="<dev-key-32+-chars>"
dotnet run --project Ecommerce.Api        # listens http://localhost:5000
```

Migrations live in `Ecommerce.Infrastructure/Migrations` (already applied in
Docker via startup `MigrateAsync`). Add one with:
`dotnet ef migrations add <Name> --project Ecommerce.Infrastructure --startup-project Ecommerce.Infrastructure`
(`DesignTimeFactory` supplies the design-time context). Seeding
(`SeedData`: 7 categories, 30 products, admin + customer) runs at startup,
is idempotent, and never deletes user data.

## Run frontend independently

```powershell
cd frontend
npm install
npm run dev        # http://localhost:5173, /api proxied to http://localhost:5000
```

## Tests & coverage

```powershell
cd backend; dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj          # 56 tests
dotnet test --collect:"XPlat Code Coverage"                             # Core 92%, Api 70.6%
cd ../frontend; npm run test:unit                                       # 50 tests (stores 82.3%)
npm run typecheck; npm run build
npx playwright test --config playwright.prod.config.ts --workers=1      # 17 E2E vs live stack
```

Details, measured numbers, and known limitations: `docs/TESTING.md`.

## Structure

```
backend/Ecommerce.{Api,Core,Infrastructure,Tests}/  .NET solution (Ecommerce.slnx)
frontend/src/{api,assets,components,composables,layouts,router,stores,types,utils,views}
nginx/{nginx.conf,Dockerfile}   docker-compose.yml   .env.example   docs/
```

Docs: `MIGRATION.md` (legacy map) · `ARCHITECTURE.md` (design + Mermaid flows) ·
`API.md` (endpoints/DTOs/statuses) · `TESTING.md` (results/coverage) ·
`TEST_CATALOG.md` (2000-product seeding + image system) ·
`REFLECTION.md` · `DEMO_SCRIPT.md`.

## Test catalog & images

```powershell
$env:SEED_TEST_CATALOG="true"
docker compose --project-name portecommerce up -d api   # inserts deterministic 2000-product catalog once (idempotent)
Remove-Item Env:\SEED_TEST_CATALOG
docker compose --project-name portecommerce up -d api   # back to steady state
python frontend/scripts/gen_images.py --api http://localhost:8080  # regenerate local product art
```

The catalog page offers **Pagination** and **Infinite scroll** modes (persisted
preference), debounced search, category/sort filters, and server-side paging.

## Configuration

All secrets via environment (`.env`, never committed). Required:
`DB_PASSWORD` (SQL Server rules: 8+ chars, upper/lower/digit/symbol),
`JWT_KEY` (random 32+ chars). Optional: `HOST_PORT` (default 8080).
Local HTTP is used for development; production should terminate HTTPS at/in front
of Nginx (refresh cookie is `Secure` when the incoming request is HTTPS).
Access tokens live in frontend memory only; refresh tokens are HttpOnly cookies
(`SameSite=Lax`, `Path=/api/auth`) and rotate on every use.

## Troubleshooting

- `401 on /api/auth/*` after `up`: ensure `JWT_KEY` is set in `.env` (fail-fast
  `${JWT_KEY:?...}` aborts startup otherwise); `docker compose logs api`.
- `api` unhealthy at boot: SQL Server takes ~40 s; Compose waits on `db`
  health + `api` runs migrations before serving (`/health/db` gates `nginx`).
- Port clash on 8080: set `HOST_PORT=8090` in `.env` and restart.
- Windows path length during `dotnet restore`: keep the repo in a short path.
- Legacy Week-3 stack also runs? Both projects use isolated Compose project
  names, networks, and volumes — they coexist (legacy :8085, this :8080).
