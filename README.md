# Yantra Shop — Ported E-Commerce System

A modern e-commerce application migrated from the legacy ASP.NET MVC 5 application to a decoupled Vue 3 frontend and .NET 10 Minimal API backend.

## Tech Stack

- **Frontend:** Vue 3, TypeScript, Vite, Pinia, Vue Router, Axios, Bootstrap 5
- **Backend:** .NET 10 Minimal API, Entity Framework Core 10
- **Database:** Microsoft SQL Server
- **Authentication:** JWT access tokens and rotating refresh tokens
- **Deployment:** Docker, Docker Compose, Nginx
- **Testing:** xUnit, Vitest, Playwright

## Features

- Product catalog with search, filters, sorting, pagination, and infinite scroll
- User authentication and role-based access
- Shopping cart and guest cart merging
- Checkout and order management
- Customer account and admin dashboard
- Containerized deployment and health checks

## Prerequisites

Install the following on Windows:

- Docker Desktop
- .NET 10 SDK
- Node.js 20+ and npm
- Git

## Implementation Steps

### 1. Clone the repository

```powershell
git clone https://github.com/Harsh280705/Ported-E-Commerce-System.git
cd Ported-E-Commerce-System
```

### 2. Configure environment variables

```powershell
Copy-Item .env.example .env
```

Edit `.env` and set strong values for `DB_PASSWORD` and `JWT_KEY`. Do not commit `.env` or share your secrets.

### 3. Build and start the application

Ensure Docker Desktop is running, then execute:

```powershell
docker compose --project-name portecommerce build
docker compose --project-name portecommerce up -d
```

Check the running services:

```powershell
docker compose --project-name portecommerce ps
```

### 4. Open the application

| Component | URL |
|---|---|
| Storefront | http://localhost:8080 |
| Products API | http://localhost:8080/api/products |
| OpenAPI JSON | http://localhost:8080/openapi/v1.json |
| Health check | http://localhost:8080/health |
| Database health | http://localhost:8080/health/db |

### 5. Sign in

| Role | Email | Password |
|---|---|---|
| Admin | `admin@yantra.local` | `Admin!123` |
| Customer | `customer@example.com` | `Customer!123` |

These are development seed credentials. Change them before any public or production deployment.

### 6. Run tests

**Backend:**

```powershell
cd backend
dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj
```

**Frontend:**

```powershell
cd ../frontend
npm install
npm run test:unit
npm run typecheck
npm run build
```

End-to-end tests require the application stack to be running:

```powershell
npx playwright test --config playwright.prod.config.ts --workers=1
```

### 7. Stop the application

From the project root:

```powershell
docker compose --project-name portecommerce down
```

This stops the services but preserves the database volume. **Do not use `down -v` unless you intentionally want to delete the stored database volume.**

## Project Structure

```text
backend/
  Ecommerce.Api/
  Ecommerce.Core/
  Ecommerce.Infrastructure/
  Ecommerce.Tests/
frontend/
  src/
nginx/
docker-compose.yml
.env.example
docs/
```

## Migration Overview

The project replaces the legacy ASP.NET MVC 5 and Entity Framework 6 architecture with a Vue 3 single-page application, a .NET 10 API, and Entity Framework Core. Docker Compose manages the services, while Nginx serves the frontend and routes API requests.

For detailed architecture, migration notes, API documentation, and testing results, see the `docs/` directory.
