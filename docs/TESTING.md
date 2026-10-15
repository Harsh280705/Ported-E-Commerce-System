# Testing

## Backend — xUnit + WebApplicationFactory (56 tests, all passing)

Run: `cd backend && dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj`
Coverage: `dotnet test --collect:"XPlat Code Coverage"` (report under
`Ecommerce.Tests/TestResults/*/coverage.cobertura.xml`).

| Suite | File | Tests | Covers |
|---|---|---|---|
| Unit | `PricingTests.cs` | 21 | shipping tiers, 8 % tax, discount %, forward-only status transitions |
| Integration | `AuthTests.cs` | 8 | register/login/me, duplicate 409, bad DTO 400, wrong password 401, refresh rotation + reuse rejection, invalid token, logout revocation |
| Integration | `CatalogTests.cs` | 8 | categories, pagination metadata, search, category filter, price sort, detail/404, `/health`, `/openapi/v1.json` |
| Integration | `CartOrderTests.cs` | 10 | guest add/update/remove, qty 400, out-of-stock 409, login merge, checkout totals/stock-clear, idempotent retry, empty-cart 400, bad method 400, auth 401, order ownership 403 |
| Integration | `AccountAdminTests.cs` | 8 (eat) | profile, address CRUD + single-default + isolation, admin 401/403, dashboard, product CRUD + history-blocked delete, category-blocked delete, status flow + backward rejection |
| Integration | `TestCatalogSeederTests.cs` | 4 | deterministic seeding + idempotent rerun, 2000 default target, admin search/filter/sort/paging, image-URL persistence + invalid-URL 400 |

Test DB: isolated **per test class** SQLite in-memory database (unique name per
`TestFactory`; `Program.cs` selects SQLite when `TestSqlite` is set, avoiding a
dual-provider conflict), seeded with 2 categories / 3 products / test admin.
HTTP clients are built with `HandleCookies=false` so refresh-token rotation and
reuse detection are asserted explicitly. Auth rate limit is relaxed (1000/min) in
the `Testing` environment only.

Measured line coverage: **Ecommerce.Core 92 %** (target ≥ 70 ✓),
**Ecommerce.Api 70.6 %** (services + endpoints), **Ecommerce.Infrastructure 40.7 %**
(mostly generated migration code). Business rules + services exceed the target.

Real bugs found by these tests and fixed: JWT placeholder key too short for HS256
(test env), dual EF provider registration in the factory, shared in-memory DB
across parallel test classes, hand-built `HttpClient`s bypassing the test server,
cookie-jar masking of refresh reuse, and a `CfgOrEnv` fallback bug (see below).

## Frontend — Vitest + Vue Test Utils (50 tests, all passing)

Run: `cd frontend && npm run test:unit` · Coverage: `npx vitest run --coverage`
(v8). 12 spec files: auth/cart/catalog/orders stores, `useProductBrowse`
(modes, dedupe, stale-race, retry, first-load flag), ProductCard (incl. image +
disabled states), Pagination, LoginForm, AdminOrderTable, AdminProductsView
(list, image badges, server search), router guards, validators +
`isFallbackImage`/`formatINR`.
Measured: **stores 82.3 % statements** (target ≥ 60 ✓; cart + catalog 100 %),
overall 20.5 % (views are covered by Playwright instead).

Typecheck (`vue-tsc --noEmit`) and production build (`vite build`) are clean.

## End-to-end — Playwright/Chromium (17 tests, all passing)

- Dev config: `playwright.config.ts` (dev server :5173 + backend :5000).
- Prod verification: `playwright.prod.config.ts` runs `e2e/smoke.spec.ts` (6) +
  `e2e/catalog.spec.ts` (11) against the Docker+Nginx stack at
  `http://localhost:8080` (no dev server).
- Run: `cd frontend && npx playwright test --config playwright.prod.config.ts --workers=1`
- Covers all 11 required scenarios plus: 2000+ catalog count, full-width grid at
  1440px, tablet/mobile breakpoints without overflow, unique local card images
  matching detail pages, pagination vs infinite switching with filter preserved,
  debounced search incl. empty state, card add-to-cart, admin CRUD with image
  update reflected in the storefront, non-admin 403 + route block.

E2E found two real product bugs, both fixed and re-verified green:
1. **Auth-restore race** — the router's initial navigation ran before the startup
   silent refresh, so full-page loads of protected routes bounced to login.
   Fixed by installing the router only after `auth.init()` resolves (`main.ts`).
2. **Null billing address** — "same as shipping" sent `billing: null`, which the API
   correctly rejects. Fixed by sending a shipping copy (`CheckoutView`).
   The E2E checkout test now also adds a cart item first (empty-cart checkout
   must fail by design).

Catalog-work bugs found by the new suites and fixed:
3. **Missing initial load** — `ProductListView` skipped its first fetch when the
   URL already matched default filters. Fixed with an `initialized` guard.
4. **Empty-state flash** — skeleton/footer competed before first load completed
   (also broke strict-mode selectors). Fixed with a `hasLoaded` flag: skeleton
   until the first page settles.
5. **Duplicate headings** — mini-cart title and cart empty-state collided with
   page H1s under accessible-name matching. Renamed to non-colliding text.
6. **4px mobile overflow** — header action row exceeded 390px viewports. Auth
   buttons now collapse into the mobile menu on xs screens.

## Production incident log (worth recording)

After `docker compose up`, authenticated endpoints returned 401 for freshly
issued tokens. Cause: `appsettings.json` carries a literal `"${JWT_KEY}"`
placeholder; the `CfgOrEnv` helper returned the dev fallback whenever the config
value was non-null — never consulting the environment — while `TokenService`
resolved the real env key. Signing (64 chars) and validation (50-char fallback)
diverged in the same process. Fixed by making the helper try
config → environment → fallback in order, rebuilt, redeployed, and re-verified
register → me → cart → merge → checkout → history plus the full E2E suite.

## Not run / limitations

- Playwright against the **dev** config was not executed (needs a local API on
  :5000 with a reachable SQL Server; the prod-config run supersedes it against
  the real deployment).
- No load/penetration testing; rate limits and security headers are baseline only.
- Legacy directory was verified untouched (`git status` clean; see final report).
