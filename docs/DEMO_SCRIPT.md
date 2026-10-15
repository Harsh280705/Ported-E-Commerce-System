# Demo script (5–10 minutes)

Stack running via `docker compose --project-name portecommerce up -d`.
Storefront http://localhost:8080 · API http://localhost:8080/api ·
Swagger (dev) http://localhost:5000/swagger · OpenAPI http://localhost:8080/openapi/v1.json ·
Health http://localhost:8080/health.
Seeded logins: admin `admin@yantra.local / Admin!123`, customer `customer@example.com / Customer!123`.
(No demo video was recorded; this script is the prepared walkthrough.)

## 1. Storefront + catalog (1 min)
- Open `/`. Point out hero, featured products, category nav.
- Go `/products`. Show pagination, sort dropdown (price low→high), category links.

## 2. Search + filter (1 min)
- Search "phone" → only matching cards (name/description/SKU).
- Click Electronics → list filters; open a product → details, price, stock, quantity.

## 3. Guest cart (1 min)
- Add 2 units → mini-cart drawer opens in header with count.
- Open `/cart` → subtotal, shipping, tax, grand total (note free-shipping ≥ ₹75 logic).

## 4. Register/login + merge (1.5 min)
- Register a new user (unique e-mail) → auto-login, guest items retained (merged).
- Show `Application → Cookies`: only `refresh_token` (HttpOnly); access token is in memory.

## 5. Checkout + confirmation (1.5 min)
- `/checkout`: fill shipping, keep "same as billing", standard shipping, COD → Place order.
- Confirmation shows order number + total. Refresh → cart is empty.

## 6. Account + history (1 min)
- `/account`: edit profile, add a second address, flip the default.
- `/account/orders` → history; open the order → lines, snapshots, status.

## 7. Admin (1.5 min)
- Login as admin → `/admin`: live counts (products/orders/customers/revenue).
- `/admin/products`: create "Demo Saree", edit price, delete it (note: products with
  orders can't be deleted → deactivate instead).
- `/admin/orders`: advance the new order `Placed → Processing`.

## 8. Platform (1 min)
- `docker compose ps` (3 healthy services); `docker volume inspect portecommerce_mssql_data`.
- `curl /health`, `/openapi/v1.json`; restart `api` and show data persists.

## Fallback one-liners (if live demo breaks)
- API direct proof: `GET /api/products`, `POST /api/auth/login` via Swagger/curl.
- Tests: `dotnet test` (52 passing), `npm run test:unit` (38), Playwright prod run (6 passing).
